import subprocess, time, urllib.request, urllib.parse, json, os, shutil
from cdp_pwa_client import MinimalCDPClient, open_pwa_tab

def test():
    user_data = os.path.join(os.environ['LOCALAPPDATA'], 'Temp', 'chrome_pwa_test1')
    if os.path.exists(user_data):
        shutil.rmtree(user_data, ignore_errors=True)
    proc = subprocess.Popen([
        r'C:\Program Files\Google\Chrome\Application\chrome.exe',
        '--headless=new',
        '--remote-debugging-port=9222',
        f'--user-data-dir={user_data}',
        'about:blank'
    ])
    time.sleep(2)
    try:
        req = urllib.request.Request('http://127.0.0.1:8080/mobile/admission', 
                                     headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'}, 
                                     method='POST')
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode())
        pwa_url = data['url']
        print('PWA Admission URL:', pwa_url)

        tab = open_pwa_tab(9222, 'about:blank')
        ws_url = tab['webSocketDebuggerUrl']
        print('Tab WS URL:', ws_url)

        client = MinimalCDPClient(ws_url)
        # Navigate via CDP so #fragment is never stripped by HTTP GET
        client.send_cdp("Page.navigate", {"url": pwa_url})
        time.sleep(2)

        val = client.eval('document.title')
        print('Page Title:', val)
        loc = client.eval('location.href')
        print('Current Location in Chrome:', loc)

        # Click JOIN GAME
        client.eval("document.querySelector('#join').click()")
        time.sleep(3)
        state = client.eval("document.querySelector('#state').textContent")
        print('Status text in PWA:', state)
        diag = client.eval('JSON.stringify(window.client?.diagnostic())')
        print('PWA Diagnostic:', diag)

        s_diag_req = urllib.request.Request('http://127.0.0.1:8080/mobile/diagnostics', 
                                            headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'})
        with urllib.request.urlopen(s_diag_req) as s_resp:
            s_diag = json.loads(s_resp.read().decode())
        print('Server Diagnostics:', json.dumps(s_diag, indent=2))
    finally:
        proc.terminate()
        proc.wait()

if __name__ == '__main__':
    test()
