import { readdir, readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
const files = [];
async function collect(dir, prefix = '') {
  for (const item of await readdir(dir, { withFileTypes: true })) {
    const relative = prefix + item.name;
    if (item.isDirectory()) await collect(dir + '/' + item.name, relative + '/');
    else if (item.name !== 'sw.js') files.push(relative);
  }
}
await collect('dist');
files.sort();
const hash = createHash('sha256');
for (const file of files) hash.update(await readFile('dist/' + file));
const cache = 'gorilla-shell-' + hash.digest('hex').slice(0, 16);
const urls = files.map(file => '/' + file);
const source = `const CACHE = ${JSON.stringify(cache)};
const URLS = ${JSON.stringify(urls)};
self.addEventListener('install', event => {
  event.waitUntil(caches.open(CACHE).then(cache => cache.addAll(URLS)));
});
self.addEventListener('activate', event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(k => k.startsWith('gorilla-shell-') && k !== CACHE).map(k => caches.delete(k)))).then(() => self.clients.claim()));
});
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);
  if (event.request.method !== 'GET' || url.origin !== self.location.origin || url.pathname.startsWith('/api/')) return;
  const path = url.pathname === '/' ? '/index.html' : url.pathname;
  if (!URLS.includes(path)) return;
  event.respondWith(caches.open(CACHE).then(cache => cache.match(path)).then(hit => hit || fetch(event.request)));
});
`;
await writeFile('dist/sw.js', source);
console.log('Offline shell generated:', cache);
