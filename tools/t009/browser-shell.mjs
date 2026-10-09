// Browser component QA with explicit RTC/permission fixtures. NOT physical sensor evidence.
import { createServer } from 'node:http';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { resolve, extname } from 'node:path';
import { pathToFileURL } from 'node:url';
import assert from 'node:assert/strict';
const output = resolve(process.argv[2] || '/tmp/gorilla-t009-browser');
await mkdir(output, { recursive: false });
const { chromium } = await import(pathToFileURL(process.env.GORILLA_PLAYWRIGHT || `${process.env.HOME}/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright-core/index.mjs`));
const root = resolve('PWA/dist');
const server = createServer(async (req, res) => {
  const path = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
  const file = resolve(root, '.' + (path === '/' ? '/index.html' : path));
  if (!file.startsWith(root + '/')) { res.writeHead(403).end(); return; }
  try { const bytes = await readFile(file); res.setHeader('Content-Type', ({ '.js': 'text/javascript', '.css': 'text/css', '.html': 'text/html', '.svg': 'image/svg+xml' })[extname(file)] || 'application/octet-stream'); res.end(bytes); }
  catch { res.writeHead(404).end(); }
});
await new Promise(r => server.listen(0, '127.0.0.1', r));
const origin = `http://127.0.0.1:${server.address().port}`;
const browser = await chromium.launch({ executablePath: process.env.GORILLA_CHROME || '/usr/bin/google-chrome', headless: true, args: ['--disable-gpu'] });
const results = [];
try {
  for (const scenario of ['granted', 'denied', 'absent', 'partial', 'request-error', 'no-permission-api']) {
    const context = await browser.newContext({ viewport: { width: 390, height: 844 }, reducedMotion: 'reduce', serviceWorkers: 'block' });
    await context.addInitScript(scenario => {
      window.testRequests = 0; window.testAnswer = scenario;
      for (const name of ['DeviceMotionEvent', 'DeviceOrientationEvent']) {
        const absent = scenario === 'absent' || scenario === 'partial' && name === 'DeviceMotionEvent';
        Object.defineProperty(window, name, { configurable: true, value: absent ? undefined : scenario === 'no-permission-api' ? {} : { requestPermission: () => { window.testRequests++; if (window.testAnswer === 'request-error') throw Error('PRIVATE_NATIVE_ERROR'); return Promise.resolve(window.testAnswer === 'denied' ? 'denied' : 'granted'); } } });
      }
      class Channel extends EventTarget {
        constructor() { super(); this.readyState = 'connecting'; this.bufferedAmount = 0; }
        open() { this.readyState = 'open'; this.dispatchEvent(new Event('open')); this.onopen?.(); }
        send(text) { const n = JSON.parse(text); queueMicrotask(() => this.onmessage?.({ data: JSON.stringify({ ...n, messageType: 'ACK' }) })); }
        close() { this.readyState = 'closed'; this.onclose?.(); }
      }
      class Peer extends EventTarget {
        constructor() { super(); this.channels = []; this.iceGatheringState = 'complete'; this.signalingState = 'stable'; this.iceConnectionState = 'new'; this.connectionState = 'new'; }
        createDataChannel() { const c = new Channel(); this.channels.push(c); return c; }
        async createOffer() { return { type: 'offer', sdp: 'UNIT_PRIVATE_SDP' }; }
        async setLocalDescription(d) { this.localDescription = d; }
        async setRemoteDescription() { this.iceConnectionState = 'connected'; this.channels.forEach(c => c.open()); }
        close() { this.iceConnectionState = 'closed'; }
      }
      window.RTCPeerConnection = Peer;
    }, scenario);
    const page = await context.newPage(); const errors = []; page.on('pageerror', error => errors.push(error.message));
    await page.route('**/mobile/join', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ sessionId: 'UNIT_SESSION', resumeToken: 'UNIT_PRIVATE_RESUME', answer: 'UNIT_PRIVATE_SDP' }) }));
    await page.goto(origin + '/#sessionId=UNIT_SESSION&admission=UNIT_PRIVATE_ADMISSION');
    await page.getByRole('button', { name: 'CONECTAR', exact: true }).waitFor();
    assert.equal(await page.evaluate(() => window.testRequests), 0);
    assert.ok(await page.getByText('No lo sueltes ni lo lances durante el juego.').isVisible());
    assert.equal(await page.locator('pre').count(), 0);
    if (scenario === 'granted') await page.screenshot({ path: output + '/connect-390.png' });
    await page.getByRole('button', { name: 'CONECTAR', exact: true }).click();
    await page.getByRole('button', { name: 'ACTIVAR CONTROL', exact: true }).waitFor();
    assert.equal(await page.evaluate(() => window.testRequests), 0);
    await page.getByRole('button', { name: 'ACTIVAR CONTROL', exact: true }).click();
    const expected = scenario === 'denied' ? 'Movimiento sin permiso' : ['absent', 'partial'].includes(scenario) ? 'Movimiento limitado' : scenario === 'request-error' ? 'No se completó la preparación' : 'Acceso al movimiento permitido';
    await page.getByRole('heading', { name: expected, exact: true }).waitFor();
    if (scenario === 'denied') {
      assert.ok(await page.getByText(/Si el navegador recuerda tu respuesta/).isVisible());
      await page.screenshot({ path: output + '/denied-390.png' });
      await page.evaluate(() => { window.testAnswer = 'granted'; });
      await page.getByRole('button', { name: 'REINTENTAR PERMISO' }).click();
      await page.getByRole('heading', { name: 'Acceso al movimiento permitido' }).waitFor();
      assert.equal(await page.evaluate(() => window.testRequests), 4);
    }
    if (scenario === 'granted') {
      await page.screenshot({ path: output + '/permission-390.png' });
      for (const width of [320, 768, 1280]) {
        await page.setViewportSize({ width, height: 844 });
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        const button = await page.getByRole('button', { name: 'Copiar diagnóstico' }).boundingBox(); assert.ok(button.height >= 44);
        await page.getByRole('button', { name: 'Copiar diagnóstico' }).focus();
        assert.equal(await page.evaluate(() => document.activeElement.textContent), 'Copiar diagnóstico');
        assert.equal(await page.locator('.status-symbol').evaluate(el => getComputedStyle(el).animationName), 'none');
        await page.screenshot({ path: output + `/permission-${width}.png` });
      }
    }
    await page.getByRole('button', { name: 'Copiar diagnóstico' }).click();
    const report = JSON.parse(await page.locator('pre').textContent());
    assert.equal(report.controller.network, 'NETWORK_READY'); assert.equal(report.signalVerified, false);
    assert.equal(JSON.stringify(report).includes('UNIT_PRIVATE'), false);
    assert.equal(JSON.stringify(report).includes('INPUT_READY'), false);
    assert.deepEqual(errors, []);
    results.push({ scenario, evidence: 'BROWSER COMPONENT FIXTURES — NOT PHYSICAL', gate: 'PASS' }); await context.close();
  }
  const page = await browser.newPage(); await page.goto(origin);
  await page.getByRole('heading', { name: 'Escanea el QR de la PC' }).waitFor();
  assert.equal(await page.getByRole('button', { name: 'CONECTAR', exact: true }).count(), 0);
  await page.goto(origin + '/?view=health'); await page.getByRole('button', { name: 'Comprobar Java ahora' }).waitFor();
  results.push({ scenario: 'missing-QR-and-retained-health', gate: 'PASS' });
  await writeFile(output + '/summary.json', JSON.stringify({ scope: 'T009 shell browser component QA; simulated transport/permissions, no physical sensors', results, responsive: [320, 390, 768, 1280], gate: 'PASS' }, null, 2) + '\n');
  console.log(JSON.stringify({ gate: 'PASS', cases: results.length, output }));
} finally { await browser.close(); await new Promise(r => server.close(r)); }
