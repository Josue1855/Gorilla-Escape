const json = (body, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' } });
const roomPattern = /^[a-z0-9]{4,8}$/;
const targetPattern = /^https:\/\/[a-z0-9-]{1,63}\.trycloudflare\.com$/;

const ASSET_LINKS = [
  {
    relation: ['delegate_permission/common.handle_all_urls'],
    target: {
      namespace: 'android_app',
      package_name: 'com.gorillaescape.gorilla_companion',
      sha256_cert_fingerprints: [
        '9F:F5:56:BF:06:8A:02:2B:E4:FD:1D:48:44:4C:48:ED:D2:D9:15:47:52:12:6B:70:0A:1E:FD:C7:6E:EC:37:56',
        '99:7D:B2:B7:54:DE:CA:14:94:30:B7:97:E5:0C:C5:54:7C:BD:58:FA:CA:43:8D:34:70:04:11:D0:24:14:26:2F'
      ]
    }
  }
];

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

    // 1. Android App Links verification
    if (url.pathname === '/.well-known/assetlinks.json' && request.method === 'GET') {
      return new Response(JSON.stringify(ASSET_LINKS, null, 2), {
        status: 200,
        headers: {
          'Content-Type': 'application/json',
          'Access-Control-Allow-Origin': '*',
          'Cache-Control': 'public, max-age=3600'
        }
      });
    }

    // 2. Room Registration API
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

    // 3. Room routing & join resolution
    let parts = url.pathname.split('/').filter(Boolean);
    if (!parts.length) return new Response('Gorilla Escape: escanea el código QR de tu partida.', { headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
    if (parts[0].toLowerCase() === 'join' && parts.length > 1) {
      parts.shift(); // remove 'join', next part is the room
    }
    const room = parts.shift().toLowerCase();
    if (!roomPattern.test(room)) return new Response('Sala no encontrada.', { status: 404 });
    const lookup = await registry(env, room).fetch('https://room.internal/');
    const target = lookup.ok ? (await lookup.json()).target : null;
    if (!target || !targetPattern.test(target)) return new Response('Sala no encontrada o expirada. Escanea el QR vigente.', { status: 404 });
    const path = '/' + parts.join('/') + (parts.length && url.pathname.endsWith('/') ? '/' : '');
    const allowed = request.method === 'GET' && (['/', '/index.html', '/manifest.webmanifest', '/sw.js', '/icons/icon-192.png', '/icons/icon-512.png'].includes(path) || /^\/assets\/[A-Za-z0-9_-]{1,100}\.(js|css|ttf)$/.test(path)) || request.method === 'POST' && ['/mobile/join', '/mobile/reconnect', '/mobile/disconnect', '/mobile/forfeit', '/mobile/profile', '/mobile/name'].includes(path);
    if (!allowed) return new Response('No encontrado.', { status: 404 });
    try {
      const headers = new Headers(request.headers);
      headers.delete('Authorization');
      const response = await fetch(new Request(new URL(path + url.search, target), { method: request.method, headers, body: request.method === 'POST' ? request.body : undefined, duplex: 'half', redirect: 'manual' }));
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
