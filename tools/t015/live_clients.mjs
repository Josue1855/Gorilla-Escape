// Real browser clients controlled by the Unity PlayMode test. Credentials remain in memory.
import {pathToFileURL} from 'node:url';
import {createInterface} from 'node:readline';
const [base,output]=process.argv.slice(2);
const {writeFile}=await import('node:fs/promises');
const {chromium}=await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));
const browser=await chromium.launch({executablePath:process.env.GORILLA_TEST_CHROME,headless:true});
const result={scope:'Real Chrome/PWA → RTC → Java → owned Unity IPC / PlayMode',source:'synthetic',physical:'NOT RUN',browser:browser.version(),cases:[],gate:'FAIL'};
const api=await browser.newContext();const clients=[];
let operator;
const check=(name,ok)=>{if(!ok)throw Error(name);result.cases.push({case:name,gate:'PASS'});};
const lines=createInterface({input:process.stdin});const commands=[];let waking;let eof=false;
lines.on('line',line=>{commands.push(line);waking?.();});lines.on('close',()=>{eof=true;waking?.();});
async function command(){while(!commands.length&&!eof)await new Promise(r=>waking=r);if(eof&&!commands.length)throw Error('Unity command EOF');return commands.shift();}
async function admission(){const r=await api.request.post(base+'/mobile/admission',{headers:{'X-Gorilla-Operator':operator}});if(!r.ok())throw Error('admission');return (await r.json()).url;}
async function client(url,resume=null){const context=await browser.newContext();const page=await context.newPage();await page.goto(base+'/mobile-lab/index.html');await page.waitForFunction(()=>typeof MobileClient==='function');await page.evaluate(async({url,resume})=>{window.client=new MobileClient({source:'synthetic'});await client.connect(url,resume);},{url,resume});const identity=await page.evaluate(()=>({...client.identity}));return {context,page,identity,url};}
async function stream(c){await c.page.evaluate(()=>{window.motionTimer=setInterval(()=>client.motion(normalizeMotion({acceleration:{x:client.identity.playerId,y:1,z:0}})),50);});}
try{
 const authentication=await command();if(!/^AUTH [A-Za-z0-9_-]{43}$/.test(authentication))throw Error('AUTH_CONFIG');operator=authentication.slice(5);
 for(let i=1;i<=4;i++){const c=await client(await admission());clients.push(c);check(`${i}-players-admitted`,new Set(clients.map(c=>c.identity.playerId)).size===i);}
 const full=await api.request.post(base+'/mobile/admission',{headers:{'X-Gorilla-Operator':operator}});check('fifth-admission-rejected',full.status()>=400);
 const fifth=await clients[3].page.evaluate(async()=>{const r=await fetch('/mobile/join',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({protocolVersion:1,messageType:'JOIN',sessionId:client.identity.sessionId,admission:'x'.repeat(43),offer:client.pc.localDescription.sdp})});return r.status;});check('fifth-join-rejected',fifth===400);
 console.log('NETWORK_ONLY');check('unity-rejected-ready-before-input',(await command())==='INPUT');
 for(const c of clients)await stream(c);console.log('INPUT_STREAMING');
 check('unity-issued-official-start',(await command())==='STARTED');
 const closed=await api.request.post(base+'/mobile/admission',{headers:{'X-Gorilla-Operator':operator}});check('post-start-issuance-rejected',closed.status()>=400);
 const closedJoin=await clients[1].page.evaluate(async()=>{const r=await fetch('/mobile/join',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({protocolVersion:1,messageType:'JOIN',sessionId:client.identity.sessionId,admission:'x'.repeat(43),offer:client.pc.localDescription.sdp})});return r.status;});check('post-start-new-join-rejected',closedJoin===400);
 // Invalid identity on real RTC must receive ERROR and cannot become official READY.
 const spoof=await clients[1].page.evaluate(async()=>{const ch=client.channels.control,prior=ch.onmessage;const n=client.envelope('HELLO',{});n.playerId=4;return new Promise(resolve=>{const t=setTimeout(()=>{ch.onmessage=prior;resolve(false);},2000);ch.onmessage=e=>{if(JSON.parse(e.data).messageType==='ERROR'){clearTimeout(t);ch.onmessage=prior;resolve(true);}else prior(e);};ch.send(JSON.stringify(n));});});check('real-rtc-playerid-spoof-rejected',spoof);
 const old=clients[0];await old.page.evaluate(async()=>{clearInterval(motionTimer);await client.close();});await old.context.close();
 console.log('DISCONNECTED');check('unity-cancelled-candidate-retained-result',(await command())==='RECONNECT');
 const resumed=await client(old.url,old.identity);clients[0]=resumed;await stream(resumed);
 check('post-start-resume-same-player-device-new-epoch',resumed.identity.playerId===old.identity.playerId&&resumed.identity.deviceSessionId===old.identity.deviceSessionId&&resumed.identity.connectionEpoch===old.identity.connectionEpoch+1);
 const replay=await resumed.page.evaluate(async old=>{const r=await fetch('/mobile/reconnect',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({sessionId:old.sessionId,playerId:old.playerId,resumeToken:old.resumeToken,offer:client.pc.localDescription.sdp})});return r.status;},old.identity);check('consumed-resume-replay-rejected',replay===400);
 const phoneStart=await resumed.page.evaluate(()=>client.send('GAME_START',{}).then(()=>false,()=>true));check('phone-cannot-issue-game-start',phoneStart);
 console.log('RESUMED');check('unity-observed-preserved-association',(await command())==='FINISH');result.gate='PASS';
}catch(e){result.error=e.message;console.log('FAILED');}
finally{
 for(const c of clients)try{await c.page.evaluate(async()=>{clearInterval(window.motionTimer);await client.close();});await c.context.close();}catch{}
 await api.close();await browser.close();lines.close();await writeFile(output,JSON.stringify(result,null,2)+'\n');
 console.log(result.gate==='PASS'?'PASS':'FAIL');process.exitCode=result.gate==='PASS'?0:1;
}
