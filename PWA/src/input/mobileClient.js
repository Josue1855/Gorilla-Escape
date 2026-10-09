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
// Metadata only: never retain candidate address, SDP, URL or credentials.
export function sanitizeCandidate(candidate){
 if(typeof candidate!=='string'||candidate.length>2048)return null;
 const f=candidate.trim().replace(/^a=/,'').split(/\s+/);if(f.length<8||!f[0].startsWith('candidate:')||f[6]!=='typ')return null;
 const protocol=f[2].toLowerCase(),type=f[7],port=Number(f[5]),address=f[4];
 if(!['udp','tcp'].includes(protocol)||!['host','srflx','prflx','relay'].includes(type)||!Number.isInteger(port)||port<1||port>65535)return null;
 const addressKind=/^[A-Za-z0-9-]{1,63}\.local$/.test(address)?'MDNS_LOCAL':/^[0-9.]+$/.test(address)?'IPV4':/^[0-9a-fA-F:]+$/.test(address)?'IPV6':'OTHER';
 return {type,protocol,addressKind,port};
}
export class MobileClient {
 constructor({source='synthetic',fetcher=fetch}={}){
  if(!['synthetic','replay','emulator','physical'].includes(source))throw Error('evidence source');
  this.source=source;this.fetcher=(...args)=>fetcher(...args);this.sequence=0;this.pending=new Map();this.latest=null;this.state='STOPPED';
  this.metrics={sent:0,received:0,errors:0,overwritten:0,motionSubmitted:0,motionSent:0,motionAck:0,motionErrors:0,rtt:[]};
 }
 diagnosticEvent(event,value){
  if(this.trace.events.length<96)this.trace.events.push({ms:Math.round((performance.now()-this.started)*100)/100,event,value});
 }
 stage(value){this.trace.stage=value;this.diagnosticEvent('stage',value);}
 diagnostic(){return structuredClone(this.trace);}
 async signal(path,body){
  const controller=new AbortController();const timer=setTimeout(()=>controller.abort(),35000);
  this.trace.postExecuted=true;
  try{
   const r=await this.fetcher(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body),signal:controller.signal});
   this.trace.httpStatus=r.status;this.stage('SIGNALING_RESPONSE');
   if(!r.ok){let reply;try{reply=await r.json();}catch{}
    const allowed=['LOBBY_ADMISSION_CLOSED','SESSION_FULL','SIGNAL_INVALID','ADMISSION_INVALID','ADMISSION_EXPIRED_OR_USED','REMOTE_SDP_FAILED','ANSWER_CREATE_FAILED','LOCAL_SDP_FAILED','SERVER_ICE_TIMEOUT','SIGNAL_FAILED_OTHER'];
    this.trace.rejectionCode=allowed.includes(reply?.code)?reply.code:'SIGNAL_FAILED_OTHER';throw Error('JOIN_E04_SIGNAL_REJECTED');
   }
   return await r.json();
  }finally{clearTimeout(timer);}
 }
 async connect(joinUrl,resume=null){
  this.started=performance.now();this.trace={stage:'START',code:null,postExecuted:false,httpStatus:null,rejectionCode:null,remoteDescription:'NOT RUN',candidates:[],events:[]};
  this.state='CONNECTING';let code='JOIN_E01_OFFER';let rejectIce;
  const iceFailed=new Promise((_,reject)=>{rejectIce=reject;});iceFailed.catch(()=>{});
  try{
   this.pc=new RTCPeerConnection({iceServers:[]});this.channels={};
   for(const key of ['signalingState','iceGatheringState','iceConnectionState','connectionState']){
    const observe=()=>{this.diagnosticEvent(key,this.pc[key]);if(key==='iceConnectionState'){
     if(['connected','completed'].includes(this.pc[key]))this.diagnosticEvent('stage','ICE_CONNECTED');
     if(this.pc[key]==='failed')rejectIce(Error('JOIN_E06_ICE_FAILED'));
    }};
    this.pc.addEventListener(key==='iceGatheringState'?'icegatheringstatechange':key==='iceConnectionState'?'iceconnectionstatechange':key==='signalingState'?'signalingstatechange':'connectionstatechange',observe);observe();
   }
   this.pc.addEventListener('icecandidate',e=>{if(e.candidate&&this.trace.candidates.length<32){const safe=sanitizeCandidate(e.candidate.candidate);if(safe)this.trace.candidates.push(safe);}});
   for(const [label,options] of [['control',{ordered:true}],['motion',{ordered:false,maxRetransmits:0}]]){
    const ch=this.pc.createDataChannel(label,options);this.channels[label]=ch;ch.onmessage=e=>this.receive(e.data);
    const observe=()=>this.diagnosticEvent(label,ch.readyState);observe();ch.addEventListener('open',observe);ch.addEventListener('close',observe);ch.addEventListener('error',observe);
    ch.onclose=()=>{if(this.state==='RUNNING')this.state='DISCONNECTED';};
   }
   this.stage('CREATE_OFFER');const offer=await this.pc.createOffer();
   code='JOIN_E02_LOCAL_SDP';this.stage('LOCAL_DESCRIPTION');await this.pc.setLocalDescription(offer);
   code='JOIN_E02_ICE_GATHER';this.stage('ICE_GATHERING');
   if(this.pc.iceGatheringState!=='complete')await deadline(new Promise(r=>{const done=()=>{if(this.pc.iceGatheringState==='complete'){this.pc.removeEventListener('icegatheringstatechange',done);r();}};this.pc.addEventListener('icegatheringstatechange',done);done();}),10000,code);
   const fragment=new URLSearchParams(new URL(joinUrl,location.href).hash.slice(1));
   code='JOIN_E03_SIGNAL_HTTP';this.stage('SIGNALING_POST');
   const reply=resume?await this.signal('/mobile/reconnect',{sessionId:resume.sessionId,playerId:resume.playerId,resumeToken:resume.resumeToken,offer:this.pc.localDescription.sdp}):await this.signal('/mobile/join',{protocolVersion:1,messageType:'JOIN',sessionId:fragment.get('sessionId'),admission:fragment.get('admission'),offer:this.pc.localDescription.sdp});
   this.identity=reply;this.sequence=0;
   code='JOIN_E05_REMOTE_SDP';this.stage('REMOTE_DESCRIPTION');await this.pc.setRemoteDescription({type:'answer',sdp:reply.answer});this.trace.remoteDescription='PASS';
   this.stage('ICE_CONNECTING');code='JOIN_E06_ICE_FAILED';
   await Promise.race([iceFailed,Promise.all(Object.entries(this.channels).map(([label,ch])=>{
    const failure=label==='control'?'JOIN_E07_CONTROL_TIMEOUT':'JOIN_E08_MOTION_TIMEOUT';
    const opened=ch.readyState==='open'?Promise.resolve():deadline(new Promise((r,j)=>{ch.onopen=r;ch.onerror=()=>j(Error(failure));}),10000,failure);
    return opened.then(()=>this.stage(label==='control'?'CONTROL_OPEN':'MOTION_OPEN'));
   }))]);
   code='JOIN_E09_HELLO_TIMEOUT';this.stage('HELLO_ACK');this.state='RUNNING';await this.send('HELLO',{});
   this.stage('READY');this.heartbeat=setInterval(()=>{this.send('HEARTBEAT',{}).catch(()=>{this.metrics.errors++;});},1000);
   return {playerId:reply.playerId,connectionEpoch:reply.connectionEpoch};
  }catch(e){
   const known=/^JOIN_E(?:01_OFFER|02_LOCAL_SDP|02_ICE_GATHER|03_SIGNAL_HTTP|04_SIGNAL_REJECTED|05_REMOTE_SDP|06_ICE_FAILED|07_CONTROL_TIMEOUT|08_MOTION_TIMEOUT|09_HELLO_TIMEOUT)$/;
   this.trace.code=known.test(e.message)?e.message:code;
   // Preserve the failed-stage report before cleanup states; no native exception text escapes.
   this.trace.channelStates=Object.fromEntries(Object.entries(this.channels||{}).map(([label,ch])=>[label,ch.readyState]));
   await this.close(false);this.state='FAILED';throw Error(this.trace.code);
  }
 }
 envelope(type,payload,clientTimestamp=Date.now()){const groups=type==='MOTION_SAMPLE'?Object.values(payload):[];const status=groups.length?(groups.every(g=>g.availability==='unavailable')?'unavailable':groups.every(g=>g.availability==='present')?'available':'degraded'):'available';return {protocolVersion:1,messageType:type,sessionId:this.identity.sessionId,playerId:this.identity.playerId,deviceSessionId:this.identity.deviceSessionId,sequence:++this.sequence,clientTimestamp,serverReceiveTimestamp:null,capabilities:['acceleration','accelerationIncludingGravity','rotationRate','orientation','screenOrientation','touch'],quality:{source:this.source,status},payload};}
 send(type,payload,clientTimestamp=Date.now()){
  if(this.state!=='RUNNING')return Promise.reject(Error('client not running'));
  const message=this.envelope(type,payload,clientTimestamp),channel=this.channels[type==='MOTION_SAMPLE'?'motion':'control'];
  const text=JSON.stringify(message);if(new TextEncoder().encode(text).length>2048||channel.bufferedAmount>8192||this.pending.size>=8)return Promise.reject(Error('bounded send'));
  this.metrics.sent++;
  return new Promise((resolve,reject)=>{const started=performance.now();const timer=setTimeout(()=>{this.pending.delete(message.sequence);reject(Error('ACK deadline'));},2000);this.pending.set(message.sequence,{resolve,reject,timer,started,type});try{channel.send(text);if(type==='MOTION_SAMPLE')this.metrics.motionSent++;}catch(error){clearTimeout(timer);this.pending.delete(message.sequence);reject(error);}});
 }
 receive(text){let n;try{n=JSON.parse(text);}catch{this.metrics.errors++;return;}
  if(n.messageType==='ERROR'){this.metrics.errors++;return;}
  const p=this.pending.get(n.sequence);if(!p)return;clearTimeout(p.timer);this.pending.delete(n.sequence);this.metrics.received++;if(p.type==='MOTION_SAMPLE')this.metrics.motionAck++;
  if(this.metrics.rtt.length<4096)this.metrics.rtt.push(performance.now()-p.started);p.resolve(n);
 }
 motion(payload,clientTimestamp=Date.now()){this.metrics.motionSubmitted++;if(this.latest)this.metrics.overwritten++;this.latest={payload,clientTimestamp};if(!this.inFlight)this.flush();}
 flush(){if(!this.latest||this.state!=='RUNNING')return;const sample=this.latest;this.latest=null;this.inFlight=true;this.send('MOTION_SAMPLE',sample.payload,sample.clientTimestamp).catch(()=>{this.metrics.errors++;this.metrics.motionErrors++;}).finally(()=>{this.inFlight=false;this.flush();});}
 async close(notify=true){
  clearInterval(this.heartbeat);this.latest=null;const identity=this.identity;this.state='STOPPING';
  for(const p of this.pending.values()){clearTimeout(p.timer);p.reject(Error('client closed'));}this.pending.clear();
  Object.values(this.channels||{}).forEach(ch=>ch.close());this.pc?.close();this.state='STOPPED';
  if(notify&&identity)await this.signal('/mobile/disconnect',{peerId:identity.peerId,resumeToken:identity.resumeToken});
 }
}
export function bindCapture(capture,client,{onEmit=()=>{},maximumHz=null}={}) {
 if(maximumHz!==null&&(!Number.isFinite(maximumHz)||maximumHz<=0))throw new RangeError('maximumHz');
 let last='',generation=null,nextDue=null,emitted=0,budgetDropped=0;
 const bind=snapshot=>{
  const m=snapshot.channels.motion,o=snapshot.channels.orientation;
  const motion=m.latest,orientation=o.latest;
  const ms=m.validSequence??motion?.sequence??0,os=o.validSequence??orientation?.sequence??0;
  const key=`${snapshot.generation??0}:${ms}:${os}`;
  if((ms===0&&os===0)||key===last||client.state!=='RUNNING')return;
  last=key;
  const currentGeneration=snapshot.generation??0;
  if(generation!==currentGeneration){generation=currentGeneration;nextDue=null;}
  const latest=(motion?.callbackMonotonicMs??-1)>=(orientation?.callbackMonotonicMs??-1)?motion:orientation;
  const at=latest?.callbackMonotonicMs;
  if(maximumHz!==null){
   // Event-driven deterministic discard after physical measurement. No timer or catch-up burst.
   if(!Number.isFinite(at)||(nextDue!==null&&at<nextDue)){budgetDropped++;return;}
   const step=1000/maximumHz;
   nextDue=nextDue===null?at+step:nextDue+step;
   if(nextDue<=at)nextDue=at+step;
  }
  client.motion(normalizeMotion(motion||{},orientation||{},latest?.screenAngleDegrees),latest?.receiptUtcMs??Date.now());
  emitted++;onEmit();
 };
 bind.metrics=()=>({maximumHz,emitted,budgetDropped,policy:'real-event discard; latest values at accepted event; no timer'});
 return bind;
}

export function attachCapture(capture,client,win=window){
 const normalize=bindCapture(capture,client);
 const listener=()=>normalize(capture.snapshot());
 win.addEventListener('devicemotion',listener);win.addEventListener('deviceorientation',listener);
 return ()=>{win.removeEventListener('devicemotion',listener);win.removeEventListener('deviceorientation',listener);};
}
