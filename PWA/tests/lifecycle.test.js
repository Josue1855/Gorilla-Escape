import test from 'node:test';
import assert from 'node:assert/strict';
import { ControllerSession } from '../src/controller/session.js';
function setup() {
 let clock=0,id=0; const timers=new Map(), intervals=new Map(), listeners=new Map();
 const target=()=>{const t=new EventTarget(),add=t.addEventListener.bind(t),remove=t.removeEventListener.bind(t);
 t.addEventListener=(name,fn)=>{const key=t===win?'win:'+name:'doc:'+name;let set=listeners.get(key);if(!set)listeners.set(key,set=new Set());set.add(fn);add(name,fn);};
 t.removeEventListener=(name,fn)=>{listeners.get((t===win?'win:':'doc:')+name)?.delete(fn);remove(name,fn);};return t;};
 const win=target(),doc=target();doc.visibilityState='visible';
 Object.assign(win,{document:doc,isSecureContext:true,DeviceMotionEvent:{},DeviceOrientationEvent:{},setTimeout:fn=>{timers.set(++id,fn);return id;},clearTimeout:i=>timers.delete(i),setInterval:fn=>{intervals.set(++id,fn);return id;},clearInterval:i=>intervals.delete(i)});
 const client={state:'STOPPED',metrics:{motionSubmitted:0},latest:null,async connect(){this.state='RUNNING';},async close(){this.state='STOPPED';},motion(){this.metrics.motionSubmitted++;},diagnostic(){return {};}};
 const model=new ControllerSession({win,doc,client,now:()=>clock,url:'http://localhost/#sessionId=fixture&admission=fixture'});
 const send=()=>{clock+=25;for(const [type,fields] of [['devicemotion',{acceleration:{x:0,y:0,z:0},accelerationIncludingGravity:{x:0,y:0,z:9.81},rotationRate:{alpha:0,beta:0,gamma:0},interval:20}],['deviceorientation',{alpha:0,beta:0,gamma:0}]]){const e=new Event(type);Object.assign(e,fields);Object.defineProperty(e,'timeStamp',{value:clock});win.dispatchEvent(e);}};
 const visible=value=>{doc.visibilityState=value;doc.dispatchEvent(new Event('visibilitychange'));};
 return {win,doc,client,model,timers,intervals,listeners,send,visible};
}
async function ready(f){await f.model.connect();await f.model.activate();for(let i=0;i<3;i++)f.send();assert.equal(f.model.state,'INPUT_READY');}
test('hidden immediately suspends, detaches sensors, discards pending input and interrupts measurement',async()=>{
 const f=setup();await ready(f);f.model.startMeasurement('rest');f.client.latest={old:true};f.visible('hidden');
 assert.equal(f.model.state,'CONTROL_SUSPENDED');assert.equal(f.model.measurement.status,'INTERRUPTED');assert.equal(f.timers.size,0);assert.equal(f.client.latest,null);
 assert.equal(f.listeners.get('win:devicemotion').size,0);assert.equal(f.listeners.get('win:deviceorientation').size,0);
 const submitted=f.client.metrics.motionSubmitted;f.send();assert.equal(f.client.metrics.motionSubmitted,submitted);await f.model.dispose();
});
test('short suspension resumes only after three fresh events per channel, never stale readiness',async()=>{
 const f=setup();await ready(f);f.visible('hidden');f.visible('visible');assert.equal(f.model.state,'PREPARING_SENSORS');
 assert.equal(f.model.capture.channels.motion.latest,null);f.send();f.send();assert.equal(f.model.state,'PREPARING_SENSORS');f.send();assert.equal(f.model.state,'INPUT_READY');await f.model.dispose();
});
test('repeated hide/show keeps exactly one listener per channel and duplicate visibility notifications are inert',async()=>{
 const f=setup();await ready(f);const runningTimers=f.intervals.size;for(let i=0;i<8;i++){
  f.visible('hidden');f.visible('hidden');
  assert.equal(f.model.state,'CONTROL_SUSPENDED');
  assert.equal(f.listeners.get('win:devicemotion').size,0);assert.equal(f.listeners.get('win:deviceorientation').size,0);
  assert.equal(f.intervals.size,runningTimers);assert.equal(f.timers.size,0);
  f.visible('visible');f.visible('visible');assert.equal(f.model.state,'PREPARING_SENSORS');
  assert.equal(f.listeners.get('win:devicemotion').size,1);assert.equal(f.listeners.get('win:deviceorientation').size,1);
  assert.equal(f.intervals.size,runningTimers);for(let j=0;j<3;j++)f.send();
 }
 assert.equal(f.listeners.get('win:devicemotion').size,1);assert.equal(f.listeners.get('win:deviceorientation').size,1);assert.equal(f.model.state,'INPUT_READY');await f.model.dispose();assert.equal(f.intervals.size,0);assert.equal(f.timers.size,0);
});
test('transport lost while hidden remains DISCONNECTED after return; no stale emission/reconnect',async()=>{
 const f=setup();await ready(f);f.visible('hidden');f.client.state='DISCONNECTED';for(const tick of [...f.intervals.values()])tick();f.visible('visible');f.send();
 assert.equal(f.model.state,'DISCONNECTED');assert.equal(f.model.capture.state,'STOPPED');await f.model.dispose();
});
test('pagehide disposes capture, timers, monitor, pending measurement and owned transport',async()=>{
 const f=setup();await ready(f);f.model.startMeasurement('gentle');f.win.dispatchEvent(new Event('pagehide'));
 assert.equal(f.model.state,'DISCONNECTED');assert.equal(f.model.capture.state,'STOPPED');assert.equal(f.model.measurement.status,'INTERRUPTED');assert.equal(f.timers.size,0);assert.equal(f.intervals.size,0);assert.equal(f.client.state,'STOPPED');
 for(const set of f.listeners.values())assert.equal(set.size,0);const count=f.client.metrics.motionSubmitted;f.send();assert.equal(f.client.metrics.motionSubmitted,count);
});

test('repeated disposal remains promise-compatible with React cleanup after pagehide',async()=>{
 const f=setup();await ready(f);f.win.dispatchEvent(new Event('pagehide'));const second=f.model.dispose();assert.equal(typeof second.then,'function');await second;assert.equal(f.model.state,'DISCONNECTED');assert.equal(f.intervals.size,0);
});
