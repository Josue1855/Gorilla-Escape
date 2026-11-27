// Executed inside a real secure-context browser. No sensor/gameplay/protocol integration.
export async function runProbe(config) {
  const enc=new TextEncoder(), dec=new TextDecoder();
  const bounded = (promise, ms=4000) => new Promise((resolve,reject) => {
    const timer=setTimeout(()=>reject(new Error('deadline')),ms);
    promise.then(v=>{clearTimeout(timer);resolve(v);},e=>{clearTimeout(timer);reject(e);});
  });
  const pin=Uint8Array.from(atob(config.hash),c=>c.charCodeAt(0));
  if(config.badPin) pin[0]^=1;
  const start=performance.now();
  const transport=new WebTransport(config.url,{allowPooling:false,serverCertificateHashes:[{algorithm:'sha-256',value:pin}]});
  transport.closed.catch(()=>{});
  let writer,reader,dwriter,dreader; let stage='CONNECTING';
  try {
    await bounded(transport.ready);
    stage='CONNECTED'; const connectMs=performance.now()-start;
    const stream=await bounded(transport.createBidirectionalStream());
    writer=stream.writable.getWriter(); reader=stream.readable.getReader();
    let buffer=new Uint8Array();
    const send=async text=>{
      const bytes=enc.encode(text), frame=new Uint8Array(bytes.length+2);
      new DataView(frame.buffer).setUint16(0,bytes.length);frame.set(bytes,2);await bounded(writer.write(frame));
    };
    const receive=async()=>{
      for(;;){
        if(buffer.length>=2){const n=new DataView(buffer.buffer,buffer.byteOffset,buffer.byteLength).getUint16(0);if(n>256)throw Error('invalid response');if(buffer.length>=n+2){const result=dec.decode(buffer.slice(2,n+2));buffer=buffer.slice(n+2);return result;}}
        const {value,done}=await bounded(reader.read());if(done)throw Error('closed');
        const joined=new Uint8Array(buffer.length+value.length);joined.set(buffer);joined.set(value,buffer.length);buffer=joined;
        if(buffer.length>4096)throw Error('bounded response');
      }
    };
    stage='AUTHENTICATING'; await send('AUTH:'+config.token);
    if(await receive()!=='READY')throw Error('not authorized');
    stage='AUTHORIZED'; if(config.oversize){stage='OVERSIZE';await send('X'.repeat(257));await receive();throw Error('oversize accepted');}
    stage='RELIABLE'; const rtts=[];
    for(let i=0;i<20;i++){const t=performance.now();await send('PING:'+i);if(await receive()!=='PING:'+i)throw Error('wrong echo');rtts.push(performance.now()-t);}
    await send('INVALID');const invalidRejected=await receive()==='INVALID';
    stage='DATAGRAM'; dwriter=transport.datagrams.writable.getWriter();dreader=transport.datagrams.readable.getReader();
    await bounded(dwriter.write(enc.encode('INVALID')));
    await bounded(dwriter.write(enc.encode('X'.repeat(257))));
    const datagramRtts=[];
    for(let i=0;i<20;i++){
      const t=performance.now();await bounded(dwriter.write(enc.encode('PING:'+i)));
      const {value,done}=await bounded(dreader.read());
      if(done||dec.decode(value)!=='PING:'+i)throw Error('wrong datagram');datagramRtts.push(performance.now()-t);
    }
    transport.close();await bounded(transport.closed);
    const stats=values=>{const s=[...values].sort((a,b)=>a-b);return {n:s.length,minMs:s[0],p50Ms:s[Math.ceil(s.length*.5)-1],p95Ms:s[Math.ceil(s.length*.95)-1],maxMs:s.at(-1)};};
    return {status:'PASS',connectMs,reliable:stats(rtts),datagrams:stats(datagramRtts),invalidRejected,disconnect:'PASS',sent:20,received:20,loss:0,errors:0};
  } catch(error) {return {status:'REJECTED',stage,elapsedMs:performance.now()-start,errorName:error.name,errorMessage:error.message,deadline:error.message==='deadline'};}
  finally {
    transport.close();
    await Promise.allSettled([reader?.cancel(),dreader?.cancel(),writer?.abort(),dwriter?.abort()].map(p=>bounded(Promise.resolve(p),2000)));
  }
}
