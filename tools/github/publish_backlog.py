"""Idempotent creation of specified backlog; never assigns users or overwrites existing items."""
import json, os, re, subprocess, time
from pathlib import Path
from collections import Counter
GH=os.environ.get('GH_BINARY','gh');ROOT=Path(__file__).resolve().parents[2]
DATA=ROOT/'docs/github';manifest=json.loads((DATA/'backlog.json').read_text());state=json.loads((DATA/'project_state.json').read_text());REPO=state['repository'];PID=state['project_id']
receipt_path=DATA/'publish_receipt.json';receipt=json.loads(receipt_path.read_text()) if receipt_path.exists() else {'repository':REPO,'issues':{},'relationships':{},'created_this_execution':0}
def api(endpoint,method='GET',payload=None,pages=False):
 args=[GH,'api',endpoint,'-X',method,'-H','X-GitHub-Api-Version: 2026-03-10']
 if pages:args+=['--paginate','--slurp']
 if payload is not None:args+=['--input','-']
 for attempt in range(3):
  r=subprocess.run(args,input=json.dumps(payload) if payload is not None else None,capture_output=True,text=True)
  if r.returncode==0:
   out=json.loads(r.stdout) if r.stdout.strip() else {}
   if isinstance(out,dict) and out.get('errors'):raise RuntimeError(str(out['errors']))
   if pages:return [x for page in out for x in page]
   return out
  if not any(x in r.stderr for x in ['429','502','503','504']):break
  time.sleep(2*(attempt+1))
 raise RuntimeError(endpoint+': '+r.stderr[:500])
def gql(query,variables):return api('graphql','POST',{'query':query,'variables':variables})['data']
def save():receipt_path.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n')
repo=api('repos/'+REPO);assert repo['permissions']['admin']
labels=api('repos/'+REPO+'/labels?per_page=100',pages=True);known={v['name'] for v in labels}
label_defs={'blocked':('b60205','Dependencia técnica sin resolver; retirar al desbloquear.'),'needs-device-test':('fbca04','Requiere prueba física, no reemplazar por mocks.'),'needs-decision':('d876e3','Requiere decisión registrada antes de implementar.'),'security':('b60205','Seguridad de red/permisos y datos.'),'performance':('5319e7','Rendimiento o latencia medidos.'),'breaking-protocol':('e99695','Cambio incompatible de protocolo.'),'documentation':('0075ca','Trabajo o cambio documental.')}
for typ in ['Epic','Feature','Task','Bug','Spike','Documentation']:label_defs['type:'+typ.lower()]=('1d76db','Convención de tipo; usar si Issue Types no disponibles: '+typ)
for area in sorted({x['area'] for x in manifest['items']}):label_defs['area:'+area.lower().replace('/','-')]=('0e8a16','Área para búsqueda global: '+area)
for name,(color,description) in label_defs.items():
 if name not in known:api('repos/'+REPO+'/labels','POST',{'name':name,'color':color,'description':description})
receipt['managed_labels']=list(label_defs);save()
ms=api('repos/'+REPO+'/milestones?state=all&per_page=100',pages=True);milestones={v['title']:v['number'] for v in ms}
for epic in [x for x in manifest['items'] if x['type']=='Epic' and x['phase']!='Futuro']:
 name='Fase '+epic['phase']+' — '+epic['title']
 if name not in milestones:
  value=api('repos/'+REPO+'/milestones','POST',{'title':name,'description':'Hito de sección 50 v4; sin fecha nueva ni Sprint asignado. Aceptación según secciones 49/54.'});milestones[name]=value['number']
receipt['milestones']=milestones;save()
existing=api('repos/'+REPO+'/issues?state=all&per_page=100',pages=True);index={}
for item in existing:
 if 'pull_request' in item:continue
 m=re.search(r'<!-- gorilla-backlog:([^ ]+) -->',item.get('body') or '')
 if m:
  if m[1] in index:raise RuntimeError('Duplicate stable key: '+m[1])
  index[m[1]]=item
fields={f['name']:f for f in state['fields']}
project_items=[];cursor=None
while True:
 result=gql('query($id:ID!,$cursor:String){node(id:$id){... on ProjectV2{items(first:100,after:$cursor){nodes{id content{... on Issue{id}} fieldValues(first:20){nodes{... on ProjectV2ItemFieldSingleSelectValue{name field{... on ProjectV2SingleSelectField{name}}}}}} pageInfo{hasNextPage endCursor}}}}}',{'id':PID,'cursor':cursor})['node']['items'];project_items+=result['nodes']
 if not result['pageInfo']['hasNextPage']:break
 cursor=result['pageInfo']['endCursor']
item_index={x['content']['id']:x['id'] for x in project_items if x.get('content')}
value_index={x['content']['id']:{v['field']['name']:v.get('name') for v in x['fieldValues']['nodes'] if v.get('field')} for x in project_items if x.get('content')}
def issue_link(k):return '#'+str(receipt['issues'][k]['number']) if k in receipt['issues'] else k
for item in manifest['items']:
 k=item['key'];src='; '.join('v4 §'+r for r in item['sources'])
 dep='\n'.join('- Blocked by '+issue_link(d) for d in item['blocked_by']) or 'Ninguna dependencia bloqueante declarada; confirmar en Sprint Planning.'
 context=f"{src}. Fase: {item['phase']}. Alcance: {item['scope']}. Referencia: https://github.com/{REPO}/blob/main/docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md"
 scope='Implementar el resultado definido por los criterios, dentro de su fase; descomposición interna en Sprint Planning.'
 if item['phase']=='Futuro':scope='Catalogar/evaluar la opción respaldada por v4; NO autoriza desarrollar P1/P2 ni cambiar decisiones congeladas.'
 if item['type']=='Epic':scope='Agrupar features y su aceptación; no es una tarea de programación ni se estima sumando otra vez sus hijos.'
 if item['type']=='Feature':scope='Agrupar tareas de la capacidad; aceptar con evidencia integrada de todos sus hijos.'
 dod='Aplicar v4 §49: implementación, build, aceptación, pruebas aplicables, integración, PR revisado, recuperación, logging, tuning y documentación. Hardware real cuando aplique. Documentación/spikes: entregar evidencia y decisión revisadas; build sólo si existe código de prueba. Features/Epics: evidencia integrada de hijos, sin contar como esfuerzo duplicado.'
 body=f"<!-- gorilla-backlog:{k} -->\n\n## Objective\n{item['objective']}\n\n## Context\n{context}\n\n## Scope\n{scope}\n\n## Out of Scope\nOtras fases; cloud obligatorio, cuentas, reconocimiento facial y soltar físicamente el teléfono. No adoptar dependencias candidatas sin prueba/decisión.\n\n## Acceptance Criteria\n"+'\n'.join('- [ ] '+a for a in item['acceptance'])+f"\n\n## Dependencies\n{dep}\n\n## Technical Notes\nTipo: {item['type']}. Área: {item['area']}. Parent: {issue_link(item['parent']) if item['parent'] else 'ninguno'}. Responsabilidades primarias según WORKFLOW; sin Assignee hasta Sprint Planning. Versiones y layout propuestos no equivalen a aprobación.\n\n## Tests\n{item['tests']}\n\n## Evidence Required\nReporte reproducible con fecha, commit, versiones, hardware cuando aplique, resultado real y limitaciones. Actualizar TEST_REPORT/DEVELOPMENT_PROGRESS. No inventar PASS ni almacenar video/fotos de cámara.\n\n## Definition of Done\n{dod}\n"
 created=False
 if k in index:value=index[k]
 else:
  ls=['type:'+item['type'].lower(),'area:'+item['area'].lower().replace('/','-')]+item['labels']
  if item['blocked_by']:ls+=['blocked']
  if item['type']=='Documentation':ls+=['documentation']
  payload={'title':'['+k+'] '+item['title'],'body':body,'labels':sorted(set(ls))}
  if item['phase']!='Futuro':payload['milestone']=next(v for n,v in milestones.items() if n.startswith('Fase '+item['phase']+' —'))
  value=api('repos/'+REPO+'/issues','POST',payload);created=True;receipt['created_this_execution']+=1
 receipt['issues'][k]={'number':value['number'],'id':value['id'],'node_id':value['node_id'],'url':value['html_url'],'phase':item['phase'],'type':item['type']};save()
 node=value['node_id']
 if node not in item_index:
  try:
   added=gql('mutation($input:AddProjectV2ItemByIdInput!){addProjectV2ItemById(input:$input){item{id}}}',{'input':{'projectId':PID,'contentId':node}})['addProjectV2ItemById']['item']['id']
  except RuntimeError as error:
   if 'Content already exists' not in str(error):raise
   added=None
   for retry in range(6):
    live=gql('query($id:ID!){node(id:$id){... on ProjectV2{items(first:100){nodes{id content{... on Issue{id number}}}}}}}',{'id':PID})['node']['items']['nodes']
    added=next((x['id'] for x in live if x.get('content') and x['content'].get('number')==value['number']),None)
    if added:break
    time.sleep(1+retry)
   if not added:raise RuntimeError('Auto-add visibility delayed; rerun safely for '+k)
  item_index[node]=added
 # Set only missing metadata, preserving PO changes across reruns.
 updates=[]
 for field,val in [('Status','Backlog'),('Phase',item['phase']),('Area',item['area'])]+([('Target Release',item['target_release'])] if item['target_release'] else []):
  if value_index.get(node,{}).get(field):continue
  f=fields[field];opt=next(o['id'] for o in f['options'] if o['name']==val)
  updates.append({'projectId':PID,'itemId':item_index[node],'fieldId':f['id'],'value':{'singleSelectOptionId':opt}})
 if updates:
  declarations=','.join('$v'+str(i)+':UpdateProjectV2ItemFieldValueInput!' for i in range(len(updates)))
  operations=' '.join('u'+str(i)+':updateProjectV2ItemFieldValue(input:$v'+str(i)+'){projectV2Item{id}}' for i in range(len(updates)))
  gql('mutation('+declarations+'){'+operations+'}',{'v'+str(i):v for i,v in enumerate(updates)})
 receipt['issues'][k]['project_item_id']=item_index[node];save();print(('Created ' if created else 'Reused ')+k+' #'+str(value['number']),flush=True)
# Native relationships: read first, never reparent existing children silently.
for item in manifest['items']:
 k=item['key'];v=receipt['issues'][k]
 if item['parent']:
  parent=receipt['issues'][item['parent']]
  children=api(f"repos/{REPO}/issues/{parent['number']}/sub_issues?per_page=100",pages=True)
  if v['id'] not in {x['id'] for x in children}:api(f"repos/{REPO}/issues/{parent['number']}/sub_issues",'POST',{'sub_issue_id':v['id']})
  receipt['relationships'][k]={'parent':item['parent'],'blocked_by':[]};save()
 if item['blocked_by']:
  current=api(f"repos/{REPO}/issues/{v['number']}/dependencies/blocked_by?per_page=100",pages=True)
  for blocker in item['blocked_by']:
   b=receipt['issues'][blocker]
   if b['id'] not in {x['id'] for x in current}:api(f"repos/{REPO}/issues/{v['number']}/dependencies/blocked_by",'POST',{'issue_id':b['id']})
  receipt['relationships'].setdefault(k,{})['blocked_by']=item['blocked_by'];save()
receipt['counts_by_phase']=dict(Counter(x['phase'] for x in manifest['items']));receipt['counts_by_type']=dict(Counter(x['type'] for x in manifest['items']));save()
print('All issues and native relationships reconciled.',flush=True)
