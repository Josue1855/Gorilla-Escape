import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

test('shared wire example preserves fields, scalar vectors and version', () => {
  const fixture = JSON.parse(readFileSync(new URL('../../Shared/Protocol/fixtures/sensor.json', import.meta.url)));
  assert.deepEqual(Object.keys(fixture).sort(), ['version','type','sessionId','playerId','sequence','timestamp','payload'].sort());
  assert.equal(fixture.version, 1); assert.equal(fixture.payload.orientation.w, 1);
  assert.deepEqual(JSON.parse(JSON.stringify(fixture)), fixture);
});
