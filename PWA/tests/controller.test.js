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
test('QA telemetry reuses measured windows, preserves missing and zero, and clears callback', async () => {
  const transport=client(), model=new ControllerSession({win:win(),url,client:transport});
  assert.deepEqual(transport.qaTelemetry(),{qaVersion:1,motionHz:null,orientationHz:null,emissionHz:null,rttP50:null,rttP95:null});
  model.capture.metrics=()=>({channels:{motion:{validUnique:{actualMeasuredHz:59.8}},orientation:{validUnique:{actualMeasuredHz:0}}}});
  model.emissionRate.metrics=()=>({actualMeasuredHz:49.5});transport.metrics={rtt:[3,1,8,2]};
  assert.deepEqual(transport.qaTelemetry(),{qaVersion:1,motionHz:59.8,orientationHz:0,emissionHz:49.5,rttP50:2,rttP95:8});
  assert.equal(JSON.stringify(transport.qaTelemetry()).includes('UNIT_SECRET'),false);
  await model.dispose();assert.equal(transport.qaTelemetry,null);
});

test('ready control preview follows local tilt without inventing game balance or sensor values', async () => {
  let at = 0;
  const model = new ControllerSession({ win: win(), url, client: client(), now: () => at });
  model.network = 'NETWORK_READY'; model.capture.state = 'RUNNING';
  model.capture.inputState = () => 'INPUT_READY';
  const sample = gamma => model.capture.onSample({ channels: {
    motion: { validSequence: 0 }, orientation: { validSequence: 0, latest: { angles: { gamma } } },
  } });
  sample(10); assert.equal(model.snapshot().controlTilt, 0);
  at = 60; sample(40); assert.equal(model.snapshot().controlTilt, 1);
  at = 120; sample(-60); assert.equal(model.snapshot().controlTilt, -1);
  at = 180; sample(null); assert.equal(model.snapshot().controlTilt, -1);
  assert.equal('gameBalance' in model.snapshot(), false);
  await model.dispose();
});

test('connection check verifies HTTPS health without sending admission or sensor data', async () => {
  const transport=client();let requested;
  transport.fetcher=async(path,options)=>{requested={path,options};return {ok:true,json:async()=>({status:'ok',service:'gorilla-escape-local',secure:true})};};
  const model=new ControllerSession({win:win(),url,client:transport});await model.checkConnection();
  assert.equal(model.snapshot().connectionCheck,'READY');assert.equal(requested.path,'/api/health');assert.equal(requested.options.cache,'no-store');assert.equal(requested.options.body,undefined);
  transport.fetcher=async()=>{throw Error('PRIVATE_CERTIFICATE_ERROR');};await model.checkConnection();
  assert.equal(model.snapshot().connectionCheck,'UNREACHABLE');assert.equal(JSON.stringify(model.snapshot()).includes('PRIVATE'),false);await model.dispose();
});
test('connection check reports insecure context without a network call', async () => {
  const transport=client();transport.fetcher=()=>{throw Error('must not call');};
  const model=new ControllerSession({win:win({isSecureContext:false}),url,client:transport});await model.checkConnection();
  assert.equal(model.snapshot().connectionCheck,'INSECURE');await model.dispose();
});

test('public tunnel connection check uses only its allowed landing page', async () => {
  const transport=client();let path;
  transport.fetcher=async value=>{path=value;return {ok:true,text:async()=>'<title>Gorilla Escape</title>'};};
  const model=new ControllerSession({win:win(),url:'https://test-controller.trycloudflare.com/#sessionId=test&admission=UNIT_SECRET',client:transport});
  await model.checkConnection();assert.equal(path,'/');assert.equal(model.snapshot().connectionCheck,'READY');assert.equal(model.snapshot().publicTunnel,true);await model.dispose();
});
test('optional player registration posts once and never retries an uncertain write', async () => {
 const transport=client(); transport.identity={peerId:'test-peer',resumeToken:'PRIVATE_RESUME'};
 let calls=0;
 transport.fetcher=async(path,options)=>{calls++;assert.equal(path,'/mobile/profile');assert.equal(options.method,'POST');throw new TypeError('network');};
 const model=new ControllerSession({win:win(),url,client:transport});await model.connect();
 assert.equal(await model.createProfile('Náufrago'),'UNCONFIRMED');
 assert.equal(await model.createProfile('Náufrago'),'UNAVAILABLE');assert.equal(calls,1);
 assert.equal(JSON.stringify(model.diagnostic()).includes('PRIVATE_RESUME'),false);
 await model.dispose();
});
test('game presentation expires when authenticated heartbeat context stops arriving', async () => {
 const transport=client();transport.gameContext={version:1,gameId:'log-balance',phase:'Crossing',playerId:1};
 transport.gameContextExpires=performance.now()+1000;
 const model=new ControllerSession({win:win(),url,client:transport});
 assert.equal(model.snapshot().gameContext.phase,'Crossing');
 transport.gameContextExpires=performance.now()-1;
 assert.equal(model.snapshot().gameContext,null);await model.dispose();
});

test('retry reuses granted sensor permissions instead of prompting again', async () => {
  let requests=0, starts=0;
  const api={requestPermission:()=>{requests++;return Promise.resolve('granted');}};
  const model=new ControllerSession({win:win({DeviceMotionEvent:api,DeviceOrientationEvent:api}),url,client:client()});
  await model.connect();
  model.capture.start=async()=>{starts++;};
  await model.activate();
  model.state='NO_SENSOR_INPUT';
  await model.activate();
  await model.dispose();
  assert.equal(requests,2);assert.equal(starts,2);
});
test('screen wake lock is reacquired after OS release and released on exit', async () => {
  let requests=0, releases=0;
  const lock={released:false,async release(){this.released=true;releases++;}};
  const w=win({navigator:{wakeLock:{async request(){requests++;lock.released=false;return lock;}}}});
  const model=new ControllerSession({win:w,url,client:client()});
  model.wakeWanted=true;await model.keepAwake();await model.keepAwake();assert.equal(requests,1);
  lock.released=true;w.document.dispatchEvent(new Event('visibilitychange'));await Promise.resolve();
  assert.equal(requests,2);await model.dispose();assert.equal(releases,1);
});

test('invalid profiles never write and known unavailability differs from an uncertain POST',async()=>{
  let calls=0;
  const model={disposed:false,network:'NETWORK_READY',profileAttempted:false,
    win:{setTimeout,clearTimeout},client:{identity:{peerId:'fixture',resumeToken:'fixture'},fetcher:async()=>{calls++;return {status:503,ok:false};}}};
  assert.equal(await ControllerSession.prototype.createProfile.call(model,'🙂'),'INVALID');assert.equal(calls,0);assert.equal(model.profileAttempted,false);
  assert.equal(await ControllerSession.prototype.createProfile.call(model,'Jugador'),'UNAVAILABLE');assert.equal(model.profileAttempted,false);
  model.client.fetcher=async()=>{calls++;throw new TypeError('fixture network failure');};
  assert.equal(await ControllerSession.prototype.createProfile.call(model,'Jugador'),'UNCONFIRMED');assert.equal(model.profileAttempted,true);
  assert.equal(await ControllerSession.prototype.createProfile.call(model,'Jugador'),'UNAVAILABLE');assert.equal(calls,2);
});

test('player name is validated and submitted with the active session', async () => {
 const transport=client();transport.identity={peerId:'peer',resumeToken:'secret'};
 let sent;transport.fetcher=async(path,options)=>{sent={path,body:JSON.parse(options.body)};return {ok:true,json:async()=>({displayName:'Josué'})};};
 const model=new ControllerSession({win:win(),url,client:transport});await model.connect();
 assert.equal(await model.setPlayerName(' Josué '),'SAVED');assert.equal(sent.path,'/mobile/name');assert.equal(sent.body.displayName,'Josué');
 assert.equal(await model.setPlayerName(' '),'INVALID');await model.dispose();
});
