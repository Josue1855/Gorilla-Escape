import { createServer } from 'node:http';
import { readFile, writeFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';
const [configFile,resultFile]=process.argv.slice(2);
const config=JSON.parse(await readFile(configFile,'utf8'));
const modulePath=process.env.WT_PLAYWRIGHT_MODULE;
if(!modulePath) throw Error('WT_PLAYWRIGHT_MODULE must name existing playwright-core');
const {chromium}=await import(pathToFileURL(modulePath));
const source=await readFile(new URL('client.js',import.meta.url),'utf8');
const server=createServer((req,res)=>{res.setHeader('Cache-Control','no-store');res.end('<!doctype html><title>Isolated WebTransport lab</title>');});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
let browser;
try {
  browser=await chromium.launch({executablePath:process.env.WT_CHROME||'/usr/bin/google-chrome',headless:true});
  const page=await browser.newPage({ignoreHTTPSErrors:false});
  await page.goto(`http://127.0.0.1:${server.address().port}`);
  const features=await page.evaluate(()=>({secureContext:isSecureContext,webtransport:typeof WebTransport==='function'}));
  if(!features.secureContext||!features.webtransport)throw Error('browser prerequisites');
  await page.addScriptTag({content:source.replace('export async function runProbe','window.runProbe = async function')});
  const results=[];
  for(const item of config.cases) {
    const result=await page.evaluate(c=>window.runProbe(c),item);
    results.push({name:item.name,expected:item.expected,...result});
  }
  await writeFile(resultFile,JSON.stringify({browser:browser.version(),features,results},null,2));
} finally {if(browser)await browser.close();await new Promise(r=>server.close(r));}
