// Integrated real Player + Java/IPC + replay Vision + 4 RTC clients. Aggregate-only evidence.
import {spawn,spawnSync} from 'node:child_process';import {randomBytes} from 'node:crypto';
import {createInterface} from 'node:readline';import {readFile,writeFile} from 'node:fs/promises';import {pathToFileURL} from 'node:url';import {resolve} from 'node:path';
const [output,rootArg,durationArg='1800']=process.argv.slice(2),root=resolve(rootArg),duration=Number(durationArg);
if(!output||!Number.isFinite(duration)||duration<1||duration>3600)throw Error('USAGE: new-output package seconds[1..3600]');
try{await readFile(output);throw Error('PRESERVE_EVIDENCE');}catch(e){if(e.code!=='ENOENT')throw e;}
const {chromium}=await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));const operator=randomBytes(32).toString('base64url');
const secrets=[operator];const report={classification:'LAB / real Unity Java RTC; replay Vision; synthetic motion',physical:'NOT RUN',requestedSeconds:duration,cpuTickHz:Number(spawnSync('getconf',['CLK_TCK'],{encoding:'utf8'}).stdout.trim()),gate:'FAIL',resources:[],samples:[],secretLogMatches:0};
let readyResolve,surface;const ready=new Promise(r=>readyResolve=r);const owned=new Set();
const player=spawn(root+'/GorillaEscape.x86_64',['-screen-fullscreen','0','-screen-width','1280','-screen-height','940','-logFile','-'],{env:{...process.env,GORILLA_CONNECTION_MODE:'CABIN',GORILLA_MAIN_MENU:'1',GORILLA_QA_OVERLAY:'1',GORILLA_QA_AUTOSTART_TESTS:'1',GORILLA_PHONE_INPUT:'1',GORILLA_IPC_JAVA:root+'/Java/runtime/bin/java',GORILLA_IPC_JAR:root+'/Java/server.jar',GORILLA_IPC_MODE:'lifecycle',GORILLA_MOBILE_LAB:'1',GORILLA_MOBILE_OPERATOR:operator,GORILLA_CAMERA_ENABLED:'1',GORILLA_CAMERA_LAB:'1',GORILLA_CAMERA_MODE:'replay',GORILLA_CAMERA_PYTHON:root+'/Vision/python',GORILLA_CAMERA_SCRIPT:resolve('tools/spikes/camera/worker.py'),GORILLA_CAMERA_FIXTURE:resolve('Shared/Protocol/camera/corpus-v1.json')},stdio:['pipe','pipe','pipe']});owned.add(player.pid);
const exited=new Promise(r=>player.once('exit',(code,signal)=>r({code,signal})));
createInterface({input:player.stdout}).on('line',line=>{if(secrets.some(s=>s&&line.includes(s)))report.secretLogMatches++;if(line.startsWith('QA_LAB_HTTP_READY '))readyResolve(Number(line.split(' ').at(-1)));if(line.startsWith('QA_SURFACE_SUMMARY '))surface=JSON.parse(line.slice(19));});
player.stderr.on('data',b=>{if(secrets.some(s=>s&&b.toString().includes(s)))report.secretLogMatches++;});
const delay=ms=>new Promise(r=>setTimeout(r,ms));const deadline=(p,ms)=>{let timer;return Promise.race([p,new Promise((_,j)=>timer=setTimeout(()=>j(Error('DEADLINE')),ms))]).finally(()=>clearTimeout(timer));};
async function tree(pid){const result=[];try{const text=await readFile('/proc/'+pid+'/status','utf8'),stats=Object.fromEntries(text.split('\n').filter(x=>x.includes(':')).map(x=>[x.split(':')[0],x.split(':').slice(1).join(':').trim()]));const children=(await readFile('/proc/'+pid+'/task/'+pid+'/children','utf8')).trim().split(/\s+/).filter(Boolean).map(Number);owned.add(pid);const {readdir}=await import('node:fs/promises');const raw=(await readFile('/proc/'+pid+'/stat','utf8')).split(') ').at(-1).split(' ');result.push({pid,component:stats.Name,rssKiB:parseInt(stats.VmRSS)||0,threads:Number(stats.Threads),fds:(await readdir('/proc/'+pid+'/fd')).length,cpuTicks:Number(raw[11])+Number(raw[12])});for(const child of children)result.push(...await tree(child));}catch{}return result;}
let browser;const contexts=[];
try{
 const port=await deadline(ready,30000),base='http://127.0.0.1:'+port;browser=await chromium.launch({executablePath:process.env.GORILLA_TEST_CHROME,headless:true});report.browser=browser.version();const api=await browser.newContext();contexts.push(api);
 // Product QR occupies one pending slot for its real TTL. Never reset product capacity for a lab.
 await delay(31000);const pages=[];
 for(let i=0;i<4;i++){
  const response=await api.request.post(base+'/mobile/admission',{headers:{'X-Gorilla-Operator':operator}});if(!response.ok())throw Error('ADMISSION');const url=(await response.json()).url;secrets.push(new URLSearchParams(new URL(url).hash.slice(1)).get('admission'));
  const context=await browser.newContext();contexts.push(context);const page=await context.newPage();pages.push(page);await page.goto(base+'/mobile-lab/index.html');await page.waitForFunction(()=>typeof MobileClient==='function');
  const resume=await page.evaluate(async({url,index})=>{window.client=new MobileClient({source:'synthetic'});await client.connect(url);window.produced=0;window.simulatedDrops=0;window.productionStart=performance.now();client.qaTelemetry=()=>({qaVersion:1,motionHz:null,orientationHz:null,emissionHz:client.metrics.motionSent*1000/Math.max(1,performance.now()-productionStart),rttP50:null,rttP95:null});
   if(index===0)client.channels.motion.onmessage=()=>{}; // 100% motion ACK loss; control ACK intact.
   if(index===1){const send=client.channels.motion.send.bind(client.channels.motion);client.channels.motion.send=text=>{if(++window.packet%7===0){window.simulatedDrops++;return;}send(text);};window.packet=0;}
   window.production=setInterval(()=>{window.produced++;client.motion(normalizeMotion({acceleration:{x:1,y:2,z:3}}));},20);return client.identity.resumeToken;
  },{url,index:i});secrets.push(resume);
 }
 report.utcStart=new Date().toISOString();const began=performance.now();
 const collect=async()=>{const clients=await Promise.all(pages.map(p=>p.evaluate(()=>({state:client.state,produced,simulatedDrops,pending:client.pending.size,telemetry:client.motionTelemetry.size,latest:client.latest!==null,metrics:client.metrics,rtt:undefined})).then(x=>{x.metrics={...x.metrics,rttSamples:x.metrics.rtt.length};delete x.metrics.rtt;return x;})));report.samples.push({elapsedSeconds:(performance.now()-began)/1000,clients});report.resources.push({elapsedSeconds:(performance.now()-began)/1000,processes:await tree(player.pid)});if(clients.some(c=>c.state!=='RUNNING'||c.pending>8||c.telemetry>64||c.metrics.errors>0))throw Error('CLIENT_BOUNDS_OR_ERRORS');};
 await collect();while(performance.now()-began<duration*1000){await delay(Math.min(30000,duration*1000-(performance.now()-began)));await collect();console.log(JSON.stringify({elapsedSeconds:Math.round((performance.now()-began)/1000),clients:4,gate:'RUNNING'}));}
 report.durationSeconds=(performance.now()-began)/1000;report.utcEnd=new Date().toISOString();
 for(const page of pages)await page.evaluate(async()=>{clearInterval(production);await client.close();});
 const diagnostics=await api.request.get(base+'/mobile/diagnostics',{headers:{'X-Gorilla-Operator':operator}});const state=await diagnostics.json();report.peersAfterClose=state.peers;
 report.gate=state.peers===0&&report.samples.at(-1).clients[0].metrics.motionAck===0&&report.samples.at(-1).clients[0].metrics.motionSent>=duration*20&&report.secretLogMatches===0?'PASS':'FAIL';
}catch(e){report.error=e.message;}
finally{
 for(const context of contexts)await context.close().catch(()=>{});await browser?.close();await tree(player.pid);
 spawnSync('python3',['-c','import sys;sys.path.insert(0,"tools");from validate_ipc_2b_player import X11;X11().close(int(sys.argv[1]))',String(player.pid)],{timeout:10000});
 try{report.exit=await deadline(exited,20000);}catch{player.kill('SIGKILL');report.exit=await exited;report.forced=true;}
 report.surface=surface;report.residualOwned=[];for(const pid of owned)try{process.kill(pid,0);report.residualOwned.push(pid);}catch{}
 if(report.forced||report.exit.code!==0||report.residualOwned.length||!surface?.javaCleanup||!surface?.cameraCleanup||!surface?.qaPhoneObserved||!surface?.visualApplied||report.secretLogMatches)report.gate='FAIL';
 await writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({gate:report.gate,durationSeconds:report.durationSeconds,error:report.error,residualOwned:report.residualOwned}));process.exitCode=report.gate==='PASS'?0:1;
}
