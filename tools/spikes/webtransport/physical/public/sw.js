const VERSION='3a-st-2026-10-07-r1',FILES=['./','./index.html','./lab.js','./capture.js','./wt.js','./sw.js'];
self.addEventListener('install',event=>event.waitUntil(caches.open(VERSION).then(c=>c.addAll(FILES)).then(()=>self.skipWaiting())));
self.addEventListener('activate',event=>event.waitUntil(self.clients.claim()));
self.addEventListener('fetch',event=>{if(event.request.method!=='GET'||new URL(event.request.url).origin!==self.location.origin)return;event.respondWith(caches.match(event.request).then(c=>c||fetch(event.request)));});
