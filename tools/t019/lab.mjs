// Real RTC/Java/IPC/Unity lab repetition. Synthetic events, no physical-phone accreditation.
import {spawn,spawnSync} from 'node:child_process';
import {randomBytes,createHash} from 'node:crypto';
import {readFile,writeFile} from 'node:fs/promises';
import {createInterface} from 'node:readline';
import {pathToFileURL} from 'node:url';
import {resolve} from 'node:path';
import {bracket,summarize} from './clock.mjs';
const [output,player,java,jar]=process.argv.slice(2);
if(!jar)throw Error('Usage: lab.mjs new-output player java jar');
try{await readFile(output);throw Error('Preserve prior evidence');}catch(e){if(e.code!=='ENOENT')throw e;}
const {chromium}=await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));
const report={classification:'LAB / synthetic, host Chrome',physicalPhones:'NOT RUN',humanTracking:'NOT RUN',motionToVisibleResult:'NOT RUN',gate:'FAIL',initialConnections:{attempted:0,successful:0},reconnections:{attempted:0,successful:0},runs:[],utcStart:new Date().toISOString()};
const deadline=(p,ms)=>{let t;return Promise.race([p,new Promise((_,reject)=>t=setTimeout(()=>reject(Error('DEADLINE')),ms))]).finally(()=>clearTimeout(t));};
let browser;
try{
 browser=await chromium.launch({executablePath:process.env.GORILLA_TEST_CHROME,headless:true});report.browser=browser.version();
 for(let run=0;run<5;run++){
  const operator=randomBytes(32).toString('base64url');let readyResolve;
  const ready=new Promise(r=>readyResolve=r);let accepted=0;const ownedJava=new Set();
  const process=spawn(resolve(player),['-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],{env:{...globalThis.process.env,GORILLA_IPC_JAVA:resolve(java),GORILLA_IPC_JAR:resolve(jar),GORILLA_IPC_MODE:'lifecycle',GORILLA_PHONE_INPUT:'1',GORILLA_MOBILE_LAB:'1',GORILLA_MOBILE_OPERATOR:operator},stdio:['pipe','pipe','pipe']});
  let stderrBytes=0;process.stderr.on('data',b=>stderrBytes+=b.length);const exited=new Promise(r=>process.once('exit',(code,signal)=>r({code,signal})));
  createInterface({input:process.stdout}).on('line',line=>{if(line.startsWith('PHONE_LAB_HTTP_READY '))readyResolve(Number(line.split(' ').at(-1)));if(line.startsWith('PHONE_INPUT_OBSERVED '))accepted++;if(line.startsWith('{"component":"ipc-unity"'))try{const e=JSON.parse(line);if(e.event==='PROCESS_STARTED')ownedJava.add(e.pid);}catch{}});
  const contexts=[];const row={run:run+1,classification:'LAB',syntheticInputsObserved:0};report.runs.push(row);
  try{
   const port=await deadline(ready,20000),base='http://127.0.0.1:'+port,api=await browser.newContext();contexts.push(api);
   const clients=[];
   async function connect(url,resume=null){const context=await browser.newContext();contexts.push(context);const page=await context.newPage();await page.goto(base+'/mobile-lab/index.html');await page.waitForFunction(()=>typeof MobileClient==='function');await page.evaluate(async({url,resume})=>{window.client=new MobileClient({source:'synthetic'});await client.connect(url,resume);},{url,resume});return {context,page,url,identity:await page.evaluate(()=>({...client.identity}))};}
   for(let i=0;i<4;i++){report.initialConnections.attempted++;const r=await api.request.post(base+'/mobile/admission',{headers:{'X-Gorilla-Operator':operator}});if(!r.ok())throw Error('ADMISSION');const c=await connect((await r.json()).url);clients.push(c);report.initialConnections.successful++;}
   if(new Set(clients.map(c=>c.identity.playerId)).size!==4)throw Error('ASSOCIATION');row.simultaneousRtcClients=4;
   const samples=await clients[0].page.evaluate(async()=>{const rows=[];for(let i=0;i<32;i++){const sendMono=performance.now(),sendUtc=Date.now();const ack=await client.send('HEARTBEAT',{},sendUtc);const receiveMono=performance.now(),receiveUtc=Date.now();rows.push({sendMono,sendUtc,receiveMono,receiveUtc,serverUtc:ack.serverReceiveTimestamp});await new Promise(r=>setTimeout(r,50));}return rows;});
   row.clock=summarize(samples.map(bracket));
   for(let i=0;i<4;i++){
    const old=clients[i];report.reconnections.attempted++;await old.page.evaluate(()=>client.close());await old.context.close();const c=await connect(old.url,old.identity);clients[i]=c;
    if(c.identity.playerId!==old.identity.playerId||c.identity.deviceSessionId!==old.identity.deviceSessionId||c.identity.connectionEpoch!==old.identity.connectionEpoch+1)throw Error('RESUME_IDENTITY');report.reconnections.successful++;
    await c.page.evaluate(()=>client.send('MOTION_SAMPLE',normalizeMotion({acceleration:{x:client.identity.playerId,y:1,z:0}})));
   }
   await new Promise(r=>setTimeout(r,150));row.syntheticInputsObserved=accepted;if(accepted<4)throw Error('UNITY_BOUNDARY');
   for(const c of clients)await c.page.evaluate(()=>client.close());
   row.gate='PASS';
  }finally{
   for(const c of contexts)await c.close().catch(()=>{});
   const close=spawnSync('python3',['-c','import sys;sys.path.insert(0,"tools");from validate_ipc_2b_player import X11;X11().close(int(sys.argv[1]))',String(process.pid)],{timeout:15000});
   if(close.status!==0)row.closeUi='FAILED';
   try{row.exit=await deadline(exited,20000);}catch{process.kill('SIGKILL');row.exit=await exited;row.forced=true;}
   row.residualOwnedJava=await Promise.all([...ownedJava].map(async pid=>{try{await readFile('/proc/'+pid+'/status');return true;}catch{return false;}}));row.stderrBytes=stderrBytes;
   if(row.exit.code!==0||row.forced||row.residualOwnedJava.some(Boolean))row.gate='FAIL';
  }
  console.log(JSON.stringify({run:run+1,gate:row.gate,connections:report.initialConnections,reconnections:report.reconnections}));
 }
 report.gate=report.runs.every(x=>x.gate==='PASS')&&report.initialConnections.successful===20&&report.reconnections.successful===20?'PASS':'FAIL';
}catch(e){report.error=e.message;}
finally{await browser?.close();for(const name of ['initialConnections','reconnections'])report[name].successPercent=100*report[name].successful/report[name].attempted;report.utcEnd=new Date().toISOString();report.sha256={player:createHash('sha256').update(await readFile(player)).digest('hex'),jar:createHash('sha256').update(await readFile(jar)).digest('hex')};await writeFile(output,JSON.stringify(report,null,2)+'\n');}
globalThis.process.exitCode=report.gate==='PASS'?0:1;
