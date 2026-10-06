"""Prepare an existing Project without resetting item values or creating duplicates."""
import json, os, subprocess
from pathlib import Path
GH=os.environ.get('GH_BINARY','gh')
OWNER='Josue1855'; REPO=OWNER+'/Gorilla-Escape'; NUMBER=1
STATE=Path('docs/github/project_state.json')
def gh(*args,payload=None):
 cmd=[GH,*args]
 if payload is not None:cmd+=['--input','-']
 r=subprocess.run(cmd,input=json.dumps(payload) if payload is not None else None,capture_output=True,text=True)
 if r.returncode:raise RuntimeError(r.stderr.strip() or r.stdout[:300])
 out=json.loads(r.stdout) if r.stdout.strip() else {}
 if isinstance(out,dict) and out.get('errors'):raise RuntimeError(str(out['errors']))
 return out
def gql(query,variables):return gh('api','graphql',payload={'query':query,'variables':variables})['data']
p=gh('project','view',str(NUMBER),'--owner',OWNER,'--format','json'); pid=p['id']
repo=gh('api','repos/'+REPO);assert repo['permissions']['admin'] and repo['visibility']=='public'
gql('mutation($input:LinkProjectV2ToRepositoryInput!){linkProjectV2ToRepository(input:$input){repository{id}}}',{'input':{'projectId':pid,'repositoryId':repo['node_id']}})
gql('mutation($input:UpdateProjectV2Input!){updateProjectV2(input:$input){projectV2{id}}}',{'input':{'projectId':pid,'shortDescription':'Product Owner: Josue. Backlog por fases, sin gameplay implementado.','readme':'Fuente de verdad: especificación maestra v4. Todo nuevo trabajo comienza en Backlog. Josue prioriza y acepta producto; Sprint Planning define asignaciones y estimaciones. main → develop → ramas de tarea. No cerrar P0 sin evidencia y aceptación.','public':False}})
fields=gh('project','field-list',str(NUMBER),'--owner',OWNER,'--format','json')['fields'];by={f['name']:f for f in fields}
options={'Status':['Backlog','Ready','In Progress','In Review','Validation','PO Review','Done'],'Priority':['Critical','High','Medium','Low'],'Phase':['0','1','2','3','4','5','6','Futuro'],'Area':['Mobile/PWA','Network','Server','Protocol','Unity/Core','Gameplay','Camera/Tracking','UI/Art','CI/QA','Documentation'],'Risk':['Critical','High','Medium','Low'],'Target Release':['v1.0 — MVP']}
for name,names in options.items():
 opts=[{'name':n,'description':n,'color':'GRAY'} for n in names]
 if name in by:
  if [v['name'] for v in by[name].get('options',[])]!=names:
   # Stop if populated field: avoid changing IDs and destroying Sprint metadata.
   if p.get('items',{}).get('totalCount',0):raise RuntimeError('Existing populated field requires manual reconciliation: '+name)
   gql('mutation($input:UpdateProjectV2FieldInput!){updateProjectV2Field(input:$input){projectV2Field{... on ProjectV2SingleSelectField{id}}}}',{'input':{'fieldId':by[name]['id'],'singleSelectOptions':opts}})
 else:
  gql('mutation($input:CreateProjectV2FieldInput!){createProjectV2Field(input:$input){projectV2Field{... on ProjectV2SingleSelectField{id}}}}',{'input':{'projectId':pid,'name':name,'dataType':'SINGLE_SELECT','singleSelectOptions':opts}})
if 'Story Points' not in by:gh('project','field-create',str(NUMBER),'--owner',OWNER,'--name','Story Points','--data-type','NUMBER','--format','json')
if 'Iteration' not in by:
 gql('mutation($input:CreateProjectV2FieldInput!){createProjectV2Field(input:$input){projectV2Field{... on ProjectV2IterationField{id}}}}',{'input':{'projectId':pid,'name':'Iteration','dataType':'ITERATION','iterationConfiguration':{'startDate':'2026-10-05','duration':7,'iterations':[{'startDate':'2026-10-05','duration':7,'title':'Sprint 1 — planificación pendiente'}]}}})
fields=gh('project','field-list',str(NUMBER),'--owner',OWNER,'--format','json')['fields']
previous=json.loads(STATE.read_text()) if STATE.exists() else {}
STATE.write_text(json.dumps({**previous,'repository_visibility':'public','owner':OWNER,'repository':REPO,'project_id':pid,'project_number':NUMBER,'project_url':p['url'],'fields':fields},ensure_ascii=False,indent=2)+'\n')
print('Project linked; 8 requested fields configured; one weekly iteration; no Sprint assigned.')
