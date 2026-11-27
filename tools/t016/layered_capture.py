#!/usr/bin/env python3
"""Aggregate-only physical diagnostic. Raw pixels go directly from v4l2-ctl to /dev/null.

Python supervises text dequeue timestamps, never raw-stream bytes. No product presets changed.
Each invocation restores the initial mode/FPS/exposure controls even after failure.
"""
import argparse,hashlib,json,math,pathlib,re,selectors,signal,subprocess,sys,threading,time

def ctl(*args):
    return subprocess.check_output(['v4l2-ctl','-d','/dev/video0',*args],text=True)

def snapshot():
    fmt=ctl('--get-fmt-video');parm=ctl('--get-parm');controls=ctl('--list-ctrls-menus')
    width,height=map(int,re.search(r'Width/Height\s*:\s*(\d+)/(\d+)',fmt).groups())
    fourcc=re.search(r"Pixel Format\s*:\s*'([^']+)'",fmt)[1]
    fps=float(re.search(r'Frames per second:\s*([\d.]+)',parm)[1])
    values={k:int(re.search(r'(?m)^\s*'+k+r' .*? value=(-?\d+)',controls)[1]) for k in
            ['auto_exposure','exposure_time_absolute','exposure_dynamic_framerate']}
    return dict(width=width,height=height,fourcc=fourcc,fps=fps,controls=values)

def mode(width,height,fourcc,fps):
    ctl(f'--set-fmt-video=width={width},height={height},pixelformat={fourcc}',f'--set-parm={fps}')

def restore(initial):
    current=snapshot()
    if current['controls']!=initial['controls']:
        # Absolute exposure is writable only in manual mode.
        ctl('--set-ctrl=auto_exposure=1')
        ctl('--set-ctrl=exposure_time_absolute='+str(initial['controls']['exposure_time_absolute']))
        ctl('--set-ctrl=exposure_dynamic_framerate='+str(initial['controls']['exposure_dynamic_framerate']))
        ctl('--set-ctrl=auto_exposure='+str(initial['controls']['auto_exposure']))
    mode(initial['width'],initial['height'],initial['fourcc'],initial['fps'])
    after=snapshot()
    if after!=initial:raise RuntimeError('INITIAL_CAMERA_CONFIGURATION_NOT_RESTORED')
    return after

def pct(values,p):
    values=sorted(values)
    return values[math.ceil(len(values)*p)-1] if values else None

def aggregate(stamps,reads):
    elapsed=stamps[-1]-stamps[0] if len(stamps)>1 else 0
    intervals=[(b-a)*1000 for a,b in zip(stamps,stamps[1:])]
    return dict(frames=len(stamps),elapsedMonotonicSeconds=elapsed,
        fps=(len(stamps)-1)/elapsed if elapsed else None,
        intervalP50Ms=pct(intervals,.5),intervalP95Ms=pct(intervals,.95),
        readP50Ms=pct(reads,.5),readP95Ms=pct(reads,.95))

def raw(warmup,duration,buffers):
    command=['stdbuf','-oL','-eL','v4l2-ctl','-d','/dev/video0','--verbose',f'--stream-mmap={buffers}','--stream-to=/dev/null']
    child=subprocess.Popen(command,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,bufsize=1)
    selector=selectors.DefaultSelector();selector.register(child.stdout,selectors.EVENT_READ)
    stamps=[];receipts=[];sequences=[];first=None;deadline=time.monotonic()+warmup+duration+15
    try:
        while time.monotonic()<deadline:
            events=selector.select(1)
            if not events:
                if child.poll() is not None:break
                continue
            line=child.stdout.readline()
            if not line:break
            match=re.search(r'cap dqbuf:.*?seq:\s*(\d+).*?ts:\s*([\d.]+).*?ts-monotonic',line)
            if not match:continue
            stamp=float(match[2]);receipt=time.monotonic()
            if first is None:first=stamp
            if stamp-first<warmup:continue
            stamps.append(stamp);receipts.append(receipt);sequences.append(int(match[1]))
            if stamp-stamps[0]>=duration:break
    finally:
        if child.poll() is None:child.send_signal(signal.SIGINT)
        try:child.wait(timeout=3)
        except subprocess.TimeoutExpired:child.kill();child.wait()
        selector.close();child.stdout.close()
    result=aggregate(stamps,[])
    result.update(command=command,timestampSource='kernel V4L2 monotonic SOE; text only',
        receiptMonotonicSeconds=receipts[-1]-receipts[0] if len(receipts)>1 else 0,
        sequenceGaps=sum(max(0,b-a-1) for a,b in zip(sequences,sequences[1:])),childExited=True)
    if result['elapsedMonotonicSeconds']<duration:raise RuntimeError('RAW_MEASUREMENT_TOO_SHORT')
    return result

def opencv(warmup,duration,threaded=False,buffers=1):
    import cv2
    capture=cv2.VideoCapture('/dev/video0',cv2.CAP_V4L2)
    stamps=[];reads=[];start=time.monotonic();pump=None;stop=threading.Event()
    try:
        for prop,value in [(cv2.CAP_PROP_FOURCC,cv2.VideoWriter_fourcc(*'MJPG')),
            (cv2.CAP_PROP_FRAME_WIDTH,640),(cv2.CAP_PROP_FRAME_HEIGHT,480),
            (cv2.CAP_PROP_FPS,30),(cv2.CAP_PROP_BUFFERSIZE,buffers)]:capture.set(prop,value)
        value=int(capture.get(cv2.CAP_PROP_FOURCC))
        actual=dict(fourcc=''.join(chr((value>>(8*i))&255) for i in range(4)),
            width=capture.get(cv2.CAP_PROP_FRAME_WIDTH),height=capture.get(cv2.CAP_PROP_FRAME_HEIGHT),
            reportedFps=capture.get(cv2.CAP_PROP_FPS),buffer=capture.get(cv2.CAP_PROP_BUFFERSIZE),backend=capture.getBackendName())
        if threaded:
            sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]/'spikes/camera'))
            from pipeline import CapturePump,Metrics
            class Observer(Metrics):
                def event(self,name,stamp=None,duration=None):
                    if stamp is not None and stamp>=self.after:
                        if name=='capture':stamps.append(stamp/1e9)
                        elif name=='captureRead':reads.append(duration)
                    super().event(name,stamp,duration)
            metrics=Observer(warmup);pump=CapturePump(capture,stop,metrics);pump.start()
            while not stamps or stamps[-1]-stamps[0]<duration:
                if time.monotonic()-start>warmup+duration+15:raise RuntimeError('THREAD_TIMEOUT')
                if pump.slot.take(stop) is None:raise RuntimeError('THREAD_CAPTURE_STOPPED')
        else:
            while not stamps or stamps[-1]-stamps[0]<duration:
                before=time.monotonic();ok,image=capture.read();after=time.monotonic()
                if not ok:raise RuntimeError('MINIMAL_READ_FAILED')
                del image
                if after-start>=warmup:stamps.append(after);reads.append((after-before)*1000)
        result=aggregate(stamps,reads);result['actualOpenCV']=actual
        result['controlsDuringOpenCV']=snapshot()
    finally:
        if pump is not None:pump.close()
        else:capture.release()
    result.update(cameraReleased=True,captureThreadJoined=pump is None or not pump.thread.is_alive())
    return result

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--layer',choices=['raw','minimal','thread'],required=True)
    parser.add_argument('--width',type=int,default=640);parser.add_argument('--height',type=int,default=480)
    parser.add_argument('--priority',type=int);parser.add_argument('--manual-exposure',type=int)
    parser.add_argument('--buffers',type=int)
    parser.add_argument('--warmup',type=float,default=5);parser.add_argument('--duration',type=float,default=35)
    parser.add_argument('--lighting',default='current ambient; illuminance unmeasured')
    parser.add_argument('--output',type=pathlib.Path,required=True);args=parser.parse_args()
    if args.output.exists():raise ValueError('Preserve existing evidence')
    if args.duration<30:raise ValueError('Measurement requires >=30 seconds')
    initial=snapshot();result={'before':initial,'layer':args.layer,'warmupSeconds':args.warmup,'lighting':args.lighting,
        'diagnosticSourceSha256':hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()}
    try:
        mode(args.width,args.height,'MJPG',30)
        if args.priority is not None:ctl('--set-ctrl=exposure_dynamic_framerate='+str(args.priority))
        if args.manual_exposure is not None:
            ctl('--set-ctrl=auto_exposure=1');ctl('--set-ctrl=exposure_time_absolute='+str(args.manual_exposure))
        result['requested']=dict(width=args.width,height=args.height,fourcc='MJPG',fps=30,priority=args.priority,manualExposure=args.manual_exposure)
        result['returned']=snapshot()
        result['measurement']=raw(args.warmup,args.duration,args.buffers or 3) if args.layer=='raw' else opencv(args.warmup,args.duration,args.layer=='thread',args.buffers or 1)
    except Exception as error:result['error']=str(error)
    finally:
        result['afterRestore']=restore(initial);result['restoredExactly']=result['afterRestore']==initial
        args.output.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result),flush=True)
    sys.exit(1 if 'error' in result else 0)
