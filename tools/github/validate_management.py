"""Offline checks for documentation and the management manifest (standard library only)."""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def validate():
 data=json.loads((ROOT/'docs/github/backlog.json').read_text());items=data['items'];by={x['key']:x for x in items}
 assert len(by)==len(items),'Duplicate backlog keys'
 for x in items:
  assert x['type'] in {'Epic','Feature','Task','Bug','Spike','Documentation'}
  assert x['phase'] in {'0','1','2','3','4','5','6','Futuro'}
  assert x['acceptance'] and all(a.strip() for a in x['acceptance'])
  assert x['sources'] and x['objective'] and x['tests']
  assert x['status']=='Backlog' and not x['assignees'] and x['iteration'] is None
  assert x['priority'] is None and x['story_points'] is None
  if x['parent']:assert x['parent'] in by and by[x['parent']]['type'] in {'Epic','Feature'}
  for d in x['blocked_by']:assert d in by and d!=x['key']
 seen=set();active=set()
 def visit(k):
  assert k not in active,'Dependency cycle: '+k
  if k in seen:return
  active.add(k)
  for d in by[k]['blocked_by']:visit(d)
  active.remove(k);seen.add(k)
 for k in by:visit(k)
 links=0
 for p in ROOT.rglob('*.md'):
  if set(p.relative_to(ROOT).parts) & {'.git','node_modules','target','dist','Library','Temp','Logs'}:continue
  text=p.read_text();assert text.strip(),str(p)
  # Strip fenced examples, which may contain placeholders intentionally.
  assert len(re.findall(r'^```',text,re.M))%2==0,str(p)
  clean=re.sub(r'```.*?```','',text,flags=re.S)
  for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)',clean):
   if re.match(r'[a-z]+://',target) or target.startswith('#'):continue
   target=target.strip('<>').split('#')[0]
   assert (p.parent/target).exists(),str(p)+': '+target
   links+=1
 print(f'PASS: {len(items)} backlog records, acyclic dependencies, valid hierarchy, {links} local links; no fake assignments/Sprints.')
if __name__=='__main__':validate()
