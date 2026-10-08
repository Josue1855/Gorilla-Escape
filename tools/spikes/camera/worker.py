#!/usr/bin/env python3
"""Linux lab vision adapter: local capture only; outputs bounded landmark JSON, never images.
One synchronous detector/writer, at most two subjects. Parent stdin EOF ends capture.
"""
import argparse,json,os,sys,threading,time
from pathlib import Path

def now(): return time.time_ns()//1_000_000

def emit(prefix,data=None):
    line=prefix if data is None else prefix+' '+json.dumps(data,separators=(',',':'),allow_nan=False)
    if len(line.encode())>16384: raise ValueError('CAMERA_FRAME_OVERSIZED')
    print(line,flush=True)

def main():
    p=argparse.ArgumentParser();p.add_argument('--mode',choices=['replay','physical','unavailable','failure','malformed','oversized','silent'],required=True)
    for n in ['fixture','model','device']: p.add_argument('--'+n,default='')
    a=p.parse_args();stop=threading.Event()
    def parent():
        while os.read(0,1): pass
        stop.set()
    reader=threading.Thread(target=parent,name='parent-eof',daemon=True);reader.start()
    capture=detector=None;frames=0;gaps=0;durations=[];captures=[];confidence=[];tracking=0;read_durations=[]
    try:
        if a.mode=='unavailable': raise RuntimeError('CAMERA_UNAVAILABLE')
        if a.mode=='silent': stop.wait();return
        if a.mode!='physical':
            corpus=json.loads(Path(a.fixture).read_text());cases=corpus['cameraCases'];origin=now()
        else:
            import cv2,mediapipe as mp
            capture=cv2.VideoCapture(a.device,cv2.CAP_V4L2)
            capture.set(cv2.CAP_PROP_FOURCC,cv2.VideoWriter_fourcc(*'MJPG'));capture.set(cv2.CAP_PROP_FRAME_WIDTH,640);capture.set(cv2.CAP_PROP_FRAME_HEIGHT,480);capture.set(cv2.CAP_PROP_FPS,30);capture.set(cv2.CAP_PROP_BUFFERSIZE,1)
            if not capture.isOpened():raise RuntimeError('CAMERA_UNAVAILABLE')
            detector=mp.tasks.vision.PoseLandmarker.create_from_options(mp.tasks.vision.PoseLandmarkerOptions(base_options=mp.tasks.BaseOptions(model_asset_path=a.model),running_mode=mp.tasks.vision.RunningMode.VIDEO,num_poses=2))
            previous=[];next_id=1;origin=now();mono=time.monotonic_ns();last_detector_time=-1
        emit('CAMERA_READY')
        if a.mode=='failure': raise RuntimeError('VISION_FAILURE_FIXTURE')
        if a.mode=='malformed': print('CAMERA_FRAME {',flush=True);stop.wait();return
        if a.mode=='oversized': print('CAMERA_FRAME '+'x'*17000,flush=True);stop.wait();return
        while not stop.is_set():
            started=time.monotonic_ns()
            if a.mode!='physical':
                # Controlled relative times are rebased onto this PC clock, never passed off as physical.
                processing_started=started;read_ms=0
                case=cases[(frames//5)%len(cases)];frame=json.loads(json.dumps(case['frame']));frame['captureTimestamp']=now();frame['processTimestamp']=now();frame['source']='replay'
                frame['captureTimeSource']='fixture';frame['quality']='fixture'
            else:
                ok,image=capture.read();stamp=now();processing_started=time.monotonic_ns();read_ms=(processing_started-started)/1e6
                if not ok:raise RuntimeError('CAMERA_READ_FAILED')
                dt=max(last_detector_time+1,(time.monotonic_ns()-mono)//1_000_000);last_detector_time=dt
                result=detector.detect_for_video(mp.Image(image_format=mp.ImageFormat.SRGB,data=cv2.cvtColor(image,cv2.COLOR_BGR2RGB)),dt)
                subjects=[];used=set()
                for landmarks in result.pose_landmarks[:2]:
                    cx=(landmarks[11].x+landmarks[12].x)/2;cy=(landmarks[11].y+landmarks[12].y)/2
                    # Ephemeral spatial continuity; ambiguity/loss never implies biometric identity.
                    matches=[s for s in previous if (s['centerX']-cx)**2+(s['centerY']-cy)**2<.04]
                    ambiguous=len(matches)>1
                    if len(matches)==1 and matches[0]['subjectId'] not in used:sid=matches[0]['subjectId']
                    else:sid=next_id;next_id+=1;ambiguous=ambiguous or len(matches)>0
                    used.add(sid);points=[]
                    for name,index in [('left-shoulder',11),('right-shoulder',12),('left-elbow',13),('right-elbow',14),('left-wrist',15),('right-wrist',16),('left-hand',19),('right-hand',20)]:
                        l=landmarks[index];v=float(l.visibility);presence=float(l.presence)
                        points.append(dict(name=name,x=float(l.x),y=float(l.y),z=float(l.z),visibility=v,presence=presence,visible=min(v,presence)>=.5))
                    conf=min(min(l['visibility'],l['presence']) for l in points[:6]);subjects.append(dict(subjectId=sid,centerX=max(0,min(1,cx)),centerY=max(0,min(1,cy)),confidence=conf,ambiguous=ambiguous,landmarks=points))
                previous=subjects
                frame=dict(version=1,width=image.shape[1],height=image.shape[0],source='physical-webcam',quality='observed',clockDomain='pc-unix-ms',captureTimeSource='read-complete',captureTimestamp=stamp,processTimestamp=now(),subjects=subjects,trackingState='TRACKING' if subjects else 'LOST')
            processing_ms=(time.monotonic_ns()-processing_started)/1e6
            frames+=1;frame['frameSequence']=frames;emit('CAMERA_FRAME',frame)
            durations.append(processing_ms);read_durations.append(read_ms);captures.append(frame['captureTimestamp']);tracking+=bool(frame['subjects']);gaps+=not bool(frame['subjects']);confidence.extend(s['confidence'] for s in frame['subjects'])
            # Bound observation metrics, no lifetime growth.
            if len(durations)>4096:durations.pop(0);read_durations.pop(0);captures.pop(0)
            if len(confidence)>8192:del confidence[:-8192]
            stop.wait(max(0,1/30-(time.monotonic_ns()-started)/1e9))
    finally:
        if capture is not None:capture.release()
        if detector is not None:detector.close()
        if frames:
            values=sorted(durations);reads=sorted(read_durations)
            emit('CAMERA_SUMMARY',dict(frames=frames,retainedMetrics=len(values),fpsObserved=(len(captures)-1)*1000/(captures[-1]-captures[0]) if len(captures)>1 and captures[-1]>captures[0] else None,processingP50Ms=values[(len(values)-1)//2],processingP95Ms=values[max(0,__import__('math').ceil(len(values)*.95)-1)],captureReadP50Ms=reads[(len(reads)-1)//2],captureReadP95Ms=reads[max(0,__import__('math').ceil(len(reads)*.95)-1)],trackingFrames=tracking,trackingGaps=gaps,confidenceMin=min(confidence) if confidence else None,confidenceMax=max(confidence) if confidence else None,hardwareDroppedFrames=None,cameraReleased=capture is not None,detectorClosed=detector is not None))
if __name__=='__main__':
    try:main()
    except Exception as e:
        print(type(e).__name__+': '+str(e),file=sys.stderr);sys.exit(2)
