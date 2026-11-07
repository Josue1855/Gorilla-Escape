import test from 'node:test';
import assert from 'node:assert/strict';
import { gamePresentation, gameStats } from '../src/controller/gamePresentation.js';
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
  assert.equal(gameStats({...context,stats:{...stats,progress:NaN}}),null);
  assert.equal(gameStats({...context,stats:{...stats,balance:2}}),null);
  assert.equal(gameStats({...context,stats:{progress:1}}),null);
  assert.equal(gameStats(null),null);
});

test('v2 presents each real game and omits unavailable statistics',()=>{
  for(const gameId of ['bowling','boxing','baseball','drums','coconut-slice','whack','log-balance']){
    const c={version:2,gameId,phase:'Playing',playerId:1,stats:{hasScore:true,score:123,hasProgress:false,progress:0,hasElapsed:false,elapsed:0,hasBalance:false,balance:0,hasDistance:false,distance:0}};
    assert.equal(gamePresentation(c,1).game,gameId);assert.equal(gamePresentation(c,1).active,true);
    assert.equal(gamePresentation(c,2).active,false);assert.deepEqual(gameStats(c),{score:123});
    assert.equal(gameStats({...c,stats:{...c.stats,score:Infinity}}),null);
    assert.equal(gamePresentation({...c,phase:'unknown'},1).game,'lobby');
  }
});
