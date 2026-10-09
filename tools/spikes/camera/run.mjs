// Real Linux Player + existing Java/RTC/mobile runtime. This harness does not alter product clients.
import {spawn,spawnSync} from 'node:child_process';
import {createInterface} from 'node:readline';
import {randomBytes} from 'node:crypto';
import {readFile,writeFile,mkdir,rename} from 'node:fs/promises';
import {resolve,dirname} from 'node:path';
import {pathToFileURL} from 'node:url';
const output=process.argv[2],mode=process.argv[3]??'replay';if(!output)throw Error('new output path required');
try{await readFile(output);throw Error('preserve prior evidence')}catch(e){if(e.code!=='ENOENT')throw e;}
const {chromium}=await import(pathToFileURL(`${process.env.HOME}/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright-core/index.mjs`));
const operator=randomBytes(32).toString('base64url'),command=`${process.env.HOME}/.cache/gorilla-fusion-command-${process.pid}.json`;
const started=performance.now();const player=spawn(resolve('Unity/Builds/FoundationLinux/GorillaEscape.x86_64'),['-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],{env:{...process.env,GORILLA_IPC_JAVA:'/usr/lib/jvm/java-21-openjdk-amd64/bin/java',GORILLA_IPC_JAR:resolve('Server/target/local-server-0.1.0-SNAPSHOT.jar'),GORILLA_IPC_MODE:'lifecycle',GORILLA_PHONE_INPUT:'1',GORILLA_MOBILE_LAB:'1',GORILLA_MOBILE_OPERATOR:operator,GORILLA_CAMERA_LAB:'1',GORILLA_CAMERA_PYTHON:`${process.env.HOME}/.cache/gorilla-camera-venv/bin/python`,GORILLA_CAMERA_SCRIPT:resolve('tools/spikes/camera/worker.py'),GORILLA_CAMERA_FIXTURE:resolve('Shared/Protocol/camera/corpus-v1.json'),GORILLA_CAMERA_MODEL:`${process.env.HOME}/.cache/gorilla-camera-models/pose_landmarker_lite-v1.task`,GORILLA_CAMERA_DEVICE:'/dev/video0',GORILLA_CAMERA_MODE:mode,GORILLA_FUSION_COMMAND:command},stdio:['pipe','pipe','pipe']});
const result={scope:'Real four Chrome clients → RTC → managed Java → unchanged IPC → PhoneInput; local camera adapter → CameraInput → turn PlayerLock → temporal alignment → FusionFrame',mode,gate:'FAIL',physicalPhone:'DEFERRED',physicalHumanFusion:'DEFERRED',observations:[],cameraFrames:[],lifecycle:[],turns:[],negatives:[],cleanup:{}};
let port,cameraReady=false,cameraError=null,browser,clients=[],overflow=false,stderrBytes=0;player.stderr.on('data',b=>stderrBytes+=b.length);
const exited=new Promise(r=>player.once('exit',(code,signal)=>r({code,signal})));let exitSeen=false;exited.then(()=>exitSeen=true);
createInterface({input:player.stdout}).on('line',line=>{
 try{
  if(line.startsWith('PHONE_LAB_HTTP_READY '))port=Number(line.slice(21));
  if(line==='CAMERA_LAB_READY')cameraReady=true;
  if(line.startsWith('CAMERA_LAB_ERROR '))cameraError=line.slice(17);
  if(line.startsWith('FUSION_OBSERVED ')){if(result.observations.length>=20000)overflow=true;else result.observations.push(JSON.parse(line.slice(16)));}
  if(line.startsWith('CAMERA_INPUT_OBSERVED ')){const frame=JSON.parse(line.slice(22));if(result.cameraFrames.length<4000)result.cameraFrames.push(frame);else overflow=true;}
  if(line.startsWith('CAMERA_LAB_CLEANUP '))result.cleanup.camera=JSON.parse(line.slice(19));
  if(line.startsWith('CAMERA_LAB_METRICS '))result.cameraMetrics=JSON.parse(line.slice(19));
  if(line.startsWith('{"component":"ipc-unity"'))result.lifecycle.push(JSON.parse(line));
 }catch{result.parseError=true;}
});
function deadline(p,ms){let t;return Promise.race([p,new Promise((_,j)=>t=setTimeout(()=>j(Error('deadline')),ms))]).finally(()=>clearTimeout(t));}
async function wait(check,ms=12000){const end=performance.now()+ms;while(performance.now()<end){const v=check();if(v)return v;if(exitSeen)throw Error('Player exited prematurely');await new Promise(r=>setTimeout(r,20));}throw Error('observable condition deadline');}
async function select(playerId,ready=true){await writeFile(command+'.tmp',JSON.stringify({revision:Date.now(),playerId,ready}));await rename(command+'.tmp',command);}
try{
 await wait(()=>port,25000);result.startupMs=performance.now()-started;
 if(mode==='replay'||mode==='physical')await wait(()=>cameraReady,12000);else await wait(()=>cameraError,12000);
 result.cameraReady=cameraReady;result.cameraError=cameraError;
 const base=`http://127.0.0.1:${port}`;browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true});result.browser=browser.version();
 async function issue(){const r=await fetch(base+'/mobile/admission',{method:'POST',headers:{'X-Gorilla-Operator':operator}});if(!r.ok)throw Error('admission failure');const body=await r.json();const decoded=spawnSync('/usr/lib/jvm/java-21-openjdk-amd64/bin/java',['--class-path',`${process.env.HOME}/.m2/repository/com/google/zxing/core/3.5.3/core-3.5.3.jar`,'tools/spikes/webrtc/DecodeQr.java'],{input:Buffer.from(body.qrPng,'base64'),timeout:10000,maxBuffer:4096});if(decoded.status||decoded.stdout.toString()!==body.url)throw Error('QR mismatch');return body;}
 async function connect(a,resume=null){const context=await browser.newContext(),page=await context.newPage();await page.goto(base+'/mobile-lab/index.html');const identity=await page.evaluate(async({url,resume})=>{window.client=new MobileClient({source:'synthetic'});return client.connect(url,resume)},{url:a.url,resume});await page.evaluate(()=>{window.pumping=true;window.pump=(async()=>{let n=0;while(pumping&&client.pc){client.motion(normalizeMotion({acceleration:{x:client.identity.playerId,y:n++,z:0},rotationRate:{alpha:n,beta:0,gamma:0}}));await new Promise(r=>setTimeout(r,1000/30));}})();});return {context,page,identity,admission:a};}
 for(let p=1;p<=4;p++)clients.push(await connect(await issue()));result.playerIds=clients.map(c=>c.identity.playerId);if(new Set(result.playerIds).size!==4)throw Error('player isolation');
 const rtc=[];for(const c of clients)rtc.push(await c.page.evaluate(async()=>{const stats=await client.pc.getStats();return {playerId:client.identity.playerId,iceServers:client.pc.getConfiguration().iceServers.length,pairs:[...stats.values()].filter(x=>x.type==='candidate-pair'&&x.state==='succeeded'&&x.nominated).map(x=>({local:stats.get(x.localCandidateId)?.candidateType,remote:stats.get(x.remoteCandidateId)?.candidateType,protocol:stats.get(x.localCandidateId)?.protocol}))};}));result.rtc=rtc;
 if(mode==='replay'){
  for(let p=1;p<=4;p++){
   const begin=result.observations.length;await select(p);await wait(()=>result.observations.slice(begin).filter(x=>x.playerId===p&&x.eligible).length>=8);
   await wait(()=>result.observations.slice(begin).some(x=>x.playerId===p&&x.lockState==='LOST'));
   const lostAt=result.observations.findLastIndex(x=>x.playerId===p&&x.lockState==='LOST');await wait(()=>result.observations.slice(lostAt+1).some(x=>x.playerId===p&&x.eligible));
   const own=result.observations.slice(begin).filter(x=>x.playerId===p&&x.eligible);if(own.some(x=>x.marker!==p||x.subjectId!==1||x.cameraSource!=='replay'||x.delta>100||!x.phoneFresh||!x.cameraFresh))throw Error('fusion mismatch');result.turns.push({playerId:p,eligible:own.length,lossRecovery:'PASS'});
  }
  await select(1);await wait(()=>result.observations.slice(-80).some(x=>x.playerId===1&&x.eligible));
  const resume=await clients[0].page.evaluate(()=>({...client.identity})),admission=clients[0].admission,before=result.observations.length;
  await clients[0].page.evaluate(async()=>{pumping=false;await pump;await client.close();});await clients[0].context.close();
  await wait(()=>result.observations.slice(before).some(x=>x.playerId===1&&!x.phoneFresh));const cameraBefore=result.cameraFrames.length;
  await wait(()=>result.cameraFrames.length>cameraBefore+5);result.negatives.push({case:'phone-loss-does-not-stop-camera',gate:'PASS'});
  clients[0]=await connect(admission,resume);if(clients[0].identity.playerId!==1||clients[0].identity.connectionEpoch!==resume.connectionEpoch+1)throw Error('resume mismatch');
  const resumedAt=result.observations.length;await wait(()=>result.observations.slice(resumedAt).some(x=>x.playerId===1&&x.eligible&&x.phoneEpoch===resume.connectionEpoch+1));result.negatives.push({case:'manual-phone-recovery-new-epoch',gate:'PASS'});
 }else{
  await select(1);const begin=result.observations.length;await wait(()=>new Set(result.observations.slice(begin).filter(x=>x.phoneFresh).map(x=>x.playerId)).size===4);
  if(mode==='physical'){await wait(()=>result.cameraFrames.length>=40);result.physicalCamera='capture → MediaPipe → CameraInputStore PASS; human leave/return NOT RUN';}
  else result.negatives.push({case:'camera-'+mode+'-does-not-stop-four-mobile-inputs',gate:'PASS'});
 }
 for(const c of clients){await c.page.evaluate(async()=>{pumping=false;await pump;await client.close();});await c.context.close();}clients=[];
 const d=await fetch(base+'/mobile/diagnostics',{headers:{'X-Gorilla-Operator':operator}});result.javaDiagnostics=await d.json();if(result.javaDiagnostics.peers!==0)throw Error('peer residual');await fetch(base+'/mobile/end',{method:'POST',headers:{'X-Gorilla-Operator':operator}});
 if(result.observations.some(x=>x.phoneFresh&&x.marker!==x.playerId))throw Error('cross-player marker');
 result.gate='PASS';
}catch(e){result.error=e.message;}
finally{
 if(browser)await browser.close();
 if(!exitSeen)spawnSync('python3',['-c',`import sys;sys.path.insert(0,'tools');from validate_ipc_2b_player import X11;X11().close(${player.pid})`],{timeout:12000});
 try{result.cleanup.player=await deadline(exited,15000);}catch{player.kill('SIGKILL');result.cleanup.player=await exited;result.cleanup.forced=true;result.gate='FAIL';}
 result.cleanup.stderrBytes=stderrBytes;const stopped=result.lifecycle.findLast(x=>x.event==='STOPPED');result.cleanup.java=stopped?{state:stopped.event,exitCode:stopped.exitCode,complete:stopped.cleanupComplete,forced:stopped.forced}:null;
 const pids=result.lifecycle.filter(x=>x.event==='PROCESS_STARTED').map(x=>x.pid);if(result.cleanup.camera?.pid)pids.push(result.cleanup.camera.pid);
 result.cleanup.residualOwn=pids.filter(pid=>{try{process.kill(pid,0);return true;}catch{return false;}}).length;
 if(result.cleanup.player.code!==0||!result.cleanup.java?.complete||result.cleanup.java.exitCode!==0||result.cleanup.residualOwn||!result.cleanup.camera?.complete||result.cleanup.camera.forced||((mode==='replay'||mode==='physical')&&result.cleanup.camera.exitCode!==0)||((mode==='failure'||mode==='unavailable')&&result.cleanup.camera?.exitCode!==2)||overflow||result.parseError)result.gate='FAIL';
 result.metrics={evaluations:result.observations.length,eligible:result.observations.filter(x=>x.eligible).length,phoneStale:result.observations.filter(x=>!x.phoneFresh).length,cameraDegraded:result.observations.filter(x=>!x.cameraFresh).length,unaligned:result.observations.filter(x=>x.status==='UNALIGNED').length};
 const receive=result.cameraFrames.map(f=>f.unityReceiveTimestamp-f.processTimestamp).sort((a,b)=>a-b);if(receive.length)result.metrics.cameraProcessToUnityMs={n:receive.length,p50:receive[Math.ceil(receive.length*.5)-1],p95:receive[Math.ceil(receive.length*.95)-1],max:receive.at(-1)};
 const values=result.observations.filter(x=>x.eligible).map(x=>x.delta).sort((a,b)=>a-b);if(values.length)result.metrics.alignmentDeltaMs={n:values.length,p50:values[Math.ceil(values.length*.5)-1],p95:values[Math.ceil(values.length*.95)-1],max:values.at(-1)};
 await mkdir(dirname(resolve(output)),{recursive:true});await writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({gate:result.gate,error:result.error,mode,turns:result.turns,metrics:result.metrics,camera:result.cameraMetrics,cleanup:result.cleanup}));
}
process.exitCode=result.gate==='PASS'?0:1;
