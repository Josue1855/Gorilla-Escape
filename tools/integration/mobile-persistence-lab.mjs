// Explicit local lab: real RTC/profile request; no admission/peer secrets in output.
import {readFile} from 'node:fs/promises';
import {createInterface} from 'node:readline';
import {pathToFileURL} from 'node:url';
const {chromium}=await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));
const lines=createInterface({input:process.stdin});const pending=[];let wake;
lines.on('line',line=>{pending.push(JSON.parse(line));wake?.();});
async function next(){while(!pending.length)await new Promise(r=>wake=r);return pending.shift();}
const config=await next(),source=await readFile(new URL('../../PWA/src/input/mobileClient.js',import.meta.url),'utf8');
const browser=await chromium.launch({executablePath:process.env.GORILLA_TEST_CHROME,headless:true});
try{
 const page=await browser.newPage();await page.goto(config.ingress+'/');
 const result=await page.evaluate(async({source,url})=>{
   const {MobileClient}=await import('data:text/javascript;base64,'+btoa(source));
   window.client=new MobileClient({source:'synthetic'});await client.connect(url);
   const bad=await fetch('/mobile/profile',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({peerId:'bad',resumeToken:'bad',displayName:'QA'})});
   const invalidName=await fetch('/mobile/profile',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({peerId:client.identity.peerId,resumeToken:client.identity.resumeToken,displayName:'🙂'})});
   return {invalidStatus:bad.status,invalidNameStatus:invalidName.status};
 },{source,url:config.url});
 // Submit exactly one authenticated write using the documented profile fields.
 const profile=await page.evaluate(async()=>{
   const body={peerId:client.identity.peerId,resumeToken:client.identity.resumeToken,displayName:'Jugador laboratorio'};
   const response=await fetch('/mobile/profile',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
   const data=await response.json();
   const repeat=await fetch('/mobile/profile',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});const cached=await repeat.json();
   return {status:response.status,id:data.id,cached:repeat.status===200&&cached.id===data.id};
 });
 if(result.invalidStatus!==400||result.invalidNameStatus!==400||profile.status!==200||!profile.id||!profile.cached)throw Error('PROFILE_GATE');
 console.log(JSON.stringify({event:'REGISTERED',id:profile.id,invalidStatus:result.invalidStatus}));
 const seen=[];
 for(let i=0;i<7;i++){
   const {gameId}=await next();
   await page.waitForFunction(id=>client.gameContext?.version===2&&client.gameContext.gameId===id,gameId,{timeout:5000});
   seen.push(await page.evaluate(()=>({gameId:client.gameContext.gameId,score:client.gameContext.stats.score})));
   console.log(JSON.stringify({event:'CONTEXT',gameId}));
 }
 const after=await page.evaluate(async()=>{const before=client.metrics.motionSent;await client.send('CLIENT_STATE',{state:'suspended'});await client.send('CLIENT_STATE',{state:'active'});return {running:client.state==='RUNNING',motionDuringDisplay:client.metrics.motionSent-before};});
 await page.evaluate(()=>client.close());console.log(JSON.stringify({event:'PASS',games:seen.length,independentDisplay:after.motionDuringDisplay===0,running:after.running}));
}catch(error){console.log(JSON.stringify({event:'FAIL',code:/JOIN_E[0-9A-Z_]+/.exec(error.message)?.[0]||error.name}));process.exitCode=1;}finally{await browser.close();lines.close();}
