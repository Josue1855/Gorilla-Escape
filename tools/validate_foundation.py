"""Check foundation wiring; this is not an engine compile or hardware test."""
import json,re
from pathlib import Path
r=Path(__file__).resolve().parents[1]
m=json.loads((r/'Unity/Packages/manifest.json').read_text())
p=m['dependencies']['com.gorillaescape.contracts'].removeprefix('file:')
assert (r/'Unity/Packages'/p/'package.json').resolve().exists()
assert '6000.3.23f1' in (r/'Unity/ProjectSettings/ProjectVersion.txt').read_text()
guids=set()
for base in [r/'Unity/Assets',r/'Shared/CSharp']:
 for f in base.rglob('*'):
  if f.suffix=='.meta':
   match=re.search(r'^guid: ([a-f0-9]{32})$',f.read_text(),re.M)
   assert match and match[1] not in guids,str(f)
   guids.add(match[1])
  elif f.is_file() and f.name!='package.json':assert Path(str(f)+'.meta').exists(),str(f)
for f in (r/'Shared/CSharp/Runtime').glob('*.cs'):assert 'using UnityEngine' not in f.read_text()
assert json.loads((r/'Shared/CSharp/Runtime/GorillaEscape.Contracts.asmdef').read_text())['noEngineReferences']
fixture=json.loads((r/'Shared/Protocol/fixtures/sensor.json').read_text())
assert fixture['version']==1 and fixture['payload']['orientation']['w']==1
manifest=json.loads((r/'PWA/public/manifest.webmanifest').read_text())
for icon in manifest['icons']:assert (r/'PWA/public'/icon['src'].lstrip('/')).exists()
assert 'ASP.NET' not in (r/'docs/DEVELOPMENT_RULES.md').read_text()
assert 'ASP.NET' not in (r/'docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md').read_text()
print(f'PASS: local UPM, {len(guids)} unique meta GUIDs, engine-independent contracts, fixture, PWA icons and amended architecture.')
