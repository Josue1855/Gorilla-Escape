import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { execFileSync } from 'node:child_process';
import vm from 'node:vm';
test('SW precaches only generated local resources and never handles session/API/diagnostic traffic',async()=>{
 const temp=await mkdtemp(tmpdir()+'/gorilla-sw-test-');try{
 await mkdir(temp+'/dist/assets',{recursive:true});for(const name of ['index.html','manifest.webmanifest','assets/main.js','assets/main.css','icon.svg'])await writeFile(temp+'/dist/'+name,'fixture '+name);
 execFileSync(process.execPath,[resolve('tools/build-service-worker.js')],{cwd:temp});const source=await readFile(temp+'/dist/sw.js','utf8');
 const handlers={},stored=[];const self={location:{origin:'https://local.invalid'},clients:{claim(){}},addEventListener:(name,fn)=>handlers[name]=fn};
 const caches={open:async name=>{assert.match(name,/^gorilla-shell-[a-f0-9]{16}$/);return {addAll:async urls=>stored.push(...urls),match:async()=>({local:true})};},keys:async()=>[],delete:async()=>true};
 vm.runInNewContext(source,{self,caches,URL,fetch:()=>{throw Error('unexpected network');}});let work;handlers.install({waitUntil:p=>work=p});await work;
 assert.deepEqual(stored.sort(),['/assets/main.css','/assets/main.js','/icon.svg','/index.html','/manifest.webmanifest']);
 for(const [method,path] of [['GET','/api/health'],['POST','/mobile/join'],['POST','/mobile/reconnect'],['POST','/mobile/admission'],['GET','/mobile/diagnostics'],['GET','/mobile/join'],['GET','https://external.invalid/index.html']]){
 let intercepted=false;handlers.fetch({request:{method,url:path.startsWith('https:')?path:'https://local.invalid'+path},respondWith:()=>intercepted=true});assert.equal(intercepted,false,path);
 }
 let response;handlers.fetch({request:{method:'GET',url:'https://local.invalid/'},respondWith:p=>response=p});assert.deepEqual(await response,{local:true});
 }finally{await rm(temp,{recursive:true,force:true});}
});
