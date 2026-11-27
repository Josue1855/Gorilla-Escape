import {spawn} from 'node:child_process';
import {createInterface} from 'node:readline';
import {createServer} from 'node:http';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {resolve,dirname} from 'node:path';
import {randomBytes,createHash} from 'node:crypto';
const dir=dirname(fileURLToPath(import.meta.url));
const output=process.argv[2]; if(!output)throw Error('result path required');
try{await readFile(output);throw Error('Preserve existing result: choose a new path')}catch(e){if(e.code!=='ENOENT')throw e}
const {chromium}=await import(pathToFileURL(process.env.WT_PLAYWRIGHT_MODULE||`${process.env.HOME}/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright-core/index.mjs`));
const cp=(await readFile(resolve(dir,'target/classpath.txt'),'utf8')).trim();
const startupStarted=performance.now();
const java=spawn('/usr/lib/jvm/java-21-openjdk-amd64/bin/java',['-cp',`${dir}/target/classes:${cp}`,'com.gorillaescape.spike.RtcProbe'],{stdio:['pipe','pipe','pipe']});
let stderrBytes=0; java.stderr.on('data',b=>{stderrBytes+=b.length});
const pending=new Map(); let closedSummary=false;
const exit=new Promise(r=>java.once('exit',(code,signal)=>r({code,signal})));
const lines=createInterface({input:java.stdout});
let readyResolve;const ready=new Promise(r=>readyResolve=r);
lines.on('line',line=>{if(line==='READY')readyResolve(); if(line==='STOPPED residualPeers=0')closedSummary=true;
 const [kind,id,data]=line.split(' ');const p=pending.get(id);if(p){pending.delete(id);clearTimeout(p.timer);kind==='ERROR'?p.reject(Error('Java rejected request')):p.resolve({kind,data});}});
function command(id,text){if(pending.size>=8||pending.has(id))return Promise.reject(Error('bounded command slots'));return new Promise((resolve,reject)=>{const timer=setTimeout(()=>{pending.delete(id);reject(Error('Java deadline'));},35000);pending.set(id,{resolve,reject,timer});java.stdin.write(text+'\n')});}
function deadline(p,ms,label){let t;return Promise.race([p,new Promise((_,r)=>{t=setTimeout(()=>r(Error(label)),ms)})]).finally(()=>clearTimeout(t));}
const host=createServer((req,res)=>{res.setHeader('Cache-Control','no-store');res.end('<!doctype html><title>Isolated RTC comparator</title>')});
await new Promise(r=>host.listen(0,'127.0.0.1',r));
let browser;const result={scope:'Linux desktop Chromium ↔ real Java; software/lab only',library:'webrtc-java 0.19.0 linux-x86_64',signaling:'owned pipes via automation; not product onboarding',iceServers:[],physical:'DEFERRED',runs:[],negatives:[],cleanup:{},gate:'FAIL'};
try{
 await deadline(ready,15000,'Java readiness');result.javaStartupMs=performance.now()-startupStarted;browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true});result.browser=browser.version();
 let counter=0;
 async function client(){const id='p'+(++counter);const credential=randomBytes(32).toString('hex');const context=await browser.newContext();const page=await context.newPage();
  await page.exposeFunction('offerJava',async sdp=>{const a=await command(id,`OFFER ${id} ${credential} ${Buffer.from(sdp).toString('base64')}`);return Buffer.from(a.data,'base64').toString()});
  await page.goto(`http://127.0.0.1:${host.address().port}`);
  await page.evaluate(async()=>{
   window.pc=new RTCPeerConnection({iceServers:[]});window.channels={};
   for(const [label,options] of [['reliable',{ordered:true}],['unordered',{ordered:false,maxRetransmits:0}]]){
    const ch=pc.createDataChannel(label,options);channels[label]=ch;
   }
   await pc.setLocalDescription(await pc.createOffer());
   if(pc.iceGatheringState!=='complete')await new Promise((r,j)=>{const t=setTimeout(()=>j(Error('browser ICE deadline')),10000);pc.addEventListener('icegatheringstatechange',()=>{if(pc.iceGatheringState==='complete'){clearTimeout(t);r()}},{once:false})});
   await pc.setRemoteDescription({type:'answer',sdp:await offerJava(pc.localDescription.sdp)});
   await Promise.all(Object.values(channels).map(ch=>ch.readyState==='open'?Promise.resolve():new Promise((r,j)=>{const t=setTimeout(()=>j(Error('channel open deadline')),10000);ch.onopen=()=>{clearTimeout(t);r()};ch.onerror=()=>{clearTimeout(t);j(Error('channel failed'))}})));
   window.echo=(label,text)=>new Promise((r,j)=>{const ch=channels[label];const t=setTimeout(()=>{ch.onmessage=null;j(Error('echo timeout'))},2000);ch.onmessage=e=>{clearTimeout(t);ch.onmessage=null;r(e.data)};ch.send(text)});
  });
  async function close(){await page.evaluate(()=>{Object.values(channels).forEach(c=>c.close());pc.close()}).catch(()=>{});const a=await command(id,`CLOSE ${id}`);if(a.kind!=='CLOSED')throw Error('close failed');await context.close();}
  return {id,page,credential,close};
 }
 async function run(c,n){return await c.page.evaluate(async({credential,n})=>{
  const channelsResult=[];
  for(const label of ['reliable','unordered']){
   if(await echo(label,'AUTH:'+credential)!=='READY')throw Error('authentication failed');
   for(const size of [32,1024]){
    const values=[];let received=0,timeouts=0,errors=0;const started=performance.now();
    for(let i=0;i<n;i++){const text=`PING:${i}:`+'x'.repeat(size);const t=performance.now();try{const reply=await echo(label,text);if(reply!==text)throw Error('wrong payload');received++;values.push(performance.now()-t)}catch(e){if(e.message==='echo timeout')timeouts++;else errors++}}
    values.sort((a,b)=>a-b);const pct=p=>values[Math.max(0,Math.ceil(values.length*p)-1)];
    channelsResult.push({channel:label,ordered:channels[label].ordered,maxRetransmits:channels[label].maxRetransmits,payloadPadding:size,sent:n,received,timeouts,errors,loss:n-received,minMs:values[0],p50Ms:pct(.5),p95Ms:pct(.95),maxMs:values.at(-1),durationMs:performance.now()-started});
   }
  }
  const stats=await pc.getStats();
  const selected=[...stats.values()].find(s=>s.type==='transport'&&s.selectedCandidatePairId);
  if(!selected)throw Error('selected candidate pair missing');
  const pair=stats.get(selected.selectedCandidatePairId);
  const local=stats.get(pair.localCandidateId),remote=stats.get(pair.remoteCandidateId);
  if(local.candidateType!=='host'||remote.candidateType!=='host')throw Error('non-host ICE path');
  window.pathEvidence={localCandidateType:local.candidateType,remoteCandidateType:remote.candidateType,protocol:local.protocol};
  return channelsResult;
 },{credential:c.credential,n});}
 const single=await client();result.runs.push({case:'single',metrics:await run(single,100)});result.pathEvidence=await single.page.evaluate(()=>window.pathEvidence);
 result.negatives.push({case:'invalid-sdp',gate:await command('invalid',`OFFER invalid ${randomBytes(32).toString('hex')} ${Buffer.from('invalid SDP').toString('base64')}`).then(()=>'FAIL',e=>e.message==='Java rejected request'?'PASS':'FAIL')});
 result.negatives.push({case:'duplicate-peer-offer-isolated',gate:await command(single.id,`OFFER ${single.id} ${randomBytes(32).toString('hex')} ${Buffer.from('invalid SDP').toString('base64')}`).then(()=>'FAIL',e=>e.message==='Java rejected request'?'PASS':'FAIL')});
 const retained=await single.page.evaluate(()=>echo('reliable','PING:888:x'));if(retained!=='PING:888:x')throw Error('duplicate offer disrupted existing peer');
 const invalid=await single.page.evaluate(()=>echo('reliable','BAD'));result.negatives.push({case:'invalid-payload',gate:invalid==='INVALID'?'PASS':'FAIL'});await single.close();
 const recovery=await client();result.runs.push({case:'manual-reconnect-new-peer',metrics:await run(recovery,100)});await recovery.close();
 const four=[];for(let i=0;i<4;i++)four.push(await client());
 result.negatives.push({case:'fifth-peer-bounded',gate:await command('fifth',`OFFER fifth ${randomBytes(32).toString('hex')} ${Buffer.from('v=0').toString('base64')}`).then(()=>'FAIL',e=>e.message==='Java rejected request'?'PASS':'FAIL')});
 result.runs.push({case:'four-active-peers',clients:await Promise.all(four.map(async c=>({id:c.id,metrics:await run(c,100)})))});
 await four[0].close();const alive=await four[1].page.evaluate(()=>echo('reliable','PING:999:x'));result.negatives.push({case:'one-disconnect-others-alive',gate:alive==='PING:999:x'?'PASS':'FAIL'});
 for(const c of four.slice(1))await c.close();
 const bad=await client();const badResult=await bad.page.evaluate(async()=>{const ch=channels.reliable;const p=new Promise(r=>{const t=setTimeout(()=>r('TIMEOUT'),2500);ch.onclose=()=>{clearTimeout(t);r('CLOSED')}});ch.send('AUTH:wrong');return p});result.negatives.push({case:'invalid-credential',gate:badResult==='CLOSED'?'PASS':'FAIL'});await bad.close();
 const big=await client();await big.page.evaluate(c=>echo('reliable','AUTH:'+c),big.credential);const bigResult=await big.page.evaluate(async()=>{const ch=channels.reliable;const p=new Promise(r=>{const t=setTimeout(()=>r('TIMEOUT'),2500);ch.onclose=()=>{clearTimeout(t);r('CLOSED')}});ch.send('x'.repeat(4097));return p});result.negatives.push({case:'oversize',gate:bigResult==='CLOSED'?'PASS':'FAIL'});await big.close();
 const metrics=result.runs.flatMap(r=>r.metrics||r.clients.flatMap(c=>c.metrics));result.gate=metrics.every(m=>m.sent===m.received&&m.timeouts===0&&m.errors===0)&&result.negatives.every(n=>n.gate==='PASS')?'PASS':'FAIL';
}catch(e){result.error=e.message;}
finally{
 if(browser)await browser.close();java.stdin.end();
 try{result.cleanup.java=await deadline(exit,10000,'Java shutdown');}catch{java.kill('SIGKILL');result.cleanup.java=await exit;result.cleanup.forced=true;result.gate='FAIL';}
 result.cleanup.residualPeersZero=closedSummary;result.cleanup.pendingCommands=pending.size;result.cleanup.stderrBytes=stderrBytes;
 for(const p of pending.values()){clearTimeout(p.timer);p.reject(Error('shutdown'))}pending.clear();await new Promise(r=>host.close(r));
 if(result.cleanup.java.code!==0||!closedSummary)result.gate='FAIL';
 result.sourceHashes={};for(const name of ['pom.xml','run.mjs','src/main/java/com/gorillaescape/spike/RtcProbe.java'])result.sourceHashes[name]=createHash('sha256').update(await readFile(resolve(dir,name))).digest('hex');
 await mkdir(dirname(resolve(output)),{recursive:true});await writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({gate:result.gate,error:result.error,runs:result.runs.length,negatives:result.negatives,cleanup:result.cleanup}));
}
process.exitCode=result.gate==='PASS'?0:1;
