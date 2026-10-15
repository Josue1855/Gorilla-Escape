// Real QA Player + real RTC; deterministic synthetic sensor input. Never captures its screen.
import {spawn,spawnSync} from 'node:child_process';
import {randomBytes} from 'node:crypto';
import {createInterface} from 'node:readline';
import {readFile,writeFile} from 'node:fs/promises';
import {pathToFileURL} from 'node:url';
const [output,player,java,jar]=process.argv.slice(2);
const {chromium}=await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));
const auth=randomBytes(32).toString('base64url');const secrets=[auth];let tokenLogs=0,portResolve,summaryResolve;
const ready=new Promise(r=>portResolve=r),summary=new Promise(r=>summaryResolve=r);
const report={classification:'LAB / synthetic host Chrome',physicalPhone:'NOT RUN',gate:'FAIL',screenshots:0,images:0,initial:0,reconnect:0};
const child=spawn(player,['-screen-fullscreen','0','-screen-width','1280','-screen-height','900','-logFile','-'],{env:{...process.env,GORILLA_QA_OVERLAY:'1',GORILLA_QA_AUTOSTART_TESTS:'1',GORILLA_PHONE_INPUT:'1',GORILLA_IPC_JAVA:java,GORILLA_IPC_JAR:jar,GORILLA_IPC_MODE:'lifecycle',GORILLA_MOBILE_LAB:'1',GORILLA_MOBILE_OPERATOR:auth,GORILLA_CAMERA_ENABLED:'1',GORILLA_CAMERA_LAB:'1',GORILLA_CAMERA_MODE:'replay',GORILLA_CAMERA_PYTHON:'/usr/bin/python3',GORILLA_CAMERA_SCRIPT:process.cwd()+'/tools/spikes/camera/worker.py',GORILLA_CAMERA_FIXTURE:process.cwd()+'/Shared/Protocol/camera/corpus-v1.json'},stdio:['pipe','pipe','pipe']});
const exit=new Promise(r=>child.once('exit',(code,signal)=>r({code,signal})));
createInterface({input:child.stdout}).on('line',line=>{if(secrets.some(s=>line.includes(s)))tokenLogs++;if(line.startsWith('QA_LAB_HTTP_READY '))portResolve(Number(line.split(' ').at(-1)));if(line.startsWith('QA_SURFACE_SUMMARY '))summaryResolve(JSON.parse(line.slice(19)));});child.stderr.on('data',b=>{if(secrets.some(s=>b.toString().includes(s)))tokenLogs++;});
const deadline=(p,ms)=>{let timer;return Promise.race([p,new Promise((_,rej)=>timer=setTimeout(()=>rej(Error('TIMEOUT')),ms))]).finally(()=>clearTimeout(timer));};
let browser;const contexts=[];
try{
 const port=await deadline(ready,30000);browser=await chromium.launch({executablePath:process.env.GORILLA_TEST_CHROME,headless:true});const base='http://127.0.0.1:'+port,api=await browser.newContext();contexts.push(api);
 const response=await api.request.post(base+'/mobile/admission',{headers:{'X-Gorilla-Operator':auth}});if(!response.ok())throw Error('ADMISSION');const url=(await response.json()).url;secrets.push(new URLSearchParams(new URL(url).hash.slice(1)).get('admission'));
 async function connect(resume,joinUrl=url){const context=await browser.newContext();contexts.push(context);const page=await context.newPage();await page.goto(base+'/mobile-lab/index.html');await page.waitForFunction(()=>typeof MobileClient==='function');await page.evaluate(async({url,resume})=>{window.client=new MobileClient({source:'synthetic'});await client.connect(url,resume);},{url:joinUrl,resume});return page;}
 let page=await connect(null);report.initial++;
 let id=await page.evaluate(()=>({...client.identity}));secrets.push(id.resumeToken);
 await page.evaluate(async()=>{for(let i=0;i<20;i++){await client.send('MOTION_SAMPLE',normalizeMotion({acceleration:{x:10,y:-5,z:0}}));await new Promise(r=>setTimeout(r,50));}});
 await page.evaluate(()=>client.close());await page.context().close();page=await connect(id);report.reconnect++;const resumed=await page.evaluate(()=>({...client.identity}));secrets.push(resumed.resumeToken);
 if(resumed.connectionEpoch!==id.connectionEpoch+1)throw Error('EPOCH');
 await page.evaluate(async()=>{for(let i=0;i<20;i++){await client.send('MOTION_SAMPLE',normalizeMotion({acceleration:{x:-10,y:5,z:0}}));await new Promise(r=>setTimeout(r,50));}});
 await page.evaluate(async()=>{await client.send('HEARTBEAT',{qaVersion:1,motionHz:20,orientationHz:null,emissionHz:20,rttP50:2,rttP95:5});await new Promise(r=>setTimeout(r,500));});
 // Let the unused UI-created admission expire; no test-only reset of product session.
 await new Promise(r=>setTimeout(r,31000));
 for(let i=0;i<3;i++){
  const response=await api.request.post(base+'/mobile/admission',{headers:{'X-Gorilla-Operator':auth}});
  if(!response.ok())throw Error('FOUR_PLAYER_ADMISSION');const extraUrl=(await response.json()).url;
  secrets.push(new URLSearchParams(new URL(extraUrl).hash.slice(1)).get('admission'));
  const extra=await connect(null,extraUrl);report.initial++;
  const identity=await extra.evaluate(()=>({...client.identity}));secrets.push(identity.resumeToken);
  await extra.evaluate(async()=>{await client.send('MOTION_SAMPLE',normalizeMotion({acceleration:{x:2,y:3,z:0}}));await client.send('HEARTBEAT',{qaVersion:1,motionHz:20,orientationHz:null,emissionHz:20,rttP50:2,rttP95:5});});
 }
 await new Promise(r=>setTimeout(r,1500));

}catch(e){report.error=e.message;}
finally{
 for(const context of contexts)await context.close().catch(()=>{});await browser?.close();spawnSync('python3',['-c','import sys;sys.path.insert(0,"tools");from validate_ipc_2b_player import X11;X11().close(int(sys.argv[1]))',String(child.pid)],{timeout:10000});
 try{report.exit=await deadline(exit,20000);report.surface=await deadline(summary,1000);}catch(e){child.kill('SIGKILL');report.forced=true;report.error=e.message;}
 report.tokenLogs=tokenLogs;if(!report.error&&!report.forced&&report.exit.code===0&&report.surface.visualApplied&&report.surface.phoneAccepted>=40&&report.surface.javaCleanup&&report.surface.cameraCleanup&&report.surface.qaPhoneObserved&&tokenLogs===0)report.gate='PASS';
 await writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));process.exitCode=report.gate==='PASS'?0:1;
}
