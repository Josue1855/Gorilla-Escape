// Focused QA checks: retained emulator measurements + bounded native anomaly replay.
// Never changes product policy or upgrades historical nominal FAIL to PASS.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { SensorCapture } from '../../PWA/src/input/sensors/capture.js';
const [alphaPath, restPath, gentlePath] = process.argv.slice(2);
if (!alphaPath || !restPath || !gentlePath) throw Error('alpha diagnostic, rest, gentle evidence paths required');
const alpha = JSON.parse(await readFile(alphaPath, 'utf8'));
const measurements = await Promise.all([restPath, gentlePath].map(async path => JSON.parse(await readFile(path, 'utf8'))));
function fixture() {
  const win = new EventTarget(), doc = new EventTarget(); let now = 0;
  Object.assign(win, { isSecureContext: true, DeviceMotionEvent: {}, DeviceOrientationEvent: {} });
  doc.visibilityState = 'visible';
  const capture = new SensorCapture({ win, doc, now: () => now, setTimer: () => 1, clearTimer() {} });
  const send = (type, fields, at) => {
    now = at; const event = new Event(type);
    for (const [key, value] of Object.entries({ ...fields, timeStamp: at })) Object.defineProperty(event, key, { value });
    win.dispatchEvent(event);
  };
  return { capture, send };
}
test('bounded native alpha diagnostic retains at most eight finite violations', () => {
  assert.ok(alpha.alphaViolationCount > 0, 'anomaly must actually reproduce');
  assert.ok(alpha.firstAlphaViolations.length > 0 && alpha.firstAlphaViolations.length <= 8);
  assert.ok(alpha.firstAlphaViolations.every(value => Number.isFinite(value) && (value < 0 || value >= 360)));
  assert.ok(alpha.alphaViolationMin <= alpha.alphaViolationMax);
  assert.equal(alpha.orientationRangeRejected.beta, 0); assert.equal(alpha.orientationRangeRejected.gamma, 0);
});
test('actual emulator alpha violations replay into maintained capture as null; zero beta/gamma retained', async () => {
  const f = fixture(); await f.capture.start();
  try {
    for (const [index, value] of alpha.firstAlphaViolations.entries()) {
      f.send('deviceorientation', { alpha: value, beta: 0, gamma: 0, absolute: false }, (index + 1) * 20);
      assert.deepEqual(f.capture.snapshot().channels.orientation.latest.angles, { alpha: null, beta: 0, gamma: 0 });
    }
    assert.equal(f.capture.metrics().channels.orientation.invalidFields, alpha.firstAlphaViolations.length);
  } finally { f.capture.stop(); }
});
test('unchanged freshness policy keeps old orientation as state, does not invent events or Hz', async () => {
  const f = fixture(); await f.capture.start();
  const motion = { acceleration: { x: 0, y: 0, z: 0 }, accelerationIncludingGravity: { x: 0, y: 0, z: 9.81 }, rotationRate: { alpha: 0, beta: 0, gamma: 0 }, interval: 20 };
  try {
    for (const at of [10, 30, 50]) {
      f.send('devicemotion', motion, at); f.send('deviceorientation', { alpha: 0, beta: 0, gamma: 0 }, at);
    }
    assert.equal(f.capture.inputState(), 'INPUT_READY');
    const prior = f.capture.metrics().channels.orientation;
    f.send('devicemotion', motion, 3000);
    assert.equal(f.capture.inputState(), 'NO_SENSOR_INPUT');
    const later = f.capture.metrics().channels.orientation;
    assert.equal(later.validSamples, prior.validSamples);
    assert.equal(later.validUnique.actualMeasuredHz, prior.validUnique.actualMeasuredHz);
    assert.deepEqual(f.capture.snapshot().channels.orientation.latest.angles, { alpha: 0, beta: 0, gamma: 0 });
  } finally { f.capture.stop(); }
});
for (const result of measurements) test(`${result.mode} emulator: separate real callback rate and budgeted transport with zero errors`, () => {
  const m = result.measurement;
  assert.equal(m.status, 'COMPLETE'); assert.equal(m.capture.captureRevision, 't010-2-coarsened-clock');
  assert.equal(m.emissionPolicy.maximumHz, 50); assert.ok(m.emission.actualMeasuredHz > 0 && m.emission.actualMeasuredHz <= 51);
  for (const channel of Object.values(m.capture.channels)) {
    assert.ok(channel.validSamples >= 3); assert.equal(channel.duplicateOrOutOfOrder, 0); assert.equal(channel.timingRejected, 0);
    assert.ok(Number.isFinite(channel.validUnique.actualMeasuredHz));
  }
  assert.equal(m.transportDelta.motionErrors, 0); assert.equal(m.transportDelta.errors, 0);
  assert.equal(result.gate, 'FAIL', 'historical nominal FAIL stays unchanged');
});
