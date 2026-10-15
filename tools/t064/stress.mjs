// Wall-clock complement to deterministic unit tests. Synthetic adapter fixture, never physical Hz/E2E.
import assert from 'node:assert/strict';import {writeFile,access} from 'node:fs/promises';
import {MobileClient,normalizeMotion} from '../../PWA/src/input/mobileClient.js';
const output=process.argv[2];if(!output)throw Error('new output required');try{await access(output);throw Error('PRESERVE_EVIDENCE');}catch(e){if(e.code!=='ENOENT')throw e;}
const delay=ms=>new Promise(r=>setTimeout(r,ms));const settle=()=>new Promise(setImmediate);
const report={scope:'wall-clock synthetic fixture; no native RTC or physical sensor cadence in this harness',ackLossPercent:100,runs:[],rssStart:process.memoryUsage().rss,gate:'FAIL'};
for(const hz of [50,60,120])for(let repeat=1;repeat<=3;repeat++){
 const c=new MobileClient(),times=[];let fakePacketDrops=0,controlAck=0,maximumPending=0;
 c.state='RUNNING';c.identity={sessionId:'fixture',playerId:1,deviceSessionId:'fixture'};
 c.channels={motion:{readyState:'open',bufferedAmount:0,send(){times.push(performance.now());if(times.length%7===0)fakePacketDrops++;},close(){}},control:{readyState:'open',bufferedAmount:0,send(text){const n=JSON.parse(text);queueMicrotask(()=>{controlAck++;c.receive(JSON.stringify({...n,messageType:'ACK',payload:{}}));});},close(){}}};
 const began=performance.now();const n=Math.round(hz*2.5);
 for(let i=0;i<n;i++){
  c.motion(normalizeMotion({acceleration:{x:1,y:2,z:3}}));
  if(i%16===0){c.motion(normalizeMotion({acceleration:{x:2,y:2,z:3}}));c.motion(normalizeMotion({acceleration:{x:3,y:2,z:3}}));}
  await settle();maximumPending=Math.max(maximumPending,c.motionTelemetry.size);if(i%20===0)await c.send('HEARTBEAT',{});
  await delay(Math.max(0,began+(i+1)*1000/hz-performance.now()));
 }
 const intervals=times.slice(1).map((t,i)=>t-times[i]).sort((a,b)=>a-b);const row={targetHz:hz,repeat,durationMs:performance.now()-began,submitted:c.metrics.motionSubmitted,sent:c.metrics.motionSent,overwritten:c.metrics.overwritten,ack:c.metrics.motionAck,ackMiss:c.metrics.motionAckMiss,maximumPendingTelemetry:maximumPending,pendingControl:c.pending.size,errors:c.metrics.errors,controlAck,fakePacketDrops,actualEmissionHz:(times.length-1)*1000/(times.at(-1)-times[0]),sendIntervalP95Ms:intervals[Math.ceil(intervals.length*.95)-1],maximumSendIntervalMs:intervals.at(-1)};
 assert.equal(row.sent,n);assert.equal(row.ack,0);assert.equal(row.pendingControl,0);assert.equal(row.errors,0);assert.ok(maximumPending<=64);assert.ok(row.maximumSendIntervalMs<500,'no 2000ms ACK stall; host scheduling must also be recorded');await c.close(false);assert.equal(c.motionTelemetry.size,0);report.runs.push(row);
}
report.rssEnd=process.memoryUsage().rss;report.gate='PASS';await writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({gate:report.gate,runs:report.runs.length,totalSent:report.runs.reduce((n,r)=>n+r.sent,0),maximumIntervalMs:Math.max(...report.runs.map(r=>r.maximumSendIntervalMs))}));
