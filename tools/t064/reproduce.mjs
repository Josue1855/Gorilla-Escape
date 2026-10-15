// Compare a historical adapter with the working adapter using a deterministic timeout clock.
import {test} from 'node:test';import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';import {writeFile} from 'node:fs/promises';
const [ref,output]=process.argv.slice(2);
const source=spawnSync('git',['show',`${ref}:PWA/src/input/mobileClient.js`],{encoding:'utf8'});if(source.status!==0)throw Error('BASELINE_MISSING');
const {MobileClient,normalizeMotion}=await import('data:text/javascript;base64,'+Buffer.from(source.stdout).toString('base64'));
test('historical stop-and-wait reproduction',async t=>{
 t.mock.timers.enable({apis:['setTimeout']});let clock=0;const sends=[];
 const c=new MobileClient();c.state='RUNNING';c.identity={sessionId:'fixture',playerId:1,deviceSessionId:'fixture'};
 c.channels={motion:{readyState:'open',bufferedAmount:0,send(){sends.push(clock);},close(){}},control:{close(){}}};
 const settle=async()=>{for(let i=0;i<8;i++)await Promise.resolve();};
 c.motion(normalizeMotion(),0);await settle();clock=20;t.mock.timers.tick(20);c.motion(normalizeMotion(),20);await settle();
 const at20=sends.length;clock=1999;t.mock.timers.tick(1979);await settle();const at1999=sends.length;
 clock=2000;t.mock.timers.tick(1);await settle();const report={ref,source:'deterministic clock / no physical input',sendTimesMs:sends,at20,at1999,pending:c.pending.size,metrics:c.metrics};
 assert.deepEqual(sends,[0,2000]);assert.equal(at20,1);assert.equal(at1999,1);await c.close(false);
 await writeFile(output,JSON.stringify(report,null,2)+'\n');
});
