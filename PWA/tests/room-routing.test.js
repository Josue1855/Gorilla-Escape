import { test } from 'node:test';
import assert from 'node:assert/strict';
import { MobileClient } from '../src/input/mobileClient.js';
test('public room requests carry the room prefix without changing local paths', async () => {
 const previous = globalThis.location, paths = [];
 try {
  globalThis.location = { hostname:'play.gorillas.run', pathname:'/abcd/' };
  const client = new MobileClient({ fetcher: async path => { paths.push(path); } });
  await client.fetcher('/mobile/join'); await client.fetcher('/');
  globalThis.location = { hostname:'192.168.1.2', pathname:'/' };
  await client.fetcher('/mobile/join');
  assert.deepEqual(paths, ['/abcd/mobile/join', '/abcd/', '/mobile/join']);
 } finally { globalThis.location = previous; }
});
