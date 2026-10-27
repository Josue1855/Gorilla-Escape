const json = (body, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' } });
const roomPattern = /^[a-z0-9]{4,8}$/;
const targetPattern = /^https:\/\/[a-z0-9-]{1,63}\.trycloudflare\.com$/;

// One strongly consistent object per room serializes claims and renewals.
export class RoomRegistry {
  constructor(state) { this.state = state; }
  async fetch(request) {
    if (request.method === 'POST') {
      const { target, owner } = await request.json();
      return this.state.blockConcurrencyWhile(async () => {
        const current = await this.state.storage.get('room');
        if (current && current.expires > Date.now() && current.owner !== owner) return json({ error: 'ROOM_IN_USE' }, 409);
        await this.state.storage.put('room', { target, owner, expires: Date.now() + 10800000 });
        return json({ ok: true, version: 2 });
      });
    }
    const current = await this.state.storage.get('room');
    return current && current.expires > Date.now() ? json({ target: current.target }) : json({ error: 'NOT_FOUND' }, 404);
  }
}
const registry = (env, room) => env.ROOM_REGISTRY.get(env.ROOM_REGISTRY.idFromName(room));

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (url.pathname === '/api/register-room' && request.method === 'POST') {
      // Deployment must set this secret; never allow registration on an unconfigured worker.
      if (!env.ROOM_REGISTRATION_SECRET || request.headers.get('Authorization') !== `Bearer ${env.ROOM_REGISTRATION_SECRET}`)
        return json({ error: 'UNAUTHORIZED' }, 401);
      let body;
      try { body = await request.json(); } catch { return json({ error: 'INVALID_JSON' }, 400); }
      const code = typeof body.roomCode === 'string' ? body.roomCode.toLowerCase() : '';
      const target = body.targetUrl;
      const owner = body.owner;
      if (!roomPattern.test(code) || typeof target !== 'string' || !targetPattern.test(target) || typeof owner !== 'string' || !/^[a-f0-9-]{36}$/.test(owner)) return json({ error: 'INVALID_REGISTRATION' }, 400);
      return registry(env, code).fetch('https://room.internal/', { method: 'POST', body: JSON.stringify({ target, owner }) });
    }
    const parts = url.pathname.split('/').filter(Boolean);
    if (!parts.length) return new Response('Gorilla Escape: escanea el código QR de tu partida.', { headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
    const room = parts.shift().toLowerCase();
    if (!roomPattern.test(room)) return new Response('Sala no encontrada.', { status: 404 });
    const lookup = await registry(env, room).fetch('https://room.internal/');
    const target = lookup.ok ? (await lookup.json()).target : null;
    if (!target || !targetPattern.test(target)) return new Response('Sala no encontrada o expirada. Escanea el QR vigente.', { status: 404 });
    const path = '/' + parts.join('/') + (parts.length && url.pathname.endsWith('/') ? '/' : '');
    const allowed = request.method === 'GET' && (['/', '/index.html', '/manifest.webmanifest', '/sw.js', '/icons/icon-192.png', '/icons/icon-512.png'].includes(path) || /^\/assets\/[A-Za-z0-9_-]{1,100}\.(js|css)$/.test(path)) || request.method === 'POST' && ['/mobile/join', '/mobile/reconnect', '/mobile/disconnect', '/mobile/profile', '/mobile/name'].includes(path);
    if (!allowed || url.search) return new Response('No encontrado.', { status: 404 });
    try {
      const headers = new Headers(request.headers);
      headers.delete('Authorization');
      const response = await fetch(new Request(new URL(path, target), { method: request.method, headers, body: request.method === 'POST' ? request.body : undefined, duplex: 'half', redirect: 'manual' }));
      if (response.status >= 300 && response.status < 400) return new Response('Destino no disponible.', { status: 502 });
      if (path === '/manifest.webmanifest' && response.ok) {
        const manifest = await response.json();
        manifest.start_url = `/${room}/`; manifest.scope = `/${room}/`;
        for (const icon of manifest.icons || []) if (icon.src?.startsWith('/')) icon.src = `/${room}${icon.src}`;
        return new Response(JSON.stringify(manifest), { status: response.status, headers: { 'Content-Type': 'application/manifest+json', 'Cache-Control': 'no-store' } });
      }
      if ((response.headers.get('content-type') || '').includes('text/html')) {
        let html = await response.text();
        html = html.replace('<head>', `<head><base href="/${room}/">`).replace(/(src|href)="\/(assets\/|manifest\.webmanifest)/g, '$1="$2');
        const out = new Headers(response.headers); out.delete('Content-Length');
        return new Response(html, { status: response.status, headers: out });
      }
      return response;
    } catch { return new Response('No se pudo conectar con la partida.', { status: 502 }); }
  }
};
