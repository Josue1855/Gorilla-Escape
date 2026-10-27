import { readFile } from 'node:fs/promises';
import { createInterface } from 'node:readline';
import { pathToFileURL } from 'node:url';
const { chromium } = await import(pathToFileURL(process.env.GORILLA_TEST_PLAYWRIGHT));
const lines = createInterface({ input: process.stdin });
const url = await new Promise(resolve => lines.once('line', resolve));
const source = await readFile(new URL('../../../PWA/src/input/mobileClient.js', import.meta.url), 'utf8');
const browser = await chromium.launch({ executablePath: process.env.GORILLA_TEST_CHROME, headless: true });
try {
 const page = await browser.newPage(); const response = await page.goto(url, { waitUntil: 'load', timeout: 30000 });
 if (response.status() !== 200) throw Error('PUBLIC_PAGE');
 const result = await page.evaluate(async ({ source, url }) => {
  const { MobileClient } = await import('data:text/javascript;base64,' + btoa(source));
  const client = new MobileClient({ source:'synthetic' });
  try { await client.connect(url); await client.send('HEARTBEAT', {});
   const named=await client.fetcher('/mobile/name',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({peerId:client.identity.peerId,resumeToken:client.identity.resumeToken,displayName:'Jugador prueba'})});
   if(!named.ok || (await named.json()).displayName!=='Jugador prueba')throw Error('NAME_FAILED');
   const result = { state:client.state, channels:Object.values(client.channels).map(channel => channel.readyState) };
   await client.close(); return result;
  } catch (error) { return { error: String(error.message).replace(/[^A-Z0-9_]/g,'') }; }
 }, { source, url });
 if (result.state !== 'RUNNING' || result.channels.some(state => state !== 'open')) throw Error(result.error || 'RTC_NOT_READY');
 console.log('PASS: navegador, sala pública y WebRTC local');
} finally { lines.close(); await browser.close(); }
