"""Create/reuse Project views and apply supported GraphQL filters."""
import json,os,subprocess
from pathlib import Path
GH=os.environ.get('GH_BINARY','gh');path=Path('docs/github/project_state.json');state=json.loads(path.read_text());pid=state['project_id']; field_ids=[f['id'] for f in state['fields'] if f['name'] in ['Title','Assignees','Status','Priority','Iteration','Phase','Area','Story Points','Risk','Target Release','Milestone','Parent issue','Linked pull requests']]
def gql(query,variables):
 r=subprocess.run([GH,'api','graphql','--input','-'],input=json.dumps({'query':query,'variables':variables}),capture_output=True,text=True)
 if r.returncode:raise RuntimeError(r.stderr)
 out=json.loads(r.stdout)
 if out.get('errors'):raise RuntimeError(str(out['errors']))
 return out['data']
views=gql('query($id:ID!){node(id:$id){... on ProjectV2{views(first:100){nodes{id name layout filter}}}}}',{'id':pid})['node']['views']['nodes'];existing={x['name']:x for x in views}
plan=[('Product Backlog','TABLE_LAYOUT','is:issue'),('Current Sprint','TABLE_LAYOUT','is:issue iteration:@current'),('Sprint Board','BOARD_LAYOUT','is:issue iteration:@current'),('My Work','TABLE_LAYOUT','is:issue assignee:@me'),('PO Review','TABLE_LAYOUT','is:issue status:"PO Review"'),('Blocked','TABLE_LAYOUT','is:issue label:blocked'),('Bugs','TABLE_LAYOUT','is:issue label:"type:bug"'),('By Area','TABLE_LAYOUT','is:issue'),('Roadmap','ROADMAP_LAYOUT','is:issue label:"type:epic"'),('Releases','TABLE_LAYOUT','is:issue -phase:Futuro')]
receipt=[]
for name,layout,filter_ in plan:
 if name in existing:v=existing[name]
 elif name=='Product Backlog' and 'View 1' in existing:v=existing['View 1']
 else:v=gql('mutation($input:CreateProjectV2ViewInput!){createProjectV2View(input:$input){projectV2View{id name}}}',{'input':{'projectId':pid,'name':name,'layout':layout}})['createProjectV2View']['projectV2View']
 gql('mutation($input:UpdateProjectV2ViewInput!){updateProjectV2View(input:$input){projectV2View{id name layout filter}}}',{'input':{'viewId':v['id'],'name':name,'layout':layout,'filter':filter_,**({'configuration':{'visibleFieldIds':field_ids}} if layout!='ROADMAP_LAYOUT' else {})}})
 receipt.append({'name':name,'id':v['id'],'layout':layout,'filter':filter_})
state['views']=receipt;path.write_text(json.dumps(state,ensure_ascii=False,indent=2)+'\n');print('10 named views created/reused with saved filters; grouping and roadmap dates remain UI review.')
