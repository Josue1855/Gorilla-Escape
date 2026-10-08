// Common client adapter: acquisition stays in SensorCapture, normalization here, RTC separately.
export const unavailable=()=>({availability:'unavailable',values:null});
export function normalizeGroup(values,axes){
  const normalized=Object.fromEntries(axes.map(axis=>[axis,typeof values?.[axis]==='boolean'&&axis==='pressed'?values[axis]:Number.isFinite(values?.[axis])?values[axis]:null]));
  const count=Object.values(normalized).filter(v=>v!==null).length;
  return count===0?unavailable():{availability:count===axes.length?'present':'partial',values:normalized};
}
export function normalizeMotion(motion={},orientation={},screenAngle=null,touch=null){return {
 acceleration:normalizeGroup(motion.acceleration,['x','y','z']),
 accelerationIncludingGravity:normalizeGroup(motion.accelerationIncludingGravity,['x','y','z']),
 rotationRate:normalizeGroup(motion.rotationRate,['alpha','beta','gamma']),
 orientation:normalizeGroup(orientation.angles,['alpha','beta','gamma']),
 screenOrientation:normalizeGroup({angle:screenAngle},['angle']),
 touch:normalizeGroup(touch,['x','y','pressed'])
};}
const deadline=(p,ms,label)=>{let t;return Promise.race([p,new Promise((_,reject)=>{t=setTimeout(()=>reject(Error(label)),ms)})]).finally(()=>clearTimeout(t));};
export class MobileClient {
 constructor({source='synthetic',fetcher=fetch}={}){
  if(!['synthetic','replay','emulator','physical'].includes(source))throw Error('evidence source');
  this.source=source;this.fetcher=(...args)=>fetcher(...args);this.sequence=0;this.pending=new Map();this.latest=null;this.state='STOPPED';
  this.metrics={sent:0,received:0,errors:0,overwritten:0,rtt:[]};
 }
 async signal(path,body){const controller=new AbortController();const timer=setTimeout(()=>controller.abort(),35000);try{const r=await this.fetcher(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body),signal:controller.signal});if(!r.ok)throw Error('signaling rejected');return await r.json();}finally{clearTimeout(timer);}}
 async connect(joinUrl,resume=null){
  this.state='CONNECTING';this.pc=new RTCPeerConnection({iceServers:[]});this.channels={};
  for(const [label,options] of [['control',{ordered:true}],['motion',{ordered:false,maxRetransmits:0}]]){
   const ch=this.pc.createDataChannel(label,options);this.channels[label]=ch;ch.onmessage=e=>this.receive(e.data);ch.onclose=()=>{if(this.state==='RUNNING')this.state='DISCONNECTED';};
  }
  try{
   await this.pc.setLocalDescription(await this.pc.createOffer());
   if(this.pc.iceGatheringState!=='complete')await deadline(new Promise(r=>{this.pc.onicegatheringstatechange=()=>{if(this.pc.iceGatheringState==='complete')r();}}),10000,'ICE gathering deadline');
   const fragment=new URLSearchParams(new URL(joinUrl,location.href).hash.slice(1));
   const reply=resume?await this.signal('/mobile/reconnect',{sessionId:resume.sessionId,playerId:resume.playerId,resumeToken:resume.resumeToken,offer:this.pc.localDescription.sdp}):await this.signal('/mobile/join',{protocolVersion:1,messageType:'JOIN',sessionId:fragment.get('sessionId'),admission:fragment.get('admission'),offer:this.pc.localDescription.sdp});
   this.identity=reply;this.sequence=0;
   await this.pc.setRemoteDescription({type:'answer',sdp:reply.answer});
   await Promise.all(Object.values(this.channels).map(ch=>ch.readyState==='open'?Promise.resolve():deadline(new Promise((r,j)=>{ch.onopen=r;ch.onerror=()=>j(Error('channel failed'));}),10000,'channel deadline')));
   this.state='RUNNING';await this.send('HELLO',{});
   this.heartbeat=setInterval(()=>{this.send('HEARTBEAT',{}).catch(()=>{this.metrics.errors++;});},1000);
   return {playerId:reply.playerId,connectionEpoch:reply.connectionEpoch};
  }catch(e){await this.close(false);this.state='FAILED';throw e;}
 }
 envelope(type,payload,clientTimestamp=Date.now()){const groups=type==='MOTION_SAMPLE'?Object.values(payload):[];const status=groups.length?(groups.every(g=>g.availability==='unavailable')?'unavailable':groups.every(g=>g.availability==='present')?'available':'degraded'):'available';return {protocolVersion:1,messageType:type,sessionId:this.identity.sessionId,playerId:this.identity.playerId,deviceSessionId:this.identity.deviceSessionId,sequence:++this.sequence,clientTimestamp,serverReceiveTimestamp:null,capabilities:['acceleration','accelerationIncludingGravity','rotationRate','orientation','screenOrientation','touch'],quality:{source:this.source,status},payload};}
 send(type,payload,clientTimestamp=Date.now()){
  if(this.state!=='RUNNING')return Promise.reject(Error('client not running'));
  const message=this.envelope(type,payload,clientTimestamp),channel=this.channels[type==='MOTION_SAMPLE'?'motion':'control'];
  const text=JSON.stringify(message);if(new TextEncoder().encode(text).length>2048||channel.bufferedAmount>8192||this.pending.size>=8)return Promise.reject(Error('bounded send'));
  this.metrics.sent++;
  return new Promise((resolve,reject)=>{const started=performance.now();const timer=setTimeout(()=>{this.pending.delete(message.sequence);reject(Error('ACK deadline'));},2000);this.pending.set(message.sequence,{resolve,reject,timer,started});try{channel.send(text);}catch(error){clearTimeout(timer);this.pending.delete(message.sequence);reject(error);}});
 }
 receive(text){let n;try{n=JSON.parse(text);}catch{this.metrics.errors++;return;}
  if(n.messageType==='ERROR'){this.metrics.errors++;return;}
  const p=this.pending.get(n.sequence);if(!p)return;clearTimeout(p.timer);this.pending.delete(n.sequence);this.metrics.received++;
  if(this.metrics.rtt.length<4096)this.metrics.rtt.push(performance.now()-p.started);p.resolve(n);
 }
 motion(payload,clientTimestamp=Date.now()){if(this.latest)this.metrics.overwritten++;this.latest={payload,clientTimestamp};if(!this.inFlight)this.flush();}
 flush(){if(!this.latest||this.state!=='RUNNING')return;const sample=this.latest;this.latest=null;this.inFlight=true;this.send('MOTION_SAMPLE',sample.payload,sample.clientTimestamp).catch(()=>{this.metrics.errors++;}).finally(()=>{this.inFlight=false;this.flush();});}
 async close(notify=true){
  clearInterval(this.heartbeat);this.latest=null;const identity=this.identity;this.state='STOPPING';
  for(const p of this.pending.values()){clearTimeout(p.timer);p.reject(Error('client closed'));}this.pending.clear();
  Object.values(this.channels||{}).forEach(ch=>ch.close());this.pc?.close();this.state='STOPPED';
  if(notify&&identity)await this.signal('/mobile/disconnect',{peerId:identity.peerId,resumeToken:identity.resumeToken});
 }
}
export function bindCapture(capture,client){let last='';return snapshot=>{const motion=snapshot.channels.motion.latest,orientation=snapshot.channels.orientation.latest;const key=`${motion?.sequence??-1}:${orientation?.sequence??-1}`;if((!motion&&!orientation)||key===last||client.state!=='RUNNING')return;last=key;client.motion(normalizeMotion(motion||{},orientation||{},motion?.screenAngleDegrees??orientation?.screenAngleDegrees));};}

export function attachCapture(capture,client,win=window){
 const normalize=bindCapture(capture,client);
 const listener=()=>normalize(capture.snapshot());
 win.addEventListener('devicemotion',listener);win.addEventListener('deviceorientation',listener);
 return ()=>{win.removeEventListener('devicemotion',listener);win.removeEventListener('deviceorientation',listener);};
}
