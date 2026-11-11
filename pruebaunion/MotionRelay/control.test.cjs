const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const html=fs.readFileSync(require('node:path').join(__dirname,'index.html'),'utf8');
const script=html.match(/<script>([\s\S]*?)<\/script>/)[1];
function environment(search, permission='granted', slow=false) {
 const elements={},events={},timers=[],posts=[],replies=[];let requests=0;
 function el(id){return elements[id]??=( {value:'',hidden:false,disabled:false,style:{},textContent:'',className:'',listeners:{},addEventListener(type,fn){this.listeners[type]=fn},focus(){this.focused=true}} );}
 const context={URLSearchParams,location:{search},document:{getElementById:el,querySelector:el},performance:{now:()=>100},Date,Math,JSON,Promise,Error,AbortController,clearTimeout(){},
 screen:{orientation:{angle:0}},navigator:{vibrate(){}},setTimeout:(fn)=>timers.push(fn),
 addEventListener:(name,fn)=>events[name]=fn,
 fetch:async(url,options)=>{posts.push({url,body:JSON.parse(options.body)});return slow ? new Promise(resolve=>replies.push(resolve)) : {ok:true}}};
 context.window=context;context.isSecureContext=true;
 context.DeviceMotionEvent=context.DeviceOrientationEvent={requestPermission:async()=>{requests++;return permission}};
 vm.runInNewContext(script,context);
 return {el,events,timers,posts,replies,get requests(){return requests}};
}
(async()=>{
 let e=environment('?room=aB23');
 assert.equal(e.el('room').value,'AB23');assert.equal(e.el('room').hidden,true);
 await e.el('activate').listeners.click();assert.equal(e.requests,2);assert.equal(e.el('controls').style.display,'grid');
 e.events.devicemotion({acceleration:{x:2},rotationRate:{alpha:3}});
 e.events.deviceorientation({alpha:350,beta:25,gamma:3});
 e.el('confirmButton').listeners.click();e.el('recenterButton').listeners.click();
 e.el('backButton').listeners.click();e.el('menuButton').listeners.click();
 await e.timers.shift()();
 assert.equal(e.posts[0].url,'/motion?room=AB23');assert.equal(e.posts[0].body.confirm,1);assert.equal(e.posts[0].body.recenter,1);assert.equal(e.posts[0].body.ax,2);assert.equal(e.posts[0].body.back,1);assert.equal(e.posts[0].body.menu,1);
 e=environment('');assert.equal(e.el('room').hidden,false);
 await e.el('activate').listeners.click();assert.equal(e.requests,0);assert.equal(e.el('room').focused,true);
 e=environment('?room=BAD<script>');assert.equal(e.el('room').hidden,false);
 e=environment('?room=ABCD','denied');await e.el('activate').listeners.click();assert.equal(e.el('activate').disabled,false);assert.equal(e.el('help').style.display,'block');
 e=environment('?room=ABCD','granted',true);
 await e.el('activate').listeners.click();
 e.events.deviceorientation({alpha:20});
 const first=e.timers[0]();
 e.events.deviceorientation({alpha:40});
 const second=e.timers[2]();
 assert.equal(e.posts.length,2);
 assert.equal(e.posts[1].body.alpha,40);
 assert.ok(e.posts[1].body.time>e.posts[0].body.time);
 e.replies[1]({ok:true}); e.replies[0]({ok:true}); await Promise.all([first,second]);
 console.log('PASS: QR room, fallback, permission gesture/denial, sensor payload, confirm and recenter.');
})().catch(e=>{console.error(e);process.exitCode=1});
