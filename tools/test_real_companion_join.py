import subprocess, time, urllib.request, json

def connect_emulator(device='emulator-5554'):
    # 1. Get admission
    req = urllib.request.Request('http://127.0.0.1:8080/mobile/admission', 
                                 headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'}, 
                                 method='POST')
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode())
    frag = data['url'].split('#')[1]
    url = f'http://127.0.0.1:8080/#{frag}'
    print(f'[{device}] Admitted: {url}')

    # 2. Launch App
    subprocess.run(['adb', '-s', device, 'shell', 'am', 'force-stop', 'com.gorillaescape.gorilla_companion'])
    time.sleep(1)
    subprocess.run(['adb', '-s', device, 'shell', 'am', 'start', '-n', 'com.gorillaescape.gorilla_companion/.MainActivity'])
    time.sleep(3)

    # 3. Expand tile
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'swipe', '240', '650', '240', '350', '300'])
    time.sleep(1)
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', '240', '685'])
    time.sleep(1)

    # 4. Scroll to view and tap EditText
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'swipe', '240', '650', '240', '250', '300'])
    time.sleep(1)
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', '240', '617'])
    time.sleep(1)

    # 5. Type URL
    p = subprocess.Popen(['adb', '-s', device, 'shell'], stdin=subprocess.PIPE, text=True)
    p.stdin.write(f"input text '{url}'\n")
    p.stdin.flush()
    time.sleep(2)
    p.stdin.write("exit\n")
    p.stdin.flush()
    p.wait()
    time.sleep(1)

    # 6. Tap ABRIR CONTROL
    print(f'[{device}] Tapping ABRIR CONTROL')
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', '240', '427'])
    time.sleep(4)

    # 7. Check server diagnostics
    diag_req = urllib.request.Request('http://127.0.0.1:8080/mobile/diagnostics', 
                                      headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'})
    with urllib.request.urlopen(diag_req) as resp:
        diag = json.loads(resp.read().decode())
    print('Server Diagnostics:', json.dumps(diag, indent=2))

    # 8. Dump screen UI and take screenshot
    subprocess.run(['adb', '-s', device, 'shell', 'screencap', '-p', f'/data/local/tmp/{device}_screen.png'])
    subprocess.run(['adb', '-s', device, 'pull', f'/data/local/tmp/{device}_screen.png', f'{device}_connected.png'])

if __name__ == '__main__':
    connect_emulator('emulator-5554')
