// QA-only offset interval from a client send/receive bracket and server ACK receive UTC.
// No symmetric-path assumption. UTC server-client; integer timestamp quantization is widened.
export function bracket({sendUtc,receiveUtc,sendMono,receiveMono,serverUtc}) {
  if (![sendUtc,receiveUtc,sendMono,receiveMono,serverUtc].every(Number.isFinite)) throw Error('NONFINITE_CLOCK');
  const rtt=receiveMono-sendMono;
  if (rtt<0 || receiveUtc<sendUtc || Math.abs((receiveUtc-sendUtc)-rtt)>4) throw Error('CLOCK_DISCONTINUITY');
  const lower=serverUtc-receiveUtc-2,upper=serverUtc-sendUtc+2;
  return {lowerMs:lower,upperMs:upper,midpointMs:(lower+upper)/2,uncertaintyMs:(upper-lower)/2,rttMs:rtt,elapsedMonoMs:receiveMono};
}
export function summarize(samples) {
  if(!samples.length || samples.length>4096) throw Error('BOUNDED_SAMPLE_COUNT');
  const best=xs=>xs.reduce((a,b)=>a.uncertaintyMs<=b.uncertaintyMs?a:b);
  const chosen=best(samples);const half=Math.floor(samples.length/2);
  let observedDrift=null;
  if(half){const first=best(samples.slice(0,half)),last=best(samples.slice(half));const duration=last.elapsedMonoMs-first.elapsedMonoMs;
    if(duration>0) observedDrift={durationMs:duration,deltaOffsetLowerMs:last.lowerMs-first.upperMs,deltaOffsetUpperMs:last.upperMs-first.lowerMs,midpointSlopePpm:(last.midpointMs-first.midpointMs)*1e6/duration,uncertaintyPpm:(last.uncertaintyMs+first.uncertaintyMs)*1e6/duration};
  }
  const rtt=samples.map(x=>x.rttMs).sort((a,b)=>a-b);
  return {N:samples.length,offsetUtcServerMinusClientMs:{lower:chosen.lowerMs,upper:chosen.upperMs,midpoint:chosen.midpointMs,uncertainty:chosen.uncertaintyMs},rttMs:{P50:rtt[Math.ceil(rtt.length*.5)-1],P95:rtt[Math.ceil(rtt.length*.95)-1]},observedDrift,method:'server ACK receive timestamp bracketed by client send/receipt; ±2ms integer quantization margin; no path symmetry assumed; stable-clock assumption; scheduling included',limitations:'Not optical/IMU truth or visible-response latency. Drift interval is descriptive over this run; no future accuracy guarantee. Same-host LAB does not certify phone↔PC clocks.'};
}
