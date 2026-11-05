import os

words = ['confianza', 'vigente', 'repetir', 'preparaci', 'permiso']

target_dirs = ['PWA/src', 'Companion/lib', 'Server/src', 'Unity/Assets/Scripts']
for d in target_dirs:
    for root, _, files in os.walk(d):
        for f in files:
            p = os.path.join(root, f)
            try:
                with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                    content = fp.read().lower()
                    for w in words:
                        if w in content:
                            print(f'Found "{w}" in: {p}')
            except Exception as e:
                pass
