#!/usr/bin/env python3
"""Linux/X11 development acceptance. Drives explicit Retry/Stop keyboard controls; only signals recorded owned children.
Raw logs stay in ignored Unity/Logs. JSON summaries contain no launch tokens or environment dumps.
Requires an already-built Linux development Player, JAR and helper fixture JARs.
"""
import argparse
import ctypes as C
import json
import os
from pathlib import Path
import signal
import subprocess
import time

class X11:
    def __init__(self):
        self.x=C.CDLL('libX11.so.6'); self.xt=C.CDLL('libXtst.so.6')
        self.x.XOpenDisplay.restype=C.c_void_p; self.display=self.x.XOpenDisplay(None)
        if not self.display: raise RuntimeError('X11 display required')
        self.x.XDefaultRootWindow.argtypes=[C.c_void_p]; self.x.XDefaultRootWindow.restype=C.c_ulong
        self.root=self.x.XDefaultRootWindow(self.display)
        self.x.XInternAtom.argtypes=[C.c_void_p,C.c_char_p,C.c_int]; self.x.XInternAtom.restype=C.c_ulong
        self.pid_atom=self.x.XInternAtom(self.display,b'_NET_WM_PID',0)
        self.x.XQueryTree.argtypes=[C.c_void_p,C.c_ulong,C.POINTER(C.c_ulong),C.POINTER(C.c_ulong),C.POINTER(C.POINTER(C.c_ulong)),C.POINTER(C.c_uint)]
        self.x.XGetWindowProperty.argtypes=[C.c_void_p,C.c_ulong,C.c_ulong,C.c_long,C.c_long,C.c_int,C.c_ulong,C.POINTER(C.c_ulong),C.POINTER(C.c_int),C.POINTER(C.c_ulong),C.POINTER(C.c_ulong),C.POINTER(C.c_void_p)]
        self.x.XFree.argtypes=[C.c_void_p]
        self.x.XTranslateCoordinates.argtypes=[C.c_void_p,C.c_ulong,C.c_ulong,C.c_int,C.c_int,C.POINTER(C.c_int),C.POINTER(C.c_int),C.POINTER(C.c_ulong)]
        self.x.XFlush.argtypes=[C.c_void_p]
        self.x.XRaiseWindow.argtypes=[C.c_void_p,C.c_ulong]
        self.x.XSetInputFocus.argtypes=[C.c_void_p,C.c_ulong,C.c_int,C.c_ulong]
        self.xt.XTestFakeMotionEvent.argtypes=[C.c_void_p,C.c_int,C.c_int,C.c_int,C.c_ulong]
        self.xt.XTestFakeButtonEvent.argtypes=[C.c_void_p,C.c_uint,C.c_int,C.c_ulong]
    def windows(self,root=None,depth=0):
        root=self.root if root is None else root
        if depth>5: return []
        r=C.c_ulong();p=C.c_ulong();children=C.POINTER(C.c_ulong)();n=C.c_uint()
        if not self.x.XQueryTree(self.display,root,C.byref(r),C.byref(p),C.byref(children),C.byref(n)): return []
        found=[children[i] for i in range(n.value)]
        if children: self.x.XFree(children)
        result=list(found)
        for child in found: result.extend(self.windows(child,depth+1))
        return result
    def owner(self,window):
        t=C.c_ulong();fmt=C.c_int();n=C.c_ulong();left=C.c_ulong();data=C.c_void_p()
        self.x.XGetWindowProperty(self.display,window,self.pid_atom,0,1,0,0,C.byref(t),C.byref(fmt),C.byref(n),C.byref(left),C.byref(data))
        value=C.cast(data,C.POINTER(C.c_ulong))[0] if data and n.value else None
        if data: self.x.XFree(data)
        return value
    def window(self,pid):
        return wait(lambda:next((w for w in self.windows() if self.owner(w)==pid),None),10)
    def click(self,pid,y):
        window=self.window(pid); x=C.c_int(); yy=C.c_int();child=C.c_ulong()
        self.x.XRaiseWindow(self.display,window); self.x.XSetInputFocus(self.display,window,1,0); self.x.XFlush(self.display)
        self.x.XTranslateCoordinates(self.display,window,self.root,120,y,C.byref(x),C.byref(yy),C.byref(child))
        self.xt.XTestFakeMotionEvent(self.display,-1,x.value,yy.value,0)
        self.xt.XTestFakeButtonEvent(self.display,1,1,0);self.x.XFlush(self.display)
        time.sleep(.08)  # Mouse press duration, never readiness.
        self.xt.XTestFakeButtonEvent(self.display,1,0,0);self.x.XFlush(self.display)
    def key(self,pid,name):
        window=self.window(pid)
        self.x.XRaiseWindow(self.display,window)
        self.x.XSetInputFocus(self.display,window,1,0)
        self.x.XFlush(self.display)
        self.x.XKeysymToKeycode.argtypes=[C.c_void_p,C.c_ulong]; self.x.XKeysymToKeycode.restype=C.c_uint
        self.xt.XTestFakeKeyEvent.argtypes=[C.c_void_p,C.c_uint,C.c_int,C.c_ulong]
        code=self.x.XKeysymToKeycode(self.display,ord(name))
        self.xt.XTestFakeKeyEvent(self.display,code,1,0);self.x.XFlush(self.display)
        time.sleep(.08)
        self.xt.XTestFakeKeyEvent(self.display,code,0,0);self.x.XFlush(self.display)
    def action(self,player,y,event,generation=1):
        # Xwayland may consume a focus event. Repeat the explicit UI action until its event,
        # not an application retry policy; active-state guards prevent duplicate launches.
        for attempt in range(4):
            self.key(player.player.pid,'r' if y==78 else 's')
            try:return player.event(event,generation,seconds=1)
            except TimeoutError:pass
        raise TimeoutError('UI action was not acknowledged: '+event)
    def close(self,pid):
        class Data(C.Union): _fields_=[('b',C.c_char*20),('s',C.c_short*10),('l',C.c_long*5)]
        class Message(C.Structure):
            _fields_=[('type',C.c_int),('serial',C.c_ulong),('send_event',C.c_int),('display',C.c_void_p),('window',C.c_ulong),('message_type',C.c_ulong),('format',C.c_int),('data',Data)]
        message=Message();message.type=33;message.display=self.display;message.window=self.window(pid)
        message.message_type=self.x.XInternAtom(self.display,b'WM_PROTOCOLS',0);message.format=32
        message.data.l[0]=self.x.XInternAtom(self.display,b'WM_DELETE_WINDOW',0)
        self.x.XSendEvent.argtypes=[C.c_void_p,C.c_ulong,C.c_int,C.c_long,C.c_void_p]
        self.x.XSendEvent(self.display,message.window,0,0,C.byref(message));self.x.XFlush(self.display)

def wait(condition,seconds=25):
    end=time.monotonic()+seconds
    while time.monotonic()<end:
        value=condition()
        if value: return value
        time.sleep(.025)  # Poll an observable signal with a deadline, not a fixed readiness sleep.
    raise TimeoutError('Expected observable signal did not arrive')

class Player:
    def __init__(self,args,name,jar=None,smoke=False,mode=None,measure=False):
        self.path=args.output/(name+'.log');self.path.unlink(missing_ok=True);self.children=[];self.listenerEvidence={};self.observed={}
        env=os.environ.copy();env.update(GORILLA_IPC_JAVA=str(args.java),GORILLA_IPC_JAR=str(jar or args.jar))
        if smoke: env['GORILLA_IPC_SMOKE_EXIT']='1';env.pop('GORILLA_IPC_MODE',None)
        else: env['GORILLA_IPC_MODE']='lifecycle';env.pop('GORILLA_IPC_SMOKE_EXIT',None)
        if mode is not None: env["GORILLA_IPC_MODE"]=mode
        if measure: env["GORILLA_IPC_MEASURE"]="1"
        self.started=time.monotonic()
        self.player=subprocess.Popen([str(args.player),'-screen-width','800','-screen-height','450','-screen-fullscreen','0','-force-glcore','-logFile',str(self.path)],env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    def events(self):
        events=[]
        for line in self.path.read_text(errors='replace').splitlines() if self.path.exists() else []:
            if line.startswith('{"component":"ipc-unity"'):
                e=json.loads(line);events.append(e)
                if e['event']=='PROCESS_STARTED' and e['pid'] not in self.children:self.children.append(e['pid'])
        return events
    def event(self,name,generation=1,seconds=25):
        return wait(lambda:next((e for e in self.events() if e['event']==name and e['generation']==generation),None),seconds)
    def verify_listeners(self,e):
        pid=e['pid'];inodes=set()
        for fd in Path('/proc',str(pid),'fd').iterdir():
            try: target=os.readlink(fd)
            except FileNotFoundError:continue
            if target.startswith('socket:['):inodes.add(target[8:-1])
        listeners=[]
        for family in ['tcp','tcp6']:
            for line in Path('/proc',str(pid),'net',family).read_text().splitlines()[1:]:
                fields=line.split()
                if fields[3]=='0A' and fields[9] in inodes:
                    assert family=='tcp' and fields[1].split(':')[0]=='0100007F', 'Non-loopback child listener'
                    listeners.append(dict(family=family,address='127.0.0.1',port=int(fields[1].split(':')[1],16)))
        assert len(listeners)==2,listeners
        self.listenerEvidence[str(pid)]=listeners
    def signal_child(self,e,which):
        pid=e['pid'];status=Path('/proc')/str(pid)/'status'
        assert status.exists() and f'PPid:\t{self.player.pid}\n' in status.read_text(), 'Not this Player child'
        descriptor=os.pidfd_open(pid)
        try: signal.pidfd_send_signal(descriptor,which)
        finally:os.close(descriptor)
    def residuals(self):
        self.events();return [p for p in self.children if Path('/proc',str(p)).exists()]
    def close(self,x):
        x.close(self.player.pid);code=self.player.wait(timeout=20)
        assert code==0,code;assert not self.residuals(),self.residuals()
    def record(self):
        return dict(playerExit=self.player.poll(),children=len(self.children),residuals=self.residuals(),events=self.events(),elapsedSeconds=round(time.monotonic()-self.started,3),listeners=self.listenerEvidence,observed=self.observed)
    def emergency_cleanup(self):
        # Only this harness's Player and its verified direct children, never a global Java search.
        for e in self.events():
            if e['event']=='PROCESS_STARTED' and Path('/proc',str(e['pid'])).exists():
                try:self.signal_child(e,signal.SIGCONT)
                except (ProcessLookupError,AssertionError):pass
        if self.player.poll() is None:
            self.player.terminate()
            try:self.player.wait(timeout=15)
            except subprocess.TimeoutExpired:self.player.kill();self.player.wait(timeout=2)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--player',type=Path,required=True);parser.add_argument('--java',type=Path,required=True)
    parser.add_argument('--jar',type=Path,required=True);parser.add_argument('--fixtures',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args();args.output.mkdir(parents=True,exist_ok=True);x=X11();results={};players=[]
    def launch(name,**kw):
        p=Player(args,name,**kw);players.append(p);return p
    try:
        p=launch('smoke',smoke=True);assert p.player.wait(timeout=30)==0
        p.events();assert not p.residuals()
        smoke=next(json.loads(l.split('IPC_2A_SMOKE ',1)[1]) for l in p.path.read_text().splitlines() if l.startswith('IPC_2A_SMOKE '))
        assert smoke['samples']==100 and smoke['errors']==0 and smoke['cleanExit'];results['regression2A']=dict(smoke=smoke,**p.record())
        p=launch('death-retry-limit')
        for generation in range(1,4):
            running=p.event('FIRST_PONG',generation);p.verify_listeners(running);p.signal_child(running,signal.SIGKILL)
            failed=p.event('FAILED',generation);assert failed['errorCode']=='JAVA_EXITED' and failed['cleanupComplete'];assert not p.residuals()
            if generation<3:x.action(p,78,'START_REQUESTED',generation+1)
        assert failed['launches']==3;x.key(p.player.pid,'r')
        end=time.monotonic()+2
        while time.monotonic()<end:assert len([e for e in p.events() if e['event']=='PROCESS_STARTED'])==3;time.sleep(.025)
        p.close(x);results['deathManualRecoveryLimit']=p.record()
        p=launch('cancel-startup');p.event('PROCESS_STARTED')
        # WM close is an explicit normal Unity shutdown. Unlike a key before the
        # first focused frame, it reliably exercises RequestStop during startup.
        p.close(x);p.event('STOPPED')
        assert not any(e['event']=='FIRST_PONG' for e in p.events()),'Startup cancel happened too late'
        results['cancelStartup']=p.record()
        p=launch('cancel-running');p.event('FIRST_PONG');x.action(p,102,'STOP_REQUESTED')
        p.event('STOPPED');assert not p.residuals();p.close(x);results['cancelRunning']=p.record()
        p=launch('quit-running');p.verify_listeners(p.event('FIRST_PONG'));p.close(x);results['normalQuitEof']=p.record()
        a=launch('singleton-owner');a.event('FIRST_PONG');b=launch('singleton-second')
        failed=b.event('FAILED');assert failed['errorCode']=='ALREADY_RUNNING';assert not b.residuals()
        assert a.player.poll() is None and Path('/proc',str(a.children[0])).exists();b.close(x);a.close(x)
        results['singleton']=dict(owner=a.record(),second=b.record())
        p=launch('no-ready',jar=args.fixtures/'no-ready.jar');p.event('PROCESS_STARTED');ready_start=time.monotonic()
        assert p.event('FAILED',seconds=25)['errorCode']=='READY_TIMEOUT'
        p.observed['readyTimeoutAndCleanupMs']=round((time.monotonic()-ready_start)*1000,3)
        assert not p.residuals();p.close(x);results['readyTimeoutFixture']=p.record()
        p=launch('connect-refused',jar=args.fixtures/'connect-refused.jar');assert p.event('FAILED')['errorCode']=='CONNECT_FAILED'
        assert not p.residuals();p.close(x);results['connectRefusedFixture']=p.record()
        p=launch('unresponsive-java');running=p.event('FIRST_PONG');cut=time.monotonic();p.signal_child(running,signal.SIGSTOP)
        p.event('STOP_REQUESTED',seconds=6);p.observed['livenessLossDetectedMs']=round((time.monotonic()-cut)*1000,3);p.signal_child(running,signal.SIGCONT)
        failed=p.event('FAILED');assert failed['errorCode']=='CONNECTION_LOST';assert not failed['forced'];p.close(x)
        results['pongDeadlineRealJava']=p.record()
        p=launch('forced-owned-shutdown');running=p.event('FIRST_PONG');p.signal_child(running,signal.SIGSTOP)
        failed=p.event('FAILED',seconds=20);assert failed['errorCode']=='CONNECTION_LOST' and failed['forced'];assert 10000<=failed['shutdownMs']<12500
        assert not p.residuals();p.close(x);results['forcedOwnedShutdown']=p.record()
        p=launch('abrupt-parent-eof');running=p.event('FIRST_PONG');p.player.kill();p.player.wait(timeout=2)
        wait(lambda:not Path('/proc',str(running['pid'])).exists(),12);p.observed['javaExitCode']='NOT_OBSERVABLE_AFTER_PARENT_CRASH';results['abruptParentEof']=p.record()
    finally:
        for p in players:p.emergency_cleanup()
        (args.output/'player-results.json').write_text(json.dumps(results,indent=2)+'\n')
    assert len(results)==11,len(results)
    print('PASS: 11 Player scenario groups; own residuals 0. Fixture cases explicitly labelled.')

if __name__=='__main__':main()
