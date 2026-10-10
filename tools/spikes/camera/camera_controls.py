"""Owned Linux V4L2 session controls. Standard control IDs, no hardware-name policy.

Candidate is opt-in, not accepted/default. Restore after capture.release, including startup failures.
Buffer count belongs to the capture handle, not a global V4L2 control.
"""
import errno,fcntl,os,struct,json

AUTO=0x009a0901;ABSOLUTE=0x009a0902;PRIORITY=0x009a0903
def request(number,size):return (3<<30)|(size<<16)|(ord('V')<<8)|number

class CameraSession:
    def __init__(self,device,profile='original',dynamic=None,snapshot_callback=None):
        self.fd=None;self.before=None;self.after=None;self.restored=False;self.profile=profile
        self.fd=os.open(device,os.O_RDWR|os.O_NONBLOCK)
        try:
            self.before=self.snapshot()
            if snapshot_callback is not None:snapshot_callback(self.encode(self.before))
            if profile=='candidate':
                for cid in [AUTO,ABSOLUTE]:
                    cap=self.query(cid)
                    if cap is None or cap['flags']&(1|2|4):raise RuntimeError('CAMERA_PROFILE_UNSUPPORTED')
                cap=self.query(ABSOLUTE)
                if not cap['minimum']<=100<=cap['maximum'] or (100-cap['minimum'])%max(1,cap['step']):raise RuntimeError('CAMERA_EXPOSURE_RANGE_UNSUPPORTED')
                menu=bytearray(struct.pack('II32sI',AUTO,1,b'',0));self.ioctl(37,menu)
                self.set(AUTO,1);self.set(ABSOLUTE,100)
                if dynamic is not None:
                    if self.query(PRIORITY) is None:raise RuntimeError('CAMERA_PRIORITY_UNSUPPORTED')
                    self.set(PRIORITY,dynamic)
                if self.get(AUTO)!=1 or self.get(ABSOLUTE)!=100:raise RuntimeError('CAMERA_PROFILE_READBACK_FAILED')
        except BaseException:
            self.close();raise
    def ioctl(self,number,data):
        fcntl.ioctl(self.fd,request(number,len(data)),data,True);return data
    def query(self,cid):
        data=bytearray(struct.pack('II32siiiiIII',cid,0,b'',0,0,0,0,0,0,0))
        try:self.ioctl(36,data)
        except OSError as error:
            if error.errno==errno.EINVAL:return None
            raise
        v=struct.unpack('II32siiiiIII',data)
        return dict(id=cid,type=v[1],name=v[2].split(b'\0')[0].decode(),minimum=v[3],maximum=v[4],step=v[5],flags=v[7])
    def get(self,cid):return struct.unpack('Ii',self.ioctl(27,bytearray(struct.pack('Ii',cid,0))))[1]
    def set(self,cid,value):self.ioctl(28,bytearray(struct.pack('Ii',cid,value)))
    def snapshot(self):
        fmt=bytearray(208);struct.pack_into('I',fmt,0,1);self.ioctl(4,fmt)
        parm=bytearray(204);struct.pack_into('I',parm,0,1);self.ioctl(21,parm)
        controls={cid:self.get(cid) for cid in [AUTO,ABSOLUTE,PRIORITY] if self.query(cid) is not None}
        return dict(format=bytes(fmt),parameters=bytes(parm),controls=controls)
    def describe(self,snapshot=None):
        value=snapshot or self.snapshot();width,height,fourcc=struct.unpack_from('III',value['format'],8)
        numerator,denominator=struct.unpack_from('II',value['parameters'],12)
        return dict(width=width,height=height,fourcc=struct.pack('I',fourcc).decode(),frameInterval=[numerator,denominator],
            controls={hex(k):v for k,v in value['controls'].items()},capabilities={hex(cid):self.query(cid) for cid in [AUTO,ABSOLUTE,PRIORITY]})
    @staticmethod
    def encode(snapshot):
        return dict(format=snapshot['format'].hex(),parameters=snapshot['parameters'].hex(),controls=snapshot['controls'])
    @staticmethod
    def decode(value):
        fmt=bytes.fromhex(value['format']);parm=bytes.fromhex(value['parameters']);controls={int(k):int(v) for k,v in value['controls'].items()}
        if len(fmt)!=208 or len(parm)!=204 or struct.unpack_from('I',fmt)[0]!=1 or struct.unpack_from('I',parm)[0]!=1 or not set(controls)<=set([AUTO,ABSOLUTE,PRIORITY]):raise ValueError('CAMERA_RESTORE_SNAPSHOT_INVALID')
        return dict(format=fmt,parameters=parm,controls=controls)
    def close(self):
        if self.fd is None:return
        try:
            if self.before is not None:
                controls=self.before['controls'];current=self.snapshot()['controls']
                if controls!=current:
                    if ABSOLUTE in controls and current.get(ABSOLUTE)!=controls[ABSOLUTE]:
                        self.set(AUTO,1);self.set(ABSOLUTE,controls[ABSOLUTE])
                    if PRIORITY in controls and current.get(PRIORITY)!=controls[PRIORITY]:self.set(PRIORITY,controls[PRIORITY])
                    if AUTO in controls:self.set(AUTO,controls[AUTO])
                self.ioctl(5,bytearray(self.before['format']));self.ioctl(22,bytearray(self.before['parameters']))
                self.after=self.snapshot();self.restored=self.after==self.before
                if not self.restored:raise RuntimeError('CAMERA_RESTORE_MISMATCH')
        finally:os.close(self.fd);self.fd=None

if __name__=='__main__':
    import argparse,sys
    p=argparse.ArgumentParser();p.add_argument('--restore-device',required=True);a=p.parse_args()
    try:
        payload=sys.stdin.readline(4097)
        if len(payload)>4096:raise ValueError('CAMERA_RESTORE_SNAPSHOT_OVERSIZED')
        before=CameraSession.decode(json.loads(payload))
        session=CameraSession(a.restore_device)
        session.before=before;session.close()
        print('CAMERA_RESTORED',flush=True)
    except Exception as error:print(str(error),file=sys.stderr);sys.exit(2)
