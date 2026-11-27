#!/usr/bin/env python3
"""Linux lab vision adapter: local capture only; outputs bounded landmark JSON, never images.
One capture thread/one pending owned frame; one detector/writer, at most two subjects. Parent stdin EOF ends capture.
"""
import argparse,json,os,sys,threading,time,signal
from pathlib import Path
from pipeline import CapturePump, Metrics
from camera_controls import CameraSession

def now(): return time.time_ns()//1_000_000

def emit(prefix,data=None):
    line=prefix if data is None else prefix+' '+json.dumps(data,separators=(',',':'),allow_nan=False)
    if len(line.encode())>16384: raise ValueError('CAMERA_FRAME_OVERSIZED')
    print(line,flush=True)

def main():
    p=argparse.ArgumentParser();p.add_argument('--mode',choices=['replay','physical','unavailable','failure','malformed','oversized','silent'],required=True)
    for n in ['fixture','model','device']: p.add_argument('--'+n,default='')
    p.add_argument('--warmup',type=float,default=os.environ.get('GORILLA_CAMERA_WARMUP','0'));p.add_argument('--duration',type=float,default=0)
    p.add_argument('--profile',choices=['original','candidate'],default=os.environ.get('GORILLA_CAMERA_PROFILE','candidate'))
    p.add_argument('--buffers',type=int,choices=[1,2,3],default=os.environ.get('GORILLA_CAMERA_BUFFERS'))
    p.add_argument('--dynamic-framerate',type=int,choices=[0,1],default=None)
    p.add_argument('--telemetry',choices=['minimal','enabled'],default=os.environ.get('GORILLA_TELEMETRY_MODE','enabled'))
    a=p.parse_args()
    if a.warmup<0 or a.duration<0:raise ValueError('CAMERA_DURATION_INVALID')
    if a.buffers not in [None,1,2,3]:raise ValueError('CAMERA_BUFFER_INVALID')
    stop=threading.Event()
    if threading.current_thread() is threading.main_thread():
        signal.signal(signal.SIGTERM,lambda *_:stop.set())
        signal.signal(signal.SIGINT,lambda *_:stop.set())
    qa_enabled=os.environ.get('GORILLA_QA_OVERLAY')=='1'
    preview_request=(0,False);request_lock=threading.Lock()
    def parent():
        nonlocal preview_request
        command=bytearray()
        while True:
            value=os.read(0,1)
            if not value:stop.set();return
            if value==b'\n':
                parts=bytes(command).split();command.clear()
                if qa_enabled and len(parts)==4 and parts[:2]==[b'QA_PREVIEW',b'1'] and parts[2].isdigit() and parts[3] in [b'0',b'1']:
                    revision=int(parts[2])
                    with request_lock:
                        if preview_request[0]<revision:preview_request=(revision,parts[3]==b'1')
            elif len(command)<64:command.extend(value)
            else:stop.set();return
    reader=threading.Thread(target=parent,name='parent-eof',daemon=True);reader.start()
    preview=None;preview_revision=0;preview_ever=False;preview_overwritten=0;preview_errors=0;next_qa_metrics=0
    capture=detector=pump=timer=session=None;metrics=Metrics(a.warmup,enabled=a.telemetry=='enabled');frames=0;gaps=0;durations=[];captures=[];confidence=[];tracking=0;read_durations=[];skipped=0
    try:
        if a.mode=='unavailable': raise RuntimeError('CAMERA_UNAVAILABLE')
        if a.mode=='silent': stop.wait();return
        if a.mode!='physical':
            corpus=json.loads(Path(a.fixture).read_text());cases=corpus['cameraCases'];origin=now()
        else:
            if not Path(a.model).is_file():raise RuntimeError("CAMERA_MODEL_MISSING")
            import cv2,mediapipe as mp
            session=CameraSession(a.device,a.profile,a.dynamic_framerate,snapshot_callback=lambda snapshot:emit('CAMERA_CONTROL_SNAPSHOT',snapshot))
            controls_before=session.describe(session.before)
            buffers=a.buffers or (2 if a.profile=='candidate' else 1)
            capture=cv2.VideoCapture(a.device,cv2.CAP_V4L2)
            capture.set(cv2.CAP_PROP_FOURCC,cv2.VideoWriter_fourcc(*'MJPG'));capture.set(cv2.CAP_PROP_FRAME_WIDTH,640);capture.set(cv2.CAP_PROP_FRAME_HEIGHT,480);capture.set(cv2.CAP_PROP_FPS,30);capture.set(cv2.CAP_PROP_BUFFERSIZE,buffers)
            if not capture.isOpened():raise RuntimeError('CAMERA_UNAVAILABLE')
            detector=mp.tasks.vision.PoseLandmarker.create_from_options(mp.tasks.vision.PoseLandmarkerOptions(base_options=mp.tasks.BaseOptions(model_asset_path=a.model),running_mode=mp.tasks.vision.RunningMode.VIDEO,num_poses=2))
            fourcc=int(capture.get(cv2.CAP_PROP_FOURCC))
            configuration=dict(backend=capture.getBackendName(),device=a.device,width=capture.get(cv2.CAP_PROP_FRAME_WIDTH),height=capture.get(cv2.CAP_PROP_FRAME_HEIGHT),fourcc=''.join(chr((fourcc>>(8*i))&255) for i in range(4)),requestedFps=30,reportedFps=capture.get(cv2.CAP_PROP_FPS),reportedBuffer=capture.get(cv2.CAP_PROP_BUFFERSIZE),numPoses=2)
            configuration.update(profile=a.profile,controlsBefore=controls_before,controlsApplied=session.describe(),acquisitionTimestampSource='OpenCV V4L2 CAP_PROP_POS_MSEC / kernel timestamp; validated monotonic range, not optical ground truth',staleBudgetMs=100)
            previous=[];next_id=1;origin=now();mono=time.monotonic_ns();last_detector_time=-1;last_measured_ids=None
            metrics=Metrics(a.warmup,enabled=a.telemetry=='enabled')
            pump=CapturePump(capture,stop,metrics,acquisition_clock=lambda:int(capture.get(cv2.CAP_PROP_POS_MSEC)*1e6),preview=preview);pump.start()
            if a.duration:
                timer=threading.Timer(a.warmup+a.duration,stop.set);timer.daemon=True;timer.start()
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
                with request_lock:revision,requested=preview_request
                if revision!=preview_revision:
                    preview_revision=revision
                    if preview is not None:
                        pump.preview=None;preview.close();preview_overwritten+=preview.slot.overwritten;preview_errors+=preview.errors;preview=None
                    if requested:
                        from preview import Preview
                        preview=Preview(stop,metrics);pump.preview=preview;preview_ever=True
                    emit('CAMERA_PREVIEW_PORT',{'version':1,'revision':revision,'port':preview.port if preview else 0})
                captured=pump.slot.take(stop)
                if captured is None:break
                image=captured.image;stamp=captured.capture_timestamp;processing_started=time.monotonic_ns();read_ms=0
                metrics.observe('handoverMs',(processing_started-captured.monotonic_ns)/1e6,processing_started)
                if captured.acquisition_ns is not None:
                    age=(processing_started-captured.acquisition_ns)/1e6;metrics.observe('frameAgeAtInferenceMs',age,processing_started)
                    if age>100:skipped+=1;metrics.event('staleSkipped');continue
                dt=max(last_detector_time+1,(captured.monotonic_ns-mono)//1_000_000);last_detector_time=dt
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
            frames+=1;frame['frameSequence']=captured.sequence if a.mode=='physical' else frames;emit('CAMERA_FRAME',frame)
            if a.mode=='physical':
                metrics.event('inference',duration=processing_ms);metrics.event('output')
                if qa_enabled and time.monotonic()>=next_qa_metrics:
                    next_qa_metrics=time.monotonic()+1;r=metrics.report()
                    emit('CAMERA_QA_METRICS',{k:(r.get(k) if r.get(k) is not None else -1) for k in ['captureFps','inferenceFps'] } | {'previewFps':r.get('previewFps',-1) if preview else -1,'overwritten':preview_overwritten+(preview.slot.overwritten if preview else 0),'errors':preview_errors+(preview.errors if preview else 0)})
            if a.mode=='physical' and metrics.enabled:
                metrics.event('tracking' if frame['subjects'] else 'trackingGap')
                measured_at=time.monotonic_ns()
                if measured_at>=metrics.after:
                    ids=frozenset(s['subjectId'] for s in subjects)
                    metrics.event('framesWithSubject' if ids else 'framesWithoutSubject',measured_at)
                    if any(s['ambiguous'] for s in subjects):metrics.event('ambiguous',measured_at)
                    if ids:
                        if last_measured_ids is not None and ids!=last_measured_ids:metrics.event('subjectIdChanges',measured_at)
                        last_measured_ids=ids
                        metrics.observe('subjectPresentConfidence',max(s['confidence'] for s in subjects),measured_at)
                        metrics.observe('subjectPresentVisibleLandmarks',sum(l['visible'] for s in subjects for l in s['landmarks']),measured_at)
                    # Existing FusionSettings/PlayerLock defaults; descriptive eligibility, not a new AC.
                    if any(not s['ambiguous'] and s['confidence']>=.6 and .2<=s['centerX']<=.8 for s in subjects):metrics.event('productEligible',measured_at)
                metrics.observe('inferenceDurationMs',processing_ms)
                metrics.observe('readCompleteToOutputMs',(time.monotonic_ns()-captured.monotonic_ns)/1e6)
                metrics.observe('subjectsDetected',len(subjects));metrics.observe('confidence',max((s['confidence'] for s in subjects),default=0))
                metrics.observe('visibleLandmarks',sum(l['visible'] for s in subjects for l in s['landmarks']))
                metrics.observe('meanPixelValue',float(image.mean()))
                metrics.event('usefulTracking' if any(s['confidence']>=.5 and sum(l['visible'] for l in s['landmarks'])>=6 for s in subjects) else 'qualityGap')
            if a.mode=='physical' and not metrics.enabled:continue
            durations.append(processing_ms);read_durations.append(read_ms);captures.append(frame['captureTimestamp']);tracking+=bool(frame['subjects']);gaps+=not bool(frame['subjects']);confidence.extend(s['confidence'] for s in frame['subjects'])
            # Bound observation metrics, no lifetime growth.
            if len(durations)>4096:durations.pop(0);read_durations.pop(0);captures.pop(0)
            if len(confidence)>8192:del confidence[:-8192]
            if a.mode!='physical':stop.wait(max(0,1/30-(time.monotonic_ns()-started)/1e9))
    finally:
        if timer is not None:timer.cancel()
        if preview is not None:preview.close()
        try:
            if pump is not None:pump.close()
            elif capture is not None:capture.release()
        finally:
            try:
                if detector is not None:detector.close()
            finally:
                if session is not None:session.close()
        if pump is not None:
            report=metrics.report();report.update(telemetryLegacyRetainedSamples=len(durations)+len(read_durations)+len(captures)+len(confidence),telemetryLegacyCapacity=20480,telemetryLegacyFrameWindowOverwrites=max(0,frames-4096) if metrics.enabled else 0);report.update(configuration=configuration,capturedFrames=pump.frames,inferredFrames=frames,cameraInputEmitted=frames,overwrittenBeforeInference=pump.slot.overwritten,pendingDiscarded=pump.slot.published-pump.slot.taken-pump.slot.overwritten,captureThreadJoined=not pump.thread.is_alive(),cameraReleased=pump.released,detectorClosed=detector is not None,hardwareDroppedFrames=None,warmupSeconds=a.warmup)
            report.update(staleSkipped=skipped,acquisitionTimestampUnavailable=pump.timestamp_unavailable,controlsRestored=session.restored,clockCorrelation='Worker latency monotonic; CameraInput UTC timestamps unchanged',humanQualityAcceptance='NOT PROVEN by presence/confidence aggregates alone')
            present=report.get('framesWithSubjectFrames',0);absent=report.get('framesWithoutSubjectFrames',0)
            report.update(framesWithSubject=present,framesWithoutSubject=absent,detectionRatio=present/(present+absent) if present+absent else None,trackingFrames=report.get('trackingFrames',0),trackingGaps=report.get('trackingGapFrames',0),ambiguousFrames=report.get('ambiguousFrames',0),subjectIdChanges=report.get('subjectIdChangesFrames',0),productEligibleFrames=report.get('productEligibleFrames',0),subjectIdChangesScope='Detected ID-set changes between nonempty measured frames, including return after a gap; ephemeral spatial IDs, not biometric identities',productThresholds=dict(minCameraConfidence=.6,roiMinX=.2,roiMaxX=.8,visibilityPresence=.5))
            if preview is not None:report.update(previewEnabled=True,previewMaxBytes=65536,previewMaxWidth=320,previewMaxHeight=240,previewMaxFps=5,previewSlotCapacity=1,previewOverwritten=preview.slot.overwritten,previewErrors=preview.errors,previewThreadsJoined=all(not t.is_alive() for t in preview.threads),previewBufferCleared=preview.latest is None)
            else:report.update(previewEnabled=False,previewWasEnabled=preview_ever,previewOverwritten=preview_overwritten,previewErrors=preview_errors,previewThreadsJoined=True,previewBufferCleared=True)
            emit('CAMERA_SUMMARY',report)
        elif frames:
            values=sorted(durations);reads=sorted(read_durations)
            emit('CAMERA_SUMMARY',dict(frames=frames,retainedMetrics=len(values),fpsObserved=(len(captures)-1)*1000/(captures[-1]-captures[0]) if len(captures)>1 and captures[-1]>captures[0] else None,processingP50Ms=values[(len(values)-1)//2],processingP95Ms=values[max(0,__import__('math').ceil(len(values)*.95)-1)],captureReadP50Ms=reads[(len(reads)-1)//2],captureReadP95Ms=reads[max(0,__import__('math').ceil(len(reads)*.95)-1)],trackingFrames=tracking,trackingGaps=gaps,confidenceMin=min(confidence) if confidence else None,confidenceMax=max(confidence) if confidence else None,hardwareDroppedFrames=None,cameraReleased=capture is not None,detectorClosed=detector is not None))
if __name__=='__main__':
    try:main()
    except Exception as e:
        print(type(e).__name__+': '+str(e),file=sys.stderr);sys.exit(2)
