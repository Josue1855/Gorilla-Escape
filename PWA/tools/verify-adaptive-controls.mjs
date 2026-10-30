// Local visual fixtures: no device, room admission or sensor simulation.
import {createServer} from 'vite';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {mkdir} from 'node:fs/promises';
import {pathToFileURL} from 'node:url';
import path from 'node:path';
const {chromium}=await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));
let renderFixture;
const server=await createServer({server:{host:'127.0.0.1',port:0},logLevel:'error',plugins:[{name:'adaptive-fixtures',configureServer(instance){instance.middlewares.use('/__qa/adaptive',(request,response)=>renderFixture(request,response));}}]});
const {default:ReadyControl}=await server.ssrLoadModule('/src/controller/ReadyControl.jsx');
const {games}=await server.ssrLoadModule('/src/controller/gamePresentation.js');
renderFixture=(request,response)=>{
  const id=new URL(request.url,'http://localhost').searchParams.get('game')||'lobby';
  if(id!=='lobby'&&!Object.hasOwn(games,id)){response.statusCode=404;response.end();return;}
  const context={version:3,contextId:'a'.repeat(32),gameId:id,screen:id==='lobby'?'menu':'game',hand:'left',phase:'Playing',playerId:id==='lobby'?0:1,
    phoneRequired:!['lobby','coconut-slice','jungle-keeper'].includes(id),stats:{hasScore:id!=='log-balance',score:10,hasProgress:true,progress:.4,hasElapsed:true,elapsed:12.3,hasBalance:id==='log-balance',balance:.15,hasDistance:id==='log-balance',distance:7.2}};
  const html=renderToStaticMarkup(createElement(ReadyControl,{view:{gameContext:context,playerId:1,state:'INPUT_READY',network:'NETWORK_READY',controlTilt:-.3},actions:createElement('button',null,'CENTRAR APUNTADO')}));
  response.setHeader('Content-Type','text/html; charset=utf-8');response.end(`<!doctype html><html lang="es"><head><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="stylesheet" href="/src/styles.css?direct"></head><body>${html}</body></html>`);
};
await server.listen();const origin=`http://127.0.0.1:${server.httpServer.address().port}`;
const browser=await chromium.launch({executablePath:process.env.GORILLA_TEST_CHROME,headless:true});
try {
  const output=path.resolve('../docs/evidence/mobile-adaptive-2026-10-29');await mkdir(output,{recursive:true});
  let checks=0;
  for(const width of [320,390,768]){
    const page=await browser.newPage({viewport:{width,height:844}});
    for(const game of ['lobby',...Object.keys(games)]){
      await page.goto(`${origin}/__qa/adaptive?game=${game}`);await page.evaluate(()=>document.fonts.ready);
      const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
      if(overflow)throw Error(`Horizontal overflow at ${width}: ${game}`);
      if(game==='lobby'&&await page.locator('.game-card').count()!==8)throw Error('Menu cards missing');
      if(await page.locator('.balance-control').count()!==(game==='log-balance'?1:0))throw Error('Incorrect tilt control');
      if(await page.locator('.camera-only').count()!==(['coconut-slice','jungle-keeper'].includes(game)?1:0))throw Error('Incorrect screen lock guidance');
      if(width===390&&['lobby','bowling','log-balance','coconut-slice'].includes(game))await page.screenshot({path:path.join(output,`${game}.png`),fullPage:true});
      checks++;
    }
    await page.close();
  }
  console.log(`PASS ${checks} responsive control fixtures; 320/390/768 px; menu, handedness, balance and camera-only layouts.`);
}finally{await browser.close();await server.close();}
