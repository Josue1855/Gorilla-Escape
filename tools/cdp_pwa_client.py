import socket, json, urllib.request, time, struct, base64, os

class MinimalCDPClient:
    def __init__(self, ws_url):
        # ws://127.0.0.1:9222/devtools/page/...
        host_port, path = ws_url.replace('ws://', '').split('/', 1)
        host, port = host_port.split(':')
        self.sock = socket.create_connection((host, int(port)), timeout=5)
        # WebSocket handshake (RFC 6455)
        key = base64.b64encode(os.urandom(16)).decode('utf-8')
        req = (
            f"GET /{path} HTTP/1.1\r\n"
            f"Host: {host_port}\r\n"
            f"Upgrade: websocket\r\n"
            f"Connection: Upgrade\r\n"
            f"Sec-WebSocket-Key: {key}\r\n"
            f"Sec-WebSocket-Version: 13\r\n\r\n"
        )
        self.sock.sendall(req.encode('utf-8'))
        resp = self.sock.recv(4096)
        if b"101 " not in resp:
            raise RuntimeError(f"WebSocket handshake failed: {resp[:100]}")
        self.msg_id = 0

    def send_cdp(self, method, params=None):
        self.msg_id += 1
        payload = json.dumps({'id': self.msg_id, 'method': method, 'params': params or {}}).encode('utf-8')
        # Frame with mask (client must mask frames)
        frame = bytearray([0x81]) # FIN + text opcode
        mask_key = os.urandom(4)
        length = len(payload)
        if length < 126:
            frame.append(0x80 | length)
        elif length < 65536:
            frame.append(0x80 | 126)
            frame.extend(struct.pack('>H', length))
        else:
            frame.append(0x80 | 127)
            frame.extend(struct.pack('>Q', length))
        frame.extend(mask_key)
        # Mask payload
        masked = bytearray(length)
        for i in range(length):
            masked[i] = payload[i] ^ mask_key[i % 4]
        frame.extend(masked)
        self.sock.sendall(frame)
        return self.msg_id

    def recv_cdp(self, expected_id=None, timeout=5):
        self.sock.settimeout(timeout)
        start = time.time()
        while time.time() - start < timeout:
            try:
                hdr = self.sock.recv(2)
            except (TimeoutError, socket.timeout):
                break
            if not hdr:
                break
            length = hdr[1] & 0x7f
            if length == 126:
                length = struct.unpack('>H', self.sock.recv(2))[0]
            elif length == 127:
                length = struct.unpack('>Q', self.sock.recv(8))[0]
            body = bytearray()
            while len(body) < length:
                chunk = self.sock.recv(min(4096, length - len(body)))
                if not chunk:
                    break
                body.extend(chunk)
            try:
                msg = json.loads(body.decode('utf-8'))
                if expected_id is None or msg.get('id') == expected_id:
                    return msg
            except Exception:
                pass
        return None

    def eval(self, expr, await_promise=False):
        mid = self.send_cdp("Runtime.evaluate", {
            "expression": expr,
            "awaitPromise": await_promise,
            "returnByValue": True
        })
        res = self.recv_cdp(expected_id=mid)
        return res.get('result', {}).get('result', {}).get('value')

    def close(self):
        try:
            self.sock.close()
        except:
            pass

def open_pwa_tab(port, url):
    # urlencode query
    encoded_url = urllib.parse.quote(url, safe=':/?#&=._-')
    req = urllib.request.Request(f'http://127.0.0.1:{port}/json/new?{encoded_url}', method='PUT')
    with urllib.request.urlopen(req) as resp:
        tab_info = json.loads(resp.read().decode())
    return tab_info

if __name__ == '__main__':
    print('Testing MinimalCDPClient')
