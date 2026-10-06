import test from 'node:test';
import assert from 'node:assert/strict';
import { checkServer } from '../src/connection/health.js';

test('accepts the compatible local health response', async () => {
  let requested;
  const result = await checkServer(async (url, options) => {
    requested = url; assert.ok(options.signal);
    return { ok: true, json: async () => ({ status: 'ok', protocolVersion: 1 }) };
  });
  assert.equal(requested, '/api/health'); assert.equal(result.protocolVersion, 1);
});
test('rejects unavailable and incompatible servers', async () => {
  await assert.rejects(checkServer(async () => ({ ok: false })));
  await assert.rejects(checkServer(async () => ({ ok: true, json: async () => ({ status: 'ok', protocolVersion: 2 }) })));
});
test('propagates network and malformed response failures for recoverable UI', async () => {
  await assert.rejects(checkServer(async () => { throw new Error('offline'); }));
  await assert.rejects(checkServer(async () => ({ ok: true, json: async () => { throw new SyntaxError(); } })));
});
