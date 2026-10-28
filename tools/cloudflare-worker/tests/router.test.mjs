import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
const source = await readFile(new URL('../src/index.js', import.meta.url), 'utf8');
const { default: worker, RoomRegistry } = await import('data:text/javascript;base64,' + Buffer.from(source).toString('base64'));
function environment() {
  const rooms = new Map();
  return { ROOM_REGISTRATION_SECRET: 'secret', ROOM_REGISTRY: {
    idFromName(name) { return name; },
    get(name) {
      if (!rooms.has(name)) {
        const data = new Map();
        rooms.set(name, new RoomRegistry({ storage: { async get(key) { return data.get(key); }, async put(key, value) { data.set(key, value); } }, async blockConcurrencyWhile(fn) { return fn(); } }));
      }
      return { fetch(url, options) { return rooms.get(name).fetch(new Request(url, options)); } };
    }
  }};
}
const owner = '12345678-1234-1234-1234-123456789012';
const register = (body, secret = 'secret') => new Request('https://play.gorillas.run/api/register-room', { method:'POST', headers:{ Authorization:`Bearer ${secret}`, 'Content-Type':'application/json' }, body:JSON.stringify(body) });
test('registration requires secret, validates destination and protects room ownership', async () => {
  const env = environment(), body = { roomCode:'abcd', targetUrl:'https://test.trycloudflare.com', owner };
  assert.equal((await worker.fetch(register(body, 'wrong'), env)).status, 401);
  assert.equal((await worker.fetch(register({ ...body, targetUrl:'https://example.com' }), env)).status, 400);
  assert.equal((await worker.fetch(register(body), env)).status, 200);
  assert.equal((await worker.fetch(register({ ...body, owner:'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' }), env)).status, 409);
  assert.equal((await worker.fetch(register({ ...body, targetUrl:'https://next.trycloudflare.com' }), env)).status, 200);
});
test('room routes reach their tunnel without a referrer and only allow public endpoints', async () => {
  const env = environment(); await worker.fetch(register({ roomCode:'abcd', targetUrl:'https://test.trycloudflare.com', owner }), env);
  const original = globalThis.fetch; const seen = [];
  globalThis.fetch = async request => { seen.push(request.url); return new Response('<head><script src="/assets/main.js"></script></head>', {headers:{'content-type':'text/html'}}); };
  try {
    const page = await worker.fetch(new Request('https://play.gorillas.run/abcd/'), env);
    const html = await page.text(); assert.match(html, /<base href="\/abcd\/">/); assert.match(html, /src="assets\/main.js"/);
    assert.equal((await worker.fetch(new Request('https://play.gorillas.run/abcd/mobile/join', { method:'POST', body:'{}' }), env)).status, 200);
    assert.equal((await worker.fetch(new Request('https://play.gorillas.run/abcd/assets/LilitaOne-a1.ttf'), env)).status, 200);
    assert.equal((await worker.fetch(new Request('https://play.gorillas.run/abcd/private'), env)).status, 404);
    assert.deepEqual(seen, ['https://test.trycloudflare.com/', 'https://test.trycloudflare.com/mobile/join', 'https://test.trycloudflare.com/assets/LilitaOne-a1.ttf']);
  } finally { globalThis.fetch = original; }
});
