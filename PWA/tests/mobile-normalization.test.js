import test from 'node:test';import assert from 'node:assert/strict';
import {normalizeMotion,normalizeGroup,MobileClient} from '../src/input/mobileClient.js';
test('partial is not zero; unavailable explicit',()=>{const m=normalizeMotion({acceleration:{x:1,y:null,z:NaN}});assert.equal(m.acceleration.availability,'partial');assert.equal(m.acceleration.values.y,null);assert.equal(m.rotationRate.availability,'unavailable');assert.equal(m.rotationRate.values,null);});
test('full fields and touch false preserved',()=>{assert.equal(normalizeGroup({x:1,y:2,z:3},['x','y','z']).availability,'present');assert.equal(normalizeMotion({}, {},0,{x:0,y:0,pressed:false}).touch.values.pressed,false);});
test('source provenance required',()=>assert.throws(()=>new MobileClient({source:'hardware-guaranteed',fetcher:()=>{}})));

test('orientation-only capture and independent orientation updates are forwarded',async()=>{
 const {bindCapture}=await import('../src/input/mobileClient.js');const values=[];const client={state:'RUNNING',motion:p=>values.push(p)};const listener=bindCapture({},client);
 const snapshot={channels:{motion:{latest:null},orientation:{latest:{sequence:1,angles:{alpha:1,beta:2,gamma:3}}}}};listener(snapshot);listener(snapshot);snapshot.channels.orientation.latest.sequence=2;listener(snapshot);
 assert.equal(values.length,2);assert.equal(values[0].acceleration.availability,'unavailable');assert.equal(values[0].orientation.availability,'present');
});
