import test from 'node:test';
import assert from 'node:assert/strict';
import { SensorCapture } from './capture.js';
class Surface extends EventTarget {
  active = new Map();
  addEventListener(type, fn) { super.addEventListener(type, fn); const s = this.active.get(type) ?? new Set(); s.add(fn); this.active.set(type,s); }
  removeEventListener(type, fn) { super.removeEventListener(type,fn); this.active.get(type)?.delete(fn); }
  count(type) { return this.active.get(type)?.size ?? 0; }
  send(type, fields = {}) { const e = new Event(type); for (const [k,v] of Object.entries(fields)) Object.defineProperty(e,k,{value:v}); this.dispatchEvent(e); }
}
function fixture(patches = {}) {
  const win = new Surface(); const doc = new Surface(); const screen = new Surface();
  Object.assign(win,{ isSecureContext:true,DeviceMotionEvent:class {},DeviceOrientationEvent:class {},screen:{orientation:screen},...patches });
  screen.angle=0; doc.visibilityState='visible'; let time=0; const timers=new Map(); let next=0;
  const probe = new SensorCapture({win,doc,now:()=>time,setTimer:fn=>{timers.set(++next,fn);return next;},clearTimer:id=>timers.delete(id)});
  return {probe,win,doc,screen,timers,at:t=>{time=t;}};
}
const motion = { acceleration:{x:1,y:2,z:3},accelerationIncludingGravity:{x:0,y:0,z:9.8},rotationRate:{alpha:5,beta:6,gamma:7}, interval:20 };
test('permissions requested in same gesture; one source denied does not invent readings',async()=>{
  const calls=[]; const f=fixture({DeviceMotionEvent:{requestPermission:()=>{calls.push('motion');return Promise.resolve('granted');}},DeviceOrientationEvent:{requestPermission:()=>{calls.push('orientation');return Promise.resolve('denied');}}});
  const start=f.probe.start(); assert.deepEqual(calls,['motion','orientation']); await start;
  assert.equal(f.win.count('devicemotion'),1);assert.equal(f.win.count('deviceorientation'),0);assert.equal(f.probe.snapshot().channels.orientation.permission,'denied');f.probe.stop();
});
test('denied, missing APIs, permission exception and insecure context are explicit',async()=>{
  for(const [patch,state] of [[{DeviceMotionEvent:null,DeviceOrientationEvent:null},'UNAVAILABLE'],[{DeviceMotionEvent:{requestPermission:()=>Promise.resolve('denied')},DeviceOrientationEvent:null},'UNAVAILABLE'],[{DeviceMotionEvent:{requestPermission:()=>{throw Error();}},DeviceOrientationEvent:null},'UNAVAILABLE'],[{DeviceMotionEvent:{requestPermission:()=>Promise.reject(Error())},DeviceOrientationEvent:null},'UNAVAILABLE'],[{isSecureContext:false},'BLOCKED_INSECURE']]){
    const f=fixture(patch);assert.equal(await f.probe.start(),false);assert.equal(f.probe.state,state);assert.equal(f.win.count('devicemotion'),0);assert.equal(f.timers.size,0);
  }
});
test('actual fields, nulls and invalid numeric values are distinguished',async()=>{
  const f=fixture();await f.probe.start();f.win.send('devicemotion',motion);
  assert.equal(f.probe.snapshot().channels.motion.latest.acceleration.z,3);assert.ok(f.probe.snapshot().channels.motion.observedFields.includes('accelerationIncludingGravity.z'));f.win.send('devicemotion',{...motion,interval:-1});assert.equal(f.probe.snapshot().channels.motion.latest.intervalMs,null);
  f.win.send('devicemotion',{acceleration:{x:NaN,y:Infinity,z:'3'},rotationRate:null});
  const c=f.probe.snapshot().channels.motion;assert.equal(c.invalid,4);assert.ok(c.missing>=6);assert.deepEqual(c.observedFields,[]);assert.equal(c.latest.acceleration.z,null);f.probe.stop();
});
test('cadence is observed callback frequency; interruption does not claim sensor loss',async()=>{
  const f=fixture();await f.probe.start();for(const t of [10,30,50]){f.at(t);f.win.send('devicemotion',motion);}
  assert.equal(f.probe.snapshot().channels.motion.effectiveEventHz,50);f.at(2051);assert.equal(f.probe.snapshot().channels.motion.interrupted,true);
  f.win.send('devicemotion',motion);assert.equal(f.probe.snapshot().channels.motion.interrupted,false);f.probe.stop();
});
test('orientation angles and screen changes stay separate',async()=>{
  const f=fixture();await f.probe.start();f.screen.angle=90;f.screen.send('change');f.win.send('deviceorientation',{alpha:30,beta:-5,gamma:7,absolute:false});
  const s=f.probe.snapshot();assert.equal(s.screenAngleDegrees,90);assert.equal(s.channels.orientation.latest.angles.beta,-5);assert.equal(s.channels.orientation.latest.absolute,false);f.win.send('deviceorientation',{alpha:360,beta:180,gamma:-91});assert.equal(f.probe.snapshot().channels.orientation.invalid,3);f.probe.stop();
});
test('suspension removes listeners, resumes without counting hidden time as cadence',async()=>{
  const f=fixture();await f.probe.start();f.at(10);f.win.send('devicemotion',motion);f.at(30);f.win.send('devicemotion',motion);
  f.doc.visibilityState='hidden';f.doc.send('visibilitychange');assert.equal(f.probe.state,'SUSPENDED');assert.equal(f.win.count('devicemotion'),0);
  f.at(5000);f.win.send('devicemotion',motion);assert.equal(f.probe.channels.motion.events,2);
  f.doc.visibilityState='visible';f.doc.send('visibilitychange');f.win.send('devicemotion',motion);f.at(5020);f.win.send('devicemotion',motion);
  assert.equal(f.probe.snapshot().channels.motion.effectiveEventHz,50);f.probe.stop();
});
test('cleanup idempotent, pagehide cleans up; restart never duplicates listeners',async()=>{
  const f=fixture();await f.probe.start();assert.equal(await f.probe.start(),false);assert.equal(f.timers.size,1);f.win.send('pagehide');f.probe.stop();
  assert.equal(f.timers.size,0);assert.equal([...f.win.active.values()].reduce((n,s)=>n+s.size,0),0);assert.equal(f.doc.count('visibilitychange'),0);assert.equal(f.screen.count('change'),0);
  await f.probe.start();assert.equal(f.win.count('devicemotion'),1);f.probe.stop();
});
test('stop or restart while permission pending isolates late results',async()=>{
  let resolve;const f=fixture({DeviceMotionEvent:{requestPermission:()=>new Promise(r=>{resolve=r;})},DeviceOrientationEvent:null});
  const pending=f.probe.start();f.probe.stop();resolve('granted');assert.equal(await pending,false);assert.equal(f.probe.state,'STOPPED');assert.equal(f.timers.size,0);assert.equal(f.win.count('devicemotion'),0);
  let oldResolve;let requests=0;const newer=fixture({DeviceMotionEvent:{requestPermission:()=>++requests===1?new Promise(r=>{oldResolve=r;}):Promise.resolve('granted')},DeviceOrientationEvent:null});
  const old=newer.probe.start();newer.probe.stop();assert.equal(await newer.probe.start(),true);oldResolve('denied');assert.equal(await old,false);assert.equal(newer.probe.state,'RUNNING');assert.equal(newer.win.count('devicemotion'),1);assert.equal(newer.timers.size,1);newer.probe.stop();
});

test('native-style timers retain Window receiver and publish RUNNING before cleanup',async()=>{
  const win=new Surface(),doc=new Surface(),screen=new Surface();
  Object.assign(win,{isSecureContext:true,DeviceMotionEvent:class {},DeviceOrientationEvent:class {},screen:{orientation:screen}});
  doc.visibilityState='visible';screen.angle=0;let tick,cleared=false;const states=[];
  win.setInterval=function(fn,ms){assert.equal(this,win,'WebIDL Window receiver');assert.equal(ms,500);tick=fn;return 42;};
  win.clearInterval=function(id){assert.equal(this,win,'WebIDL Window receiver');assert.equal(id,42);cleared=true;};
  const probe=new SensorCapture({win,doc,notify:s=>states.push(s.state)});
  assert.equal(await probe.start(),true);assert.equal(states.at(-1),'RUNNING');
  tick();assert.equal(states.at(-1),'RUNNING');probe.stop();assert.equal(cleared,true);
  assert.equal(win.count('devicemotion'),0);assert.equal(probe.timer,null);
});
