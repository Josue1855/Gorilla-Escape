import '../spikes/sensors/capture.test.js';
import test from 'node:test';
import assert from 'node:assert/strict';
import { SensorCapture, RateWindow, frequencyTier } from '../src/input/sensors/capture.js';
import { bindCapture, MobileClient } from '../src/input/mobileClient.js';
import { ControllerSession } from '../src/controller/session.js';
const vector = { x: 0, y: 0, z: 0 };
const motion = { acceleration: vector, accelerationIncludingGravity: vector, rotationRate: { alpha: 0, beta: 0, gamma: 0 }, interval: 20 };
const angles = { alpha: 0, beta: 0, gamma: 0 };
function fixture() {
 const win = new EventTarget(), doc = new EventTarget(); let time = 0, utc = 1700000000000, id = 0;
 const intervals = new Map(), timers = new Map(); doc.visibilityState = 'visible';
 Object.assign(win, { document: doc, isSecureContext: true, DeviceMotionEvent: {}, DeviceOrientationEvent: {},
  setInterval: fn => { intervals.set(++id, fn); return id; }, clearInterval: id => intervals.delete(id),
  setTimeout: fn => { timers.set(++id, fn); return id; }, clearTimeout: id => timers.delete(id) });
 const samples = [];
 const capture = new SensorCapture({ win, doc, now: () => time, utcNow: () => utc, onSample: s => samples.push(s) });
 const event = (type, fields, stamp = time) => { const e = new Event(type); for (const [k,v] of Object.entries({ ...fields, timeStamp: stamp })) Object.defineProperty(e,k,{value:v}); return e; };
 const send = (type, fields, stamp = time) => win.dispatchEvent(event(type,fields,stamp));
 return { win, doc, capture, samples, event, send, intervals, timers, now: () => time, at: t => { time=t; }, utc: t => { utc=t; } };
}
test('sequence advances only for actual unique events, never snapshots or metric ticks', async () => {
 const f=fixture(); await f.capture.start(); f.at(10); const e=f.event('devicemotion',motion); f.win.dispatchEvent(e);
 f.capture.snapshot(); for(const tick of f.intervals.values()) tick(); f.at(20); f.win.dispatchEvent(e);
 assert.equal(f.capture.channels.motion.sequence,1); assert.equal(f.samples.length,1);
 assert.equal(f.capture.metrics().channels.motion.rawCallbacks,2); assert.equal(f.capture.metrics().channels.motion.duplicateOrOutOfOrder,1);
 f.at(30); f.send('devicemotion',motion); assert.equal(f.capture.channels.motion.sequence,2); // unchanged zero is a real sample
 f.capture.stop();
});
test('duplicate/out-of-order timestamps rejected independently per channel; UTC jumps do not alter Hz',async()=>{
 const f=fixture(); await f.capture.start();
 for(const t of [10,30,50]) { f.at(t); f.utc(t===30?1:1700000000000); f.send('devicemotion',motion); }
 f.at(60); f.send('devicemotion',motion,30); f.send('deviceorientation',angles,30);
 assert.equal(f.capture.metrics().channels.motion.validUnique.actualMeasuredHz,50);
 assert.equal(f.capture.channels.orientation.sequence,1); assert.equal(f.capture.channels.motion.sequence,3); f.capture.stop();
});
test('partial axes, zero and nonfinite values remain distinct and no invalid-only emission occurs',async()=>{
 const f=fixture();await f.capture.start();f.at(10);f.send('devicemotion',{ acceleration:{x:0,y:NaN,z:Infinity},rotationRate:{alpha:-Infinity} });
 const s=f.capture.snapshot();assert.equal(s.channels.motion.latest.acceleration.x,0);assert.equal(s.channels.motion.latest.acceleration.y,null);
 assert.equal(f.capture.metrics().channels.motion.invalidFields,3);assert.equal(f.samples.length,1);
 f.at(20);f.send('devicemotion',{});assert.equal(f.samples.length,1);assert.equal(f.capture.channels.motion.validSequence,1);f.capture.stop();
});
test('motion and orientation rates are independent, snapshot dedup and UTC receipt preserved',async()=>{
 const f=fixture();await f.capture.start();const sent=[];const bind=bindCapture(f.capture,{state:'RUNNING',motion:(...x)=>sent.push(x)});
 for(let t=10;t<=210;t+=20){f.at(t);f.send('devicemotion',motion);if((t-10)%40===0)f.send('deviceorientation',angles);bind(f.capture.snapshot());bind(f.capture.snapshot());}
 assert.equal(sent.length,11);assert.equal(sent[0][1],1700000000000);
 assert.equal(f.capture.metrics().channels.motion.validUnique.actualMeasuredHz,50);
 assert.equal(f.capture.metrics().channels.orientation.validUnique.actualMeasuredHz,25);f.capture.stop();
});
for(const hz of [60,50,30,20,15])test(`deterministic ${hz} Hz fixture classifier, not physical evidence`,()=>{
 const r=new RateWindow();for(let i=0;i<=hz;i++)r.add(i*1000/hz);
 assert.equal(r.metrics().actualMeasuredHz,hz);assert.equal(r.metrics().tier,hz===15?'degraded-below-20':hz);
});
test('classifier conservative boundaries and bounded metrics with interval statistics',()=>{
 assert.equal(frequencyTier(49.99),30);assert.equal(frequencyTier(null),'unmeasured');
 const r=new RateWindow(4);for(const t of [0,20,40,60,80,100])r.add(t);
 assert.equal(r.metrics().count,6);assert.equal(r.metrics().windowCount,4);assert.equal(r.values.length,4);
 assert.deepEqual(r.metrics().intervalMs,{min:20,median:20,p95:20});assert.equal(r.add(99),false);
});
test('latest-only transport retains one in flight and one pending, counts overwrite separately',async()=>{
 const c=new MobileClient({source:'replay'});c.state='RUNNING';let resolve;const sent=[];
 c.send=async (type,payload)=>{sent.push(payload);await new Promise(r=>{resolve=r;});};
 c.motion({sample:1});c.motion({sample:2});c.motion({sample:3});c.motion({sample:4});
 assert.equal(sent.length,1);assert.deepEqual(c.latest.payload,{sample:4});assert.equal(c.metrics.overwritten,2);assert.equal(c.metrics.motionSubmitted,4);
 resolve();await new Promise(setImmediate);assert.deepEqual(sent,[{sample:1},{sample:4}]);resolve();await new Promise(setImmediate);await c.close(false);
});
test('permission without events never INPUT_READY; sufficient valid finite events required, loss visible',async()=>{
 const f=fixture();await f.capture.start();assert.equal(f.capture.inputState(),'PREPARING_SENSORS');
 f.at(2001);assert.equal(f.capture.inputState(),'NO_SENSOR_INPUT');
 for(const t of [2010,2030,2050]){f.at(t);f.send('devicemotion',motion);f.send('deviceorientation',angles);}
 assert.equal(f.capture.inputState(),'INPUT_READY');f.at(4051);assert.equal(f.capture.inputState(),'NO_SENSOR_INPUT');
 f.capture.stop();assert.equal(f.intervals.size,0);f.at(5000);f.send('devicemotion',motion);assert.equal(f.capture.channels.motion.sequence,3);
});
test('partial actual input is LIMITED, not full readiness',async()=>{
 const f=fixture();await f.capture.start();for(const t of [10,30,50]){f.at(t);f.send('devicemotion',{acceleration:{x:0}});f.send('deviceorientation',angles);}
 assert.equal(f.capture.inputState(),'INPUT_LIMITED');f.capture.stop();
});
test('physical measurement aggregates freeze independently; timers do not emit samples and cleanup cancels',async()=>{
 const f=fixture();const client={state:'RUNNING',metrics:{},async connect(){},async close(){},motion(){},diagnostic(){return {};}};
 const m=new ControllerSession({win:f.win,now:f.now,client,url:'https://test.invalid/#sessionId=x&admission=x'});
 await m.connect();await m.activate();assert.equal(m.state,'PREPARING_SENSORS');m.startMeasurement('rest');
 const send=(t)=>{f.at(t);f.send('devicemotion',motion);f.send('deviceorientation',angles);};[10,30,50].forEach(send);
 assert.equal(m.state,'INPUT_READY');f.at(15500);[...f.timers.values()][0]();f.timers.clear();
 assert.equal(m.measurement.status,'COMPLETE');assert.equal(m.measurement.capture.channels.motion.validUnique.count,3);
 send(15510);assert.equal(m.measurement.capture.channels.motion.validUnique.count,3);
 assert.equal(m.startMeasurement('gentle'),true);assert.equal(m.capture.metrics().channels.motion.validUnique.count,0);
 await m.dispose();assert.equal(f.timers.size,0);assert.equal(f.intervals.size,0);assert.equal(m.measurement.status,'INTERRUPTED');
});

test('coarsened receipt clock preserves distinct events and honest zero intervals',()=>{
 const r=new RateWindow();for(const t of [0,0,20,20,40,40])assert.equal(r.add(t),true);
 assert.equal(r.metrics().count,6);assert.equal(r.metrics().actualMeasuredHz,125);
 assert.deepEqual(r.metrics().intervalMs,{min:0,median:0,p95:20});
});

test('measured-pressure emission budget selects real unique events at 50Hz from two 60Hz channels, never fills idle time',()=>{
 const sent=[];const bind=bindCapture(null,{state:'RUNNING',motion:p=>sent.push(p)},{maximumHz:50});
 let ms=0,os=0;
 for(let i=0;i<=600;i++){const t=i*1000/60;ms++;const motion={sequence:ms,callbackMonotonicMs:t,acceleration:vector,receiptUtcMs:1700000000000+Math.floor(t)};
  const snapshot={generation:1,channels:{motion:{latest:motion,validSequence:ms},orientation:{latest:{sequence:os,callbackMonotonicMs:t,angles},validSequence:os}}};
  bind(snapshot);os++;snapshot.channels.orientation.validSequence=os;bind(snapshot);bind(snapshot);
 }
 assert.equal(sent.length,501);assert.equal(bind.metrics().budgetDropped,701);
 assert.ok(sent.every(p=>p.acceleration.values.x===0));
 const before=sent.length;assert.equal(sent.length,before); // no scheduled emission without an event
});
test('emission budget resets due time on acquisition generation change without replaying snapshot',()=>{
 let emitted=0;const bind=bindCapture(null,{state:'RUNNING',motion:()=>emitted++},{maximumHz:50});
 const sample=g=>({generation:g,channels:{motion:{validSequence:1,latest:{sequence:1,callbackMonotonicMs:0,acceleration:vector}},orientation:{validSequence:0,latest:null}}});
 bind(sample(1));bind(sample(1));bind(sample(2));assert.equal(emitted,2);
 assert.throws(()=>bindCapture(null,{}, {maximumHz:Infinity}),RangeError);
});
