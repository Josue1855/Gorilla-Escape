import urllib.request, json, subprocess, time, os

def main():
    device = 'emulator-5556'
    # 1. Tap EditText
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', '240', '617'])
    time.sleep(1)

    # 2. Get fresh admission
    req = urllib.request.Request('http://127.0.0.1:8080/mobile/admission', 
                                 headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'}, 
                                 method='POST')
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode())
    frag = data['url'].split('#')[1]
    url = f'http://127.0.0.1:8080/#{frag}'
    print('Player 4 Admission URL:', url)

    # 3. Type URL
    p = subprocess.Popen(['adb', '-s', device, 'shell'], stdin=subprocess.PIPE, text=True)
    p.communicate(input=f"input text '{url}'\nexit\n", timeout=10)
    time.sleep(1)

    # 4. Tap ABRIR CONTROL
    # Try both possible positions (with keyboard open and closed)
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', '240', '427'])
    time.sleep(1)
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', '240', '681'])
    time.sleep(4)

    # 5. Check server diagnostics
    diag_req = urllib.request.Request('http://127.0.0.1:8080/mobile/diagnostics', 
                                      headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'})
    with urllib.request.urlopen(diag_req) as diag_resp:
        diag = json.loads(diag_resp.read().decode())
    print('Server Diagnostics Peers:', diag.get('peers'))
    print(json.dumps(diag, indent=2))

    # Pull screenshot
    screen_remote = f"/data/local/tmp/{device}_screen.png"
    screen_local = os.path.join(r"C:\Users\equipo\.gemini\antigravity\brain\2c221f95-4be5-4c9d-a5ca-eccb82e6b254", f"companion_player4_{device}.png")
    subprocess.run(["adb", "-s", device, "shell", "screencap", "-p", screen_remote])
    subprocess.run(["adb", "-s", device, "pull", screen_remote, screen_local])
    print(f"Captured screenshot for {device} at {screen_local}")

if __name__ == '__main__':
    main()
