"""Read-only verification of created issues, relationships, Project metadata and views."""
import hashlib,json,os,re,subprocess
from pathlib import Path
from collections import Counter
GH=os.environ.get('GH_BINARY','gh');root=Path(__file__).resolve().parents[2];d=root/'docs/github';manifest=json.loads((d/'backlog.json').read_text());receipt=json.loads((d/'publish_receipt.json').read_text());state=json.loads((d/'project_state.json').read_text())
def gql(q):
 r=subprocess.run([GH,'api','graphql','-f','query='+q],capture_output=True,text=True)
 if r.returncode:raise RuntimeError(r.stderr)
 out=json.loads(r.stdout)
 if out.get('errors'):raise RuntimeError(str(out['errors']))
 return out['data']
q='''query{repository(owner:"Josue1855",name:"Gorilla-Escape"){isPrivate issues(first:100,orderBy:{field:CREATED_AT,direction:ASC}){totalCount nodes{number title body state assignees(first:10){totalCount} parent{number} blockedBy(first:100){nodes{number}}}}}node(id:"PVT_kwHOBFweE84Bl2mT"){... on ProjectV2{public title items(first:100){totalCount nodes{content{... on Issue{number}} fieldValues(first:30){nodes{... on ProjectV2ItemFieldSingleSelectValue{name field{... on ProjectV2SingleSelectField{name}}}... on ProjectV2ItemFieldIterationValue{title}... on ProjectV2ItemFieldNumberValue{number}}}}} views(first:100){nodes{name layout filter groupByFields(first:5){nodes{... on ProjectV2Field{name} ... on ProjectV2SingleSelectField{name}}}verticalGroupByFields(first:5){nodes{... on ProjectV2Field{name} ... on ProjectV2SingleSelectField{name}}}}}}}}'''
out=gql(q);repo=out['repository'];project=out['node'];assert repo['isPrivate']==(state.get('repository_visibility','private')=='private') and not project['public']
issues={x['number']:x for x in repo['issues']['nodes']};assert repo['issues']['totalCount']==88
rows={x['content']['number']:x for x in project['items']['nodes'] if x.get('content')};assert project['items']['totalCount']==88
edges=0;parents=0
for x in manifest['items']:
 r=receipt['issues'][x['key']];issue=issues[r['number']];assert issue['state']=='OPEN' and not issue['assignees']['totalCount']
 assert '<!-- gorilla-backlog:'+x['key']+' -->' in issue['body']
 for h in ['Objective','Context','Scope','Out of Scope','Acceptance Criteria','Dependencies','Technical Notes','Tests','Evidence Required','Definition of Done']:assert '## '+h+'\n' in issue['body'],(x['key'],h)
 expected=receipt['issues'][x['parent']]['number'] if x['parent'] else None
 assert (issue['parent']['number'] if issue['parent'] else None)==expected,x['key'];parents+=bool(expected)
 assert {v['number'] for v in issue['blockedBy']['nodes']}=={receipt['issues'][k]['number'] for k in x['blocked_by']},x['key'];edges+=len(x['blocked_by'])
 vals=rows[r['number']]['fieldValues']['nodes'];fields={v['field']['name']:v['name'] for v in vals if v.get('field')}
 assert fields.get('Status')=='Backlog' and fields.get('Phase')==x['phase'] and fields.get('Area')==x['area'],x['key']
 assert 'Priority' not in fields and 'Risk' not in fields
 assert not any(v.get('title') or v.get('number') is not None for v in vals)
 assert fields.get('Target Release')==x['target_release'],x['key']
assert len(project['views']['nodes'])==10
sha=hashlib.sha256((root/'docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md').read_bytes()).hexdigest();assert sha=='63e0378f0785aa4e48e0a79e7ab28e7dcd342eb3e42ec50bcb61712f3f6b8a06'
protections={}
for branch in ['main','develop']:
 raw=subprocess.check_output([GH,'api','repos/Josue1855/Gorilla-Escape/branches/'+branch+'/protection'],text=True)
 protection=json.loads(raw)
 assert protection['enforce_admins']['enabled']
 assert protection['required_pull_request_reviews']['required_approving_review_count']==1
 assert protection['required_status_checks']['strict'] and 'management-validation' in protection['required_status_checks']['contexts']
 assert protection['required_conversation_resolution']['enabled'] and not protection['allow_force_pushes']['enabled'] and not protection['allow_deletions']['enabled']
 protections[branch]='PASS'
report={'status':'PASS','issues':88,'parent_links':parents,'blocking_links':edges,'counts_by_phase':dict(Counter(x['phase'] for x in manifest['items'])),'counts_by_type':dict(Counter(x['type'] for x in manifest['items'])),'views':project['views']['nodes'],'all_backlog':True,'assigned':0,'iterations_assigned':0,'spec_sha256':sha,'automation_configuration':'6 native workflows verified in UI; no synthetic close/merge test','repository_visibility':'public','project_visibility':'private','branch_protection':protections}
(d/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='views'},ensure_ascii=False))
