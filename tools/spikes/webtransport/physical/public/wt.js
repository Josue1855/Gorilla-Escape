const enc=new TextEncoder(),dec=new TextDecoder();
export function deadline(promise,ms=10000){return new Promise((resolve,reject)=>{const timer=setTimeout(()=>reject(Error('deadline')),ms);promise.then(x=>{clearTimeout(timer);resolve(x)},e=>{clearTimeout(timer);reject(e)});});}
export class Link {
  constructor(config){this.c=config;this.stage='CONNECTING';this.buffer=new Uint8Array();}
  async open(){
    const pin=Uint8Array.from(atob(this.c.hash),c=>c.charCodeAt(0));if(this.c.badPin)pin[0]^=1;
    this.started=performance.now();this.wt=new WebTransport(this.c.url,{allowPooling:false,serverCertificateHashes:[{algorithm:'sha-256',value:pin}]});this.wt.closed.catch(()=>{});
    await deadline(this.wt.ready,30000);this.connectMs=performance.now()-this.started;
    this.stage='AUTHENTICATING';const stream=await deadline(this.wt.createBidirectionalStream());this.writer=stream.writable.getWriter();this.reader=stream.readable.getReader();
    if(await this.exchange(`AUTH:${this.c.sessionId}:${this.c.token}`)!=='READY')throw Error('admission rejected');this.stage='RUNNING';return this;
  }
  async exchange(text){
    const data=enc.encode(text),frame=new Uint8Array(data.length+2);new DataView(frame.buffer).setUint16(0,data.length);frame.set(data,2);await deadline(this.writer.write(frame));
    for(;;){if(this.buffer.length>=2){const n=new DataView(this.buffer.buffer,this.buffer.byteOffset,this.buffer.length).getUint16(0);if(n<1||n>256)throw Error('invalid framing');if(this.buffer.length>=n+2){const s=dec.decode(this.buffer.slice(2,n+2));this.buffer=this.buffer.slice(n+2);return s;}}
      const x=await deadline(this.reader.read());if(x.done)throw Error('closed');const combined=new Uint8Array(this.buffer.length+x.value.length);combined.set(this.buffer);combined.set(x.value,this.buffer.length);if(combined.length>4096)throw Error('bounded response');this.buffer=combined;}
  }
  async close(){this.wt?.close();await Promise.allSettled([this.reader?.cancel(),this.writer?.abort()].map(x=>deadline(Promise.resolve(x),2000)));}
}
export async function probe(config,mode='positive'){
  const link=new Link(config),result={mode};
  try{
    await link.open();result.connectMs=link.connectMs;
    if(mode==='oversize'){link.stage='OVERSIZE';await link.exchange('X'.repeat(257));throw Error('oversize accepted');}
    link.stage='RELIABLE';result.reliable={sent:0,received:0,errors:0,rttMs:[]};
    for(let i=0;i<100;i++){const t=performance.now();result.reliable.sent++;const response=await link.exchange('PING:'+i);if(response!=='PING:'+i)throw Error('integrity/order');result.reliable.received++;result.reliable.rttMs.push(performance.now()-t);}
    result.malformedControlled=await link.exchange('INVALID')==='INVALID';
    link.stage='DATAGRAM';const writer=link.wt.datagrams.writable.getWriter(),reader=link.wt.datagrams.readable.getReader();
    const times=new Map(),seen=new Set();let last=-1;result.datagrams={sent:0,received:0,duplicates:0,outOfOrder:0,invalid:0,rttMs:[]};let reading=true;
    const read=(async()=>{while(reading){try{const x=await deadline(reader.read(),6000);if(x.done)break;const s=dec.decode(x.value);if(!/^PING:[0-9]{1,4}$/.test(s)){result.datagrams.invalid++;continue;}const n=Number(s.slice(5));if(!times.has(n)){result.datagrams.invalid++;continue;}if(seen.has(n)){result.datagrams.duplicates++;continue;}if(n<last)result.datagrams.outOfOrder++;last=n;seen.add(n);result.datagrams.received++;result.datagrams.rttMs.push(performance.now()-times.get(n));}catch{break;}}})();
    try{for(let i=0;i<100;i++){times.set(i,performance.now());await deadline(writer.write(enc.encode('PING:'+i)));result.datagrams.sent++;await new Promise(r=>setTimeout(r,20));}
      const end=performance.now()+2000;while(seen.size<100&&performance.now()<end)await new Promise(r=>setTimeout(r,20));
    }finally{reading=false;await reader.cancel();await writer.abort();await read;}
    result.datagrams.lost=100-seen.size;result.status='PASS';return result;
  }catch(e){return {...result,status:'REJECTED',stage:link.stage,errorName:e.name,errorMessage:e.message,deadline:e.message==='deadline'};}
  finally{await link.close();}
}
