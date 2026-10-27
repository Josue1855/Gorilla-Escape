import test from 'node:test';
import assert from 'node:assert/strict';
import {textPages} from '../src/controller/paging.js';
test('diagnostic pages preserve every character and explicit newline without overflow',()=>{
 const text='123456789\n\nABCD\nEFGH';
 assert.deepEqual(textPages(text,4,2),['1234\n5678','9\n','ABCD\nEFGH']);
 for(const page of textPages(text,4,2)){
  const lines=page.split('\n');assert.ok(lines.length<=2);assert.ok(lines.every(l=>l.length<=4));
 }
});
test('very small viewports still offer one character per page',()=>{
 assert.deepEqual(textPages('ABC',0,0),['A','B','C']);assert.deepEqual(textPages('',20,5),['']);
});
