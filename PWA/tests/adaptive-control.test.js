import test from 'node:test';
import assert from 'node:assert/strict';
import {ControllerSession} from '../src/controller/session.js';
import {controlProfile,gamePresentation,gameStats} from '../src/controller/gamePresentation.js';

const stats={hasScore:true,score:10,hasProgress:true,progress:.4,hasElapsed:false,elapsed:0,hasBalance:true,balance:-.2,hasDistance:false,distance:0};
const context=(gameId='lobby')=>({version:3,gameId,phase:'Ready',playerId:gameId==='lobby'?0:1,screen:gameId==='lobby'?'menu':'game',phoneRequired:!['lobby','coconut-slice','jungle-keeper'].includes(gameId),contextId:'a'.repeat(32),hand:'left',stats});
function fixture(){
  const doc=new EventTarget();doc.visibilityState='visible';const win=new EventTarget();
  const saved=new Map(),timers=new Map();let id=0;
  Object.assign(win,{document:doc,isSecureContext:true,setInterval:()=>++id,clearInterval(){},setTimeout:fn=>{timers.set(++id,fn);return id;},clearTimeout:i=>timers.delete(i),sessionStorage:{getItem:key=>saved.get(key),setItem:(key,value)=>saved.set(key,value)}});
  const identity={sessionId:'fixture',playerId:1,deviceSessionId:'device',resumeToken:'r'.repeat(43)};
  const client={identity,state:'RUNNING',gameContext:context(),gameContextExpires:performance.now()+10000,metrics:{},async close(){this.state='STOPPED';},async send(){return {};},async connect(url,resume){this.resume=resume;this.state='RUNNING';this.onIdentity?.(this.identity);},diagnostic(){return {};}};
  const model=new ControllerSession({win,doc,client,url:'https://example.test/#sessionId=fixture&admission=one-use'});
  model.network='NETWORK_READY';model.attempted=true;
  return {model,client,win,doc,saved,timers};
}
test('menu and eight game profiles distinguish sensors, dominant hand and spectator navigation',()=>{
  for(const id of ['lobby','bowling','boxing','baseball','drums','coconut-slice','jungle-keeper','whack','log-balance']){
    const c=context(id),profile=controlProfile(c,1);
    assert.equal(profile.valid,true);assert.equal(profile.hand,'left');assert.equal(profile.canNavigate,true);
    assert.equal(controlProfile(c,2).canNavigate,false);
    assert.equal(profile.phoneRequired,!['lobby','coconut-slice','jungle-keeper'].includes(id));
    assert.equal(gamePresentation(c,1).game,id);
  }
  assert.equal(controlProfile({...context(),contextId:'wrong'},1).canNavigate,false);
  assert.match(gamePresentation(context('bowling'),1).instruction,/izquierda/);
  assert.deepEqual(gameStats(context('log-balance')),{score:10,progress:.4,balance:-.2});
});
test('camera-only scene stops sensors and releases wake intent without dropping the peer',async()=>{
  const f=fixture();f.client.gameContext=context('coconut-slice');f.model.capture.state='RUNNING';f.model.actionPressed=true;f.model.wakeWanted=true;
  f.model.syncProfile();assert.equal(f.model.capture.state,'STOPPED');assert.equal(f.model.actionPressed,false);assert.equal(f.model.wakeWanted,false);assert.equal(f.client.state,'RUNNING');await f.model.dispose();
});
test('foreground reconnect uses the resume credential and never replays a touch action',async()=>{
  const f=fixture();f.client.state='DISCONNECTED';f.model.actionPressed=true;f.client.latest={old:true};
  await Promise.all([f.model.reconnect(),f.model.reconnect()]);
  assert.equal(f.client.resume.resumeToken,'r'.repeat(43));assert.equal(f.model.network,'NETWORK_READY');assert.equal(f.model.actionPressed,false);assert.equal(f.client.latest,null);
  assert.ok([...f.saved.values()].every(value=>!value.includes('one-use')));
  assert.equal(JSON.stringify(f.model.snapshot()).includes('resumeToken'),false);await f.model.dispose();
});
test('commands use the active scene token and cannot be sent by a spectator or while disconnected',async()=>{
  const f=fixture();const sent=[];f.client.send=async(type,payload)=>sent.push({type,payload});
  assert.equal(await f.model.command('select-game','bowling'),true);assert.equal(sent[0].type,'UI_COMMAND');assert.equal(sent[0].payload.contextId,'a'.repeat(32));
  f.client.identity={...f.client.identity,playerId:2};assert.equal(await f.model.command('menu'),false);
  f.client.identity.playerId=1;f.model.network='DISCONNECTED';assert.equal(await f.model.command('menu'),false);assert.equal(sent.length,1);await f.model.dispose();
});
