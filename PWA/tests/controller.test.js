import test from 'node:test';
import assert from 'node:assert/strict';
import { permissionCapabilities, requestControlPermissions, permissionState } from '../src/controller/permissions.js';
import { ControllerSession } from '../src/controller/session.js';
const win = (extra = {}) => { const w = new EventTarget(), doc = new EventTarget(); doc.visibilityState = 'visible'; return Object.assign(w, { document: doc, isSecureContext: true, setInterval, clearInterval, setTimeout, clearTimeout, ...extra }); };
const url = 'https://example.invalid/#sessionId=test&admission=UNIT_SECRET';
const client = () => ({ state: 'STOPPED', calls: 0, async connect() { this.calls++; this.state = 'RUNNING'; }, async close() { this.state = 'STOPPED'; }, diagnostic() { return { stage: 'READY', code: null }; } });

test('permission probe never requests access on load; capability-based, no UA', () => {
  let requests = 0;
  const w = win({ DeviceMotionEvent: { requestPermission: () => { requests++; } }, DeviceOrientationEvent: {} });
  assert.deepEqual(permissionCapabilities(w), { motion: 'request-required', orientation: 'not-required-by-api' });
  const model = new ControllerSession({ win: w, url, client: client() });
  assert.equal(model.state, 'CONNECT'); assert.equal(requests, 0);
});
test('both permissions invoked synchronously before awaiting, granted is not a verified stream', async () => {
  const calls = []; let finish;
  const w = win({ DeviceMotionEvent: { requestPermission: () => { calls.push('motion'); return new Promise(r => { finish = r; }); } }, DeviceOrientationEvent: { requestPermission: () => { calls.push('orientation'); return 'granted'; } } });
  const request = requestControlPermissions(w);
  assert.deepEqual(calls, ['motion', 'orientation']); finish('granted');
  assert.equal(permissionState(await request), 'PERMISSION_GRANTED');
});
test('denial is separate from absent API and retry really requests again', async () => {
  let allowed = false; let requests = 0;
  const api = { requestPermission: () => { requests++; return Promise.resolve(allowed ? 'granted' : 'denied'); } };
  const model = new ControllerSession({ win: win({ DeviceMotionEvent: api, DeviceOrientationEvent: api }), url, client: client() });
  await model.connect(); await model.activate(); assert.equal(model.state, 'PERMISSION_DENIED');
  allowed = true; await model.activate(); assert.equal(model.state, 'PREPARING_SENSORS'); assert.equal(requests, 4);
  await model.dispose();
});
test('all APIs absent and partial API support remain explicit, no zeros manufactured', async () => {
  const none = await requestControlPermissions(win());
  assert.equal(permissionState(none), 'SENSOR_UNAVAILABLE');
  const partial = await requestControlPermissions(win({ DeviceOrientationEvent: {} }));
  assert.equal(permissionState(partial), 'SENSOR_UNAVAILABLE');
  assert.deepEqual(partial.permissions, { motion: 'api-absent', orientation: 'not-required-by-api' });
});
test('Android-style API without requestPermission is not reported as an explicit grant', async () => {
  const report = await requestControlPermissions(win({ DeviceMotionEvent: {}, DeviceOrientationEvent: {} }));
  assert.equal(report.permissions.motion, 'not-required-by-api');
  assert.equal(permissionState(report), 'PERMISSION_GRANTED');
});
test('insecure context never invokes sensor permission, errors are sanitized', async () => {
  const report = await requestControlPermissions(win({ isSecureContext: false, DeviceMotionEvent: { requestPermission: () => { throw Error('must not call'); } } }));
  assert.equal(permissionState(report), 'ERROR');
  const failed = await requestControlPermissions(win({ DeviceMotionEvent: { requestPermission: () => { throw Error('PRIVATE_ERROR'); } }, DeviceOrientationEvent: {} }));
  assert.equal(permissionState(failed), 'ERROR'); assert.equal(JSON.stringify(failed).includes('PRIVATE_ERROR'), false);
});
test('CONNECT admission used once, technical connection precedes explicit permission', async () => {
  let requests = 0; const transport = client();
  const model = new ControllerSession({ win: win({ DeviceMotionEvent: { requestPermission: () => { requests++; return 'granted'; } } }), url, client: transport });
  assert.equal(model.activate(), undefined);
  await Promise.all([model.connect(), model.connect()]);
  assert.equal(transport.calls, 1); assert.equal(requests, 0); assert.equal(model.state, 'PERMISSION_REQUIRED');
  assert.equal(model.network, 'NETWORK_READY');
  const report = JSON.stringify(model.diagnostic());
  assert.equal(report.includes('UNIT_SECRET'), false); assert.equal(report.includes('INPUT_READY'), false);
  assert.equal(model.diagnostic().signalVerified, false); await model.dispose();
});
test('no QR does not simulate discovery or invoke transport', async () => {
  const transport = client(); const model = new ControllerSession({ win: win(), url: 'https://example.invalid/', client: transport });
  await model.connect(); assert.equal(model.state, 'NO_ADMISSION'); assert.equal(transport.calls, 0);
});
test('failed admission cannot be replayed; transport error text stays private', async () => {
  const transport = client(); transport.connect = async () => { transport.calls++; throw Error('SECRET_ADMISSION'); };
  const model = new ControllerSession({ win: win(), url, client: transport });
  await model.connect(); await model.connect(); assert.equal(transport.calls, 1); assert.equal(model.state, 'ERROR');
  assert.equal(JSON.stringify(model.snapshot()).includes('SECRET'), false);
});
test('transport disconnect observed, late permission cannot override it, monitor cleaned', async () => {
  let tick; let cleared = 0; let grant;
  const w = win({ setInterval: callback => { tick = callback; return 7; }, clearInterval: id => { if (id === 7) cleared++; }, DeviceMotionEvent: { requestPermission: () => new Promise(r => { grant = r; }) }, DeviceOrientationEvent: {} });
  const transport = client(); const model = new ControllerSession({ win: w, url, client: transport });
  await model.connect(); const pending = model.activate(); transport.state = 'DISCONNECTED'; tick(); grant('granted'); await pending;
  assert.equal(model.state, 'DISCONNECTED'); assert.equal(model.network, 'DISCONNECTED');
  await model.dispose(); assert.ok(cleared >= 1); assert.equal(transport.state, 'STOPPED');
});
test('disposing during connect cannot publish readiness or retain owned transport', async () => {
  let finish; const transport = client(); transport.connect = () => new Promise(r => { finish = r; });
  const events = []; const model = new ControllerSession({ win: win(), url, client: transport, notify: v => events.push(v.state) });
  const connecting = model.connect(); await model.dispose(); finish({}); await connecting;
  assert.deepEqual(events, ['CONNECTING']); assert.equal(transport.state, 'STOPPED');
});
