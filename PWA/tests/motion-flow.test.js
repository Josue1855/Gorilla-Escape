import test from 'node:test';
import assert from 'node:assert/strict';
import {MobileClient,normalizeMotion} from '../src/input/mobileClient.js';
const settle=async()=>{for(let i=0;i<8;i++)await Promise.resolve();};
const sample=normalizeMotion({acceleration:{x:1,y:2,z:3}});
function fixture(ack=false){
 const client=new MobileClient({fetcher:()=>{}}),sent=[];
 client.state='RUNNING';client.identity={sessionId:'session',playerId:1,deviceSessionId:'device'};
 client.channels={motion:{readyState:'open',bufferedAmount:0,send(text){const n=JSON.parse(text);sent.push(n);if(ack)queueMicrotask(()=>client.receive(JSON.stringify({...n,messageType:'ACK',payload:{}})));},close(){}},control:{readyState:'open',bufferedAmount:0,send(text){const n=JSON.parse(text);queueMicrotask(()=>client.receive(JSON.stringify({...n,messageType:'ACK',payload:{}})));},close(){}}};
 return {client,sent};
}
test('motion delivered + ACK delivered',async()=>{const {client,sent}=fixture(true);client.motion(sample);await settle();assert.equal(sent.length,1);assert.equal(client.metrics.motionAck,1);await client.close(false);});
for(const loss of ['motion packet lost','motion delivered but ACK lost'])test(loss+' does not hold next send for 2000ms',async t=>{
 t.mock.timers.enable({apis:['setTimeout']});const {client,sent}=fixture();client.motion(sample,100);await settle();
 t.mock.timers.tick(20);client.motion(sample,120);await settle();
 assert.equal(sent.length,2,'new motion must send after 20ms without waiting for ACK deadline');assert.equal(client.pending.size,0);await client.close(false);
});
test('burst is latest-only and bounded while ACK absent; control remains reliable',async()=>{
 const {client,sent}=fixture();client.motion(sample,1);await settle();for(let i=2;i<=1001;i++)client.motion(sample,i);await settle();
 assert.equal(sent.at(-1).clientTimestamp,1001);assert.equal(sent.length,2);assert.ok(client.metrics.overwritten>=999);assert.ok(client.motionTelemetry.size<=64);assert.equal(client.pending.size,0);
 await client.send('HEARTBEAT',{});assert.equal(client.pending.size,0);await client.close(false);
});
for(const hz of [50,60,120])test(`${hz}Hz source x 3 runs, 100% ACK loss, bounded telemetry`,async()=>{
 for(let run=0;run<3;run++){const {client,sent}=fixture();for(let i=0;i<hz*10;i++){client.motion(sample,Math.round(i*1000/hz));await settle();}assert.equal(sent.length,hz*10);assert.equal(client.pending.size,0);assert.ok(client.motionTelemetry.size<=64);assert.ok(client.metrics.motionAckMiss>=sent.length-64);await client.close(false);assert.equal(client.motionTelemetry.size,0);}
});
test('buffer pressure drops latest instead of queuing history; closed motion leaves control intact',async()=>{
 const {client,sent}=fixture();client.channels.motion.bufferedAmount=8193;client.motion(sample);await settle();assert.equal(sent.length,0);assert.equal(client.metrics.motionBufferedDrop,1);
 client.channels.motion.bufferedAmount=0;client.motion(sample);await settle();assert.equal(sent.length,1);client.channels.motion.readyState='closed';client.motion(sample);await settle();await client.send('HEARTBEAT',{});assert.equal(sent.length,1);await client.close(false);
});
test('duplicate, out-of-order, invalid and old identity ACKs cannot advance input',async()=>{
 const {client,sent}=fixture();for(let i=0;i<3;i++){client.motion(sample);await settle();}
 const ack=n=>client.receive(JSON.stringify({...n,messageType:'ACK',payload:{}}));
 ack({...sent[0],deviceSessionId:'old'});assert.equal(client.metrics.motionAck,0);ack(sent[2]);ack(sent[0]);ack(sent[2]);assert.equal(client.metrics.motionAck,2);
 await client.close(false);ack(sent[1]);assert.equal(client.metrics.motionAck,2);client.motion(sample);await settle();assert.equal(client.latest,null);
});
