import test from 'node:test';
import assert from 'node:assert/strict';
import { gamePresentation, gameStats, BalanceCues } from '../src/controller/gamePresentation.js';
test('unknown, absent or malformed context remains in lobby', () => {
  for (const c of [null, {}, {version:1,gameId:'other'}, {version:1,gameId:'log-balance',phase:'Crossing',playerId:99}]) {
    assert.equal(gamePresentation(c,1).game,'lobby');
  }
});
test('Unity phase controls instructions; other players see their turn accurately', () => {
  const c={version:1,gameId:'log-balance',phase:'Crossing',playerId:2};
  assert.equal(gamePresentation(c,2).active,true);
  assert.equal(gamePresentation(c,1).active,false);
  assert.match(gamePresentation(c,1).instruction,/Otro náufrago/);
  assert.match(gamePresentation({...c,phase:'Won'},2).heading,/superada/);
  assert.equal(gamePresentation({...c,phase:'Paused'},2).active,false);
});

test('mobile score uses bounded authoritative stats and never invents missing values', () => {
  const stats={balance:.2,tendency:.3,progress:1,distance:18,elapsed:22.7,roll:10,speed:.88,wind:.2};
  const context={version:1,gameId:'log-balance',phase:'Won',playerId:1,stats};
  assert.equal(gameStats(context),stats);
  assert.equal(gameStats({...context,stats:{...stats,danger:2}}),null);
  assert.equal(gameStats({...context,stats:{...stats,progress:NaN}}),null);
  assert.equal(gameStats({...context,stats:{...stats,balance:2}}),null);
  assert.equal(gameStats({...context,stats:{progress:1}}),null);
  assert.equal(gameStats(null),null);
});

test('danger cues have recovery time, rate limits and no spectator or paused vibration',()=>{
  const cues=new BalanceCues();
  assert.equal(cues.update('Crossing',.59,0,true),null);
  assert.equal(cues.update('Crossing',.65,10,true),'warning');
  assert.equal(cues.update('Crossing',.65,200,true),null);
  assert.equal(cues.update('Crossing',.9,470,true),'critical');
  assert.equal(cues.update('Paused',1,1200,true),null);
  assert.equal(cues.update('Crossing',1,1500,false),null);
  assert.equal(cues.update('Fell',1,1600,true),'Fell');
  assert.equal(cues.update('Fell',1,1800,true),null);
});
