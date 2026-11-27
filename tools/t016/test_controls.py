import errno,pathlib,struct,sys,unittest
from unittest.mock import patch
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]/'spikes/camera'))
from camera_controls import CameraSession,AUTO,ABSOLUTE,PRIORITY

class FakeSession(CameraSession):
    initial={AUTO:3,ABSOLUTE:156,PRIORITY:1}
    fail_absolute=False;unsupported=False
    def ioctl(self,number,data):
        FakeSession.last=self
        if not hasattr(self,'values'):self.values=dict(self.initial)
        if number in [4,21]:
            if number==4:struct.pack_into('III',data,8,640,480,int.from_bytes(b'MJPG','little'))
            else:struct.pack_into('II',data,12,1,30)
        elif number==36:
            cid=struct.unpack_from('I',data)[0]
            if self.unsupported and cid==ABSOLUTE:raise OSError(errno.EINVAL,'Unsupported')
            data[:]=struct.pack('II32siiiiIII',cid,3 if cid==AUTO else 1,b'Alias-independent',10 if cid==ABSOLUTE else 0,700 if cid==ABSOLUTE else 3,1,0,0,0,0)
        elif number==27:struct.pack_into('i',data,4,self.values[struct.unpack_from('I',data)[0]])
        elif number==28:
            cid,value=struct.unpack('Ii',data)
            if self.fail_absolute and cid==ABSOLUTE and value==100:raise OSError(errno.EIO,'Partial apply failure')
            self.values[cid]=value
        return data

class ControlsTests(unittest.TestCase):
    def test_snapshot_only_close_does_not_reconfigure_busy_foreign_stream(self):
        original=FakeSession.ioctl
        def busy(session,number,data):
            if number in [5,22]:raise OSError(errno.EBUSY,'Foreign active stream')
            return original(session,number,data)
        with patch('camera_controls.os.open',return_value=99),patch('camera_controls.os.close') as closed,patch.object(FakeSession,'ioctl',busy):
            s=FakeSession('/fake','original');s.close();self.assertTrue(s.restored);closed.assert_called_once_with(99)
    def test_format_restore_resets_interval_then_restores_parameters(self):
        original=FakeSession.ioctl
        def stateful(session,number,data):
            if not hasattr(session,'fmt'):
                session.fmt=bytearray(208);struct.pack_into('I',session.fmt,0,1);struct.pack_into('III',session.fmt,8,1920,1080,int.from_bytes(b'MJPG','little'))
                session.parm=bytearray(204);struct.pack_into('I',session.parm,0,1);struct.pack_into('II',session.parm,12,1,30)
            if number==4:data[:]=session.fmt
            elif number==21:data[:]=session.parm
            elif number==5:
                session.fmt=data[:];struct.pack_into('II',session.parm,12,1,15)
            elif number==22:session.parm=data[:]
            else:return original(session,number,data)
            return data
        with patch('camera_controls.os.open',return_value=99),patch('camera_controls.os.close'),patch.object(FakeSession,'ioctl',stateful):
            s=FakeSession('/fake');struct.pack_into('II',s.fmt,8,640,480);s.close()
            self.assertTrue(s.restored);self.assertEqual(s.before,s.after)
    def test_manual_preserves_priority_and_restores_all(self):
        with patch('camera_controls.os.open',return_value=99),patch('camera_controls.os.close') as closed:
            s=FakeSession('/fake','candidate');self.assertEqual({AUTO:1,ABSOLUTE:100,PRIORITY:1},s.values)
            s.close();self.assertEqual(s.initial,s.values);self.assertTrue(s.restored);closed.assert_called_once_with(99)
    def test_priority_override_is_restored(self):
        with patch('camera_controls.os.open',return_value=99),patch('camera_controls.os.close'):
            s=FakeSession('/fake','candidate',0);self.assertEqual(0,s.values[PRIORITY]);s.close();self.assertEqual(1,s.values[PRIORITY])
    def test_partial_apply_failure_rolls_back(self):
        with patch('camera_controls.os.open',return_value=99),patch('camera_controls.os.close') as closed,patch.object(FakeSession,'fail_absolute',True):
            with self.assertRaisesRegex(OSError,'Partial apply'):FakeSession('/fake','candidate')
            self.assertEqual(FakeSession.initial,FakeSession.last.values)
            closed.assert_called_once_with(99)
    def test_snapshot_codec_roundtrip_and_invalid_ids(self):
        with patch('camera_controls.os.open',return_value=99),patch('camera_controls.os.close'):
            s=FakeSession('/fake');encoded=s.encode(s.before);self.assertEqual(s.before,s.decode(encoded));s.close()
            encoded['controls']={123:0}
            with self.assertRaisesRegex(ValueError,'SNAPSHOT_INVALID'):s.decode(encoded)
    def test_unsupported_candidate_fails_explicitly_and_closes(self):
        with patch('camera_controls.os.open',return_value=99),patch('camera_controls.os.close') as closed,patch.object(FakeSession,'unsupported',True):
            snapshots=[]
            with self.assertRaisesRegex(RuntimeError,'CAMERA_PROFILE_UNSUPPORTED'):FakeSession('/fake','candidate',snapshot_callback=snapshots.append)
            self.assertEqual([],snapshots);self.assertEqual(FakeSession.initial,FakeSession.last.values)
            closed.assert_called_once_with(99)
