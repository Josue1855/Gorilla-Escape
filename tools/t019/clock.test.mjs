import test from 'node:test';
import assert from 'node:assert/strict';
import {bracket,summarize} from './clock.mjs';
test('asymmetric transit interval contains independently known server offset',()=>{
 for(const forward of [0,1,20,100])for(const back of [0,4,40]){
  const offset=37,sendUtc=1000,receiveUtc=sendUtc+forward+back;
  const r=bracket({sendUtc,receiveUtc,sendMono:0,receiveMono:forward+back,serverUtc:sendUtc+forward+offset});
  assert.ok(r.lowerMs<=offset&&offset<=r.upperMs);
 }
});
test('clock jump and reverse monotonic timestamps are rejected',()=>{
 assert.throws(()=>bracket({sendUtc:1000,receiveUtc:1500,sendMono:0,receiveMono:5,serverUtc:1030}),/DISCONTINUITY/);
 assert.throws(()=>bracket({sendUtc:1,receiveUtc:2,sendMono:2,receiveMono:1,serverUtc:2}),/DISCONTINUITY/);
});
test('nonfinite samples cannot enter aggregate',()=>assert.throws(()=>bracket({sendUtc:NaN,receiveUtc:2,sendMono:0,receiveMono:1,serverUtc:2}),/NONFINITE/));
test('best bracket and drift uncertainty remain descriptive, with known drift enclosed',()=>{
 const samples=[bracket({sendUtc:1000,receiveUtc:1010,sendMono:0,receiveMono:10,serverUtc:1042}),bracket({sendUtc:2000,receiveUtc:2002,sendMono:1000,receiveMono:1002,serverUtc:2038})];
 const r=summarize(samples);assert.equal(r.N,2);assert.equal(r.offsetUtcServerMinusClientMs.uncertainty,3);
 assert.ok(r.observedDrift.deltaOffsetLowerMs<=0&&r.observedDrift.deltaOffsetUpperMs>=0);
});
test('empty and oversized collections fail instead of fake zero metrics',()=>{assert.throws(()=>summarize([]));assert.throws(()=>summarize(Array(4097).fill({})));});
