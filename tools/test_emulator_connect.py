import urllib.request, json, subprocess, time, re

def main():
    # 1. Get fresh admission from server
    req = urllib.request.Request('http://127.0.0.1:8080/mobile/admission', 
                                 headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'}, 
                                 method='POST')
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode())
    raw_url = data['url']
    frag = raw_url.split('#')[1]
    app_url = f'http://127.0.0.1:8080/#{frag}'
    print('Connecting with URL:', app_url)

    device = 'emulator-5554'
    # 2. Launch Companion
    subprocess.run(['adb', '-s', device, 'shell', 'am', 'force-stop', 'com.gorillaescape.gorilla_companion'])
    time.sleep(1)
    subprocess.run(['adb', '-s', device, 'shell', 'am', 'start', '-n', 'com.gorillaescape.gorilla_companion/.MainActivity'])
    time.sleep(3)

    # 3. Scroll down to show expansion tile
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'swipe', '240', '650', '240', '350', '300'])
    time.sleep(1)

    # Dump UI
    subprocess.run(['adb', '-s', device, 'shell', 'uiautomator', 'dump', '/data/local/tmp/ui.xml'])
    xml = subprocess.check_output(['adb', '-s', device, 'shell', 'cat', '/data/local/tmp/ui.xml']).decode('utf-8', errors='ignore')
    
    # Find "Tengo un enlace de partida" bounds in text or content-desc
    m = re.search(r'(?:text|content-desc)="[^"]*Tengo un enlace de partida[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"', xml)
    if m:
        cx = (int(m.group(1)) + int(m.group(3))) // 2
        cy = (int(m.group(2)) + int(m.group(4))) // 2
        print(f'Tapping expansion tile at ({cx}, {cy})')
        subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', str(cx), str(cy)])
        time.sleep(1)
    else:
        print('Could not find expansion tile in dump')

    # Scroll up to bring expanded contents into full view
    subprocess.run(['adb', '-s', device, 'shell', 'input', 'swipe', '240', '650', '240', '250', '300'])
    time.sleep(1)

    # Dump UI after expanding
    subprocess.run(['adb', '-s', device, 'shell', 'uiautomator', 'dump', '/data/local/tmp/ui2.xml'])
    xml2 = subprocess.check_output(['adb', '-s', device, 'shell', 'cat', '/data/local/tmp/ui2.xml']).decode('utf-8', errors='ignore')
    
    # Find EditText bounds
    m_edit = re.search(r'class="android.widget.EditText"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"', xml2)
    if m_edit:
        cx = (int(m_edit.group(1)) + int(m_edit.group(3))) // 2
        cy = (int(m_edit.group(2)) + int(m_edit.group(4))) // 2
        print(f'Tapping EditText at ({cx}, {cy})')
        subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', str(cx), str(cy)])
        time.sleep(0.5)
        # Type the URL via adb (escaping # and &)
        safe_url = app_url.replace('&', '\\&').replace('#', '\\#')
        subprocess.run(['adb', '-s', device, 'shell', f'input text "{safe_url}"'])
        time.sleep(1)
    else:
        print('Could not find EditText in dump')

    # Dump UI to find "ABRIR CONTROL" in text or content-desc
    subprocess.run(['adb', '-s', device, 'shell', 'uiautomator', 'dump', '/data/local/tmp/ui3.xml'])
    xml3 = subprocess.check_output(['adb', '-s', device, 'shell', 'cat', '/data/local/tmp/ui3.xml']).decode('utf-8', errors='ignore')
    m_btn = re.search(r'(?:text|content-desc)="[^"]*ABRIR CONTROL[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"', xml3)
    if m_btn:
        cx = (int(m_btn.group(1)) + int(m_btn.group(3))) // 2
        cy = (int(m_btn.group(2)) + int(m_btn.group(4))) // 2
        print(f'Tapping ABRIR CONTROL at ({cx}, {cy})')
        subprocess.run(['adb', '-s', device, 'shell', 'input', 'tap', str(cx), str(cy)])
        time.sleep(4)
    else:
        print('Could not find ABRIR CONTROL button')

    # Check server diagnostics
    diag_req = urllib.request.Request('http://127.0.0.1:8080/mobile/diagnostics', 
                                      headers={'X-Gorilla-Operator': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'})
    with urllib.request.urlopen(diag_req) as resp:
        diag = json.loads(resp.read().decode())
    print('Server Diagnostics:', json.dumps(diag, indent=2))

if __name__ == '__main__':
    main()
