// Opt-in localhost Chrome/native RTC check. Admission and resume secrets stay in private stdin.
import {readFile} from 'node:fs/promises';
import {createInterface} from 'node:readline';
import {pathToFileURL} from 'node:url';
const {chromium}=await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));
const lines=createInterface({input:process.stdin}),pending=[];let wake;
lines.on('line',line=>{pending.push(JSON.parse(line));wake?.();});
async function next(){while(!pending.length)await new Promise(resolve=>wake=resolve);return pending.shift();}
const config=await next(),source=await readFile(new URL('../../PWA/src/input/mobileClient.js',import.meta.url),'utf8');
const browser=await chromium.launch({executablePath:process.env.GORILLA_TEST_CHROME,headless:true});
try {
  const page=await browser.newPage();await page.goto(config.ingress+'/');
  await page.evaluate(async({source,url})=>{
    const {MobileClient}=await import('data:text/javascript;base64,'+btoa(source));
    window.client=new MobileClient({source:'synthetic'});window.joinUrl=url;await client.connect(url);
  },{source,url:config.url});
  await page.waitForFunction(()=>client.gameContext?.version===3&&client.gameContext.gameId==='lobby');
  await page.evaluate(()=>client.send('UI_COMMAND',{requestId:crypto.randomUUID().replaceAll('-',''),contextId:client.gameContext.contextId,action:'select-game',target:'bowling'}));
  console.log(JSON.stringify({event:'SELECTED'}));await next();
  await page.waitForFunction(()=>client.gameContext?.gameId==='bowling');
  await page.evaluate(()=>client.send('UI_COMMAND',{requestId:crypto.randomUUID().replaceAll('-',''),contextId:client.gameContext.contextId,action:'set-hand',target:'left'}));
  console.log(JSON.stringify({event:'HAND'}));await next();
  await page.waitForFunction(()=>client.gameContext?.hand==='left');
  const resumed=await page.evaluate(async()=>{
    const resume={...client.identity},epoch=resume.connectionEpoch;
    await client.send('CLIENT_STATE',{state:'suspended'});await client.close(false);
    await client.connect(window.joinUrl,resume);
    return {samePlayer:client.identity.playerId===resume.playerId,newEpoch:client.identity.connectionEpoch>epoch,ready:client.state==='RUNNING'};
  });
  if(!resumed.samePlayer||!resumed.newEpoch||!resumed.ready)throw Error('RESUME');
  await page.waitForFunction(()=>client.gameContext?.gameId==='bowling'&&client.gameContext.hand==='left');
  await page.evaluate(()=>client.send('UI_COMMAND',{requestId:crypto.randomUUID().replaceAll('-',''),contextId:client.gameContext.contextId,action:'menu',target:''}));
  console.log(JSON.stringify({event:'RETURNED',...resumed}));await next();
  await page.waitForFunction(()=>client.gameContext?.gameId==='lobby');await page.evaluate(()=>client.close());
  console.log(JSON.stringify({event:'PASS'}));
} catch(error){console.log(JSON.stringify({event:'FAIL',code:/JOIN_E[0-9A-Z_]+/.exec(error.message)?.[0]||error.name}));process.exitCode=1;}
finally{await browser.close();lines.close();}
