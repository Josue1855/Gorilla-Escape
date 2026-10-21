import test from 'node:test';
import assert from 'node:assert/strict';
import { gamePresentation } from '../src/controller/gamePresentation.js';
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
