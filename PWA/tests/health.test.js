import test from 'node:test';
import assert from 'node:assert/strict';
import { checkServer } from '../src/connection/health.js';

test('accepts the compatible local health response', async () => {
  let requested;
  const result = await checkServer(async (url, options) => {
    requested = url; assert.ok(options.signal); assert.equal(options.cache, 'no-store');
    return { ok: true, json: async () => ({ status: 'ok', protocolVersion: 1, service: 'gorilla-escape-local', serverInstanceId: '12345678-1234-1234-1234-123456789abc', requestNumber: 1, secure: true }) };
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

test('rejects stale/incomplete health, without inventing a connected session', async () => {
  const base = { status: 'ok', protocolVersion: 1, service: 'gorilla-escape-local', serverInstanceId: '12345678-1234-1234-1234-123456789abc', requestNumber: 1, secure: true };
  for (const patch of [{ serverInstanceId: 'wrong' }, { requestNumber: 0 }, { secure: undefined }, { service: 'other' }]) {
    await assert.rejects(checkServer(async () => ({ ok: true, json: async () => ({ ...base, ...patch }) })));
  }
});
