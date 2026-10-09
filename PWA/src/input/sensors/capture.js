// Browser acquisition only. No gameplay interpretation and no synthesized samples.
const finite = value => typeof value === 'number' && Number.isFinite(value);
// Timestamps only: bounded numeric window, never a raw sensor trace.
export class RateWindow {
  constructor(capacity = 4096) {
    if (!Number.isInteger(capacity) || capacity < 2 || capacity > 4096) throw new RangeError('capacity');
    this.values = new Float64Array(capacity); this.reset();
  }
  reset() { this.count = 0; this.size = 0; this.cursor = 0; this.previous = null; }
  add(at) {
    if (!finite(at) || (this.previous !== null && at < this.previous)) return false;
    this.values[this.cursor] = at; this.cursor = (this.cursor + 1) % this.values.length;
    this.size = Math.min(this.size + 1, this.values.length); this.count++; this.previous = at; return true;
  }
  metrics() {
    const times = Array.from({ length: this.size }, (_, i) => this.values[(this.cursor - this.size + i + this.values.length) % this.values.length]);
    const durationMs = times.length > 1 ? times.at(-1) - times[0] : 0;
    const intervals = times.slice(1).map((t, i) => t - times[i]).sort((a, b) => a - b);
    const percentile = p => intervals.length ? intervals[Math.max(0, Math.ceil(p * intervals.length) - 1)] : null;
    const actualMeasuredHz = durationMs > 0 ? (times.length - 1) * 1000 / durationMs : null;
    return { count: this.count, windowCount: this.size, capacity: this.values.length, durationMs,
      actualMeasuredHz, tier: frequencyTier(actualMeasuredHz), intervalMs: { min: percentile(0), median: percentile(.5), p95: percentile(.95) } };
  }
}
export function frequencyTier(hz) {
  if (!finite(hz) || hz < 0) return 'unmeasured';
  for (const tier of [60, 50, 30, 20]) if (hz >= tier) return tier;
  return 'degraded-below-20';
}
export class SensorCapture {
  constructor({ win = window, doc = document, now = () => performance.now(),
    notify = () => {}, onSample = () => {}, utcNow = () => Date.now(), setTimer = (fn, ms) => win.setInterval(fn, ms),
    clearTimer = id => win.clearInterval(id),
    interruptionMs = 2000 } = {}) {
    if (!finite(interruptionMs) || interruptionMs <= 0) throw new RangeError('interruptionMs');
    Object.assign(this, { win, doc, now, notify, onSample, utcNow, setTimer, clearTimer, interruptionMs });
    this.measurementEpoch = 0; this.generation = 0; this.timer = null; this.attached = false;
    this.motion = e => this.receive('motion', e);
    this.orientation = e => this.receive('orientation', e);
    this.visibility = () => this.suspend(this.doc.visibilityState === 'hidden');
    this.pagehide = () => this.stop();
    this.screenChange = () => { this.screenAngle = this.readScreen(); this.emit(); };
    this.state = 'STOPPED'; this.reset();
  }
  reset() {
    this.channels = Object.fromEntries(['motion', 'orientation'].map(name => [name,
      { permission: 'not-requested', events: 0, invalid: 0, missing: 0,
        intervals: 0, elapsedMs: 0, lastAt: null, previous: null, latest: null,
        sequence: 0, validSequence: 0, lastEvent: null, lastTimestamp: null,
        rawRate: new RateWindow(), validRate: new RateWindow(), duplicates: 0, timingRejected: 0, rawCallbacks: 0,
        validAt: null, validSamples: 0, missingAxes: {}, invalidAxes: {} }]));
    this.startedAt = null; this.screenAngle = this.readScreen();
  }
  readScreen() {
    const angle = this.win.screen?.orientation?.angle ?? this.win.orientation;
    return finite(angle) ? angle : null;
  }
  async start({ permissions: provided } = {}) {
    if (this.state === 'REQUESTING' || this.state === 'RUNNING' || this.state === 'SUSPENDED') return false;
    this.stop(); this.reset(); const generation = ++this.generation;
    if (this.win.isSecureContext !== true) { this.state = 'BLOCKED_INSECURE'; this.emit(); return false; }
    this.state = 'REQUESTING'; this.emit();
    // Invoke both methods synchronously from the caller's user gesture, before awaiting.
    const requests = ['motion', 'orientation'].map(name => {
      if (provided) return Promise.resolve(provided[name]);
      const api = this.win[name === 'motion' ? 'DeviceMotionEvent' : 'DeviceOrientationEvent'];
      if (!api) return Promise.resolve('api-absent');
      if (typeof api.requestPermission !== 'function') return Promise.resolve('not-required-by-api');
      try { return Promise.resolve(api.requestPermission()).then(value =>
        value === 'granted' || value === 'denied' ? value : 'permission-error', () => 'permission-error'); }
      catch { return Promise.resolve('permission-error'); }
    });
    const permissions = provided ? ['motion', 'orientation'].map(name => provided[name]) : await Promise.all(requests);
    if (generation !== this.generation) return false;
    permissions.forEach((value, i) => { this.channels[i === 0 ? 'motion' : 'orientation'].permission = value; });
    if (!permissions.some(p => p === 'granted' || p === 'not-required-by-api')) {
      this.state = 'UNAVAILABLE'; this.emit(); return false;
    }
    this.startedAt = this.now();
    this.doc.addEventListener('visibilitychange', this.visibility);
    this.win.addEventListener('pagehide', this.pagehide);
    this.win.addEventListener('orientationchange', this.screenChange);
    this.win.screen?.orientation?.addEventListener('change', this.screenChange);
    this.state = this.doc.visibilityState === 'hidden' ? 'SUSPENDED' : 'RUNNING';
    if (this.state === 'RUNNING') this.attach();
    this.timer = this.setTimer(() => this.emit(), 500);
    this.emit(); return true;
  }
  attach() {
    if (this.attached) return;
    for (const name of ['motion', 'orientation']) {
      if (['granted', 'not-required-by-api'].includes(this.channels[name].permission))
        this.win.addEventListener(name === 'motion' ? 'devicemotion' : 'deviceorientation', this[name]);
    }
    this.attached = true;
  }
  detach() {
    this.win.removeEventListener('devicemotion', this.motion);
    this.win.removeEventListener('deviceorientation', this.orientation);
    this.attached = false;
    for (const channel of Object.values(this.channels)) channel.previous = null;
  }
  suspend(hidden) {
    if (!['RUNNING', 'SUSPENDED'].includes(this.state)) return;
    ++this.measurementEpoch; this.detach(); this.state = hidden ? 'SUSPENDED' : 'RUNNING';
    if (!hidden) {
      this.startedAt = this.now();
      for (const channel of Object.values(this.channels)) channel.lastAt = null;
      this.attach();
    }
    this.emit();
  }
  stop() {
    ++this.generation; this.detach();
    if (this.timer !== null) this.clearTimer(this.timer);
    this.timer = null;
    this.doc.removeEventListener('visibilitychange', this.visibility);
    this.win.removeEventListener('pagehide', this.pagehide);
    this.win.removeEventListener('orientationchange', this.screenChange);
    this.win.screen?.orientation?.removeEventListener('change', this.screenChange);
    this.state = 'STOPPED';
  }
  receive(name, event) {
    if (this.state !== 'RUNNING') return;
    const channel = this.channels[name]; const at = this.now();
    channel.rawCallbacks++; channel.rawRate.add(at);
    const stamp = finite(event.timeStamp) && event.timeStamp > 0 ? event.timeStamp : null;
    if (event === channel.lastEvent || (stamp !== null && channel.lastTimestamp !== null && stamp <= channel.lastTimestamp)) {
      channel.duplicates++; return;
    }
    channel.lastEvent = event; if (stamp !== null) channel.lastTimestamp = stamp;
    if (channel.previous !== null && at < channel.previous) { channel.timingRejected++; return; }
    const clean = (value, axis = 'interval') => {
      if (value === null || value === undefined) { channel.missing++; channel.missingAxes[axis] = (channel.missingAxes[axis] || 0) + 1; return null; }
      if (!finite(value)) { channel.invalid++; channel.invalidAxes[axis] = (channel.invalidAxes[axis] || 0) + 1; return null; }
      return value;
    };
    const vector = (value, axes, group) => Object.fromEntries(axes.map(axis => [axis, clean(value?.[axis], `${group}.${axis}`)]));
    const bounded = (value, low, high, axis) => {
      const result = clean(value, axis);
      if (result !== null && (result < low || result >= high)) { channel.invalid++; return null; }
      return result;
    };
    const interval = name !== 'motion' || event.interval == null ? null : clean(event.interval);
    const validInterval = interval !== null && interval <= 0 ? (channel.invalid++, null) : interval;
    const payload = name === 'motion' ? {
      acceleration: vector(event.acceleration, ['x', 'y', 'z'], 'acceleration'),
      accelerationIncludingGravity: vector(event.accelerationIncludingGravity, ['x', 'y', 'z'], 'accelerationIncludingGravity'),
      rotationRate: vector(event.rotationRate, ['alpha', 'beta', 'gamma'], 'rotationRate'),
      intervalMs: validInterval,
    } : { angles: { alpha: bounded(event.alpha, 0, 360, 'alpha'), beta: bounded(event.beta, -180, 180, 'beta'), gamma: bounded(event.gamma, -90, 90, 'gamma') },
      absolute: typeof event.absolute === 'boolean' ? event.absolute : null };
    if (channel.previous !== null && at > channel.previous) {
      channel.intervals++; channel.elapsedMs += at - channel.previous;
    }
    channel.previous = at; channel.lastAt = at; channel.events++;
    channel.latest = { sequence: ++channel.sequence, callbackMonotonicMs: at, receiptUtcMs: this.utcNow(),
      eventTimestampMs: finite(event.timeStamp) ? event.timeStamp : null,
      eventTimestampMeaning: 'DOM event; hardware capture time unverified',
      screenAngleDegrees: this.screenAngle, ...payload };
    // Values may be unchanged at rest; identity is an event, not vector content.
    const groups = name === 'motion' ? [payload.acceleration, payload.accelerationIncludingGravity, payload.rotationRate] : [payload.angles];
    if (groups.some(group => Object.values(group).some(finite))) {
      channel.validSamples++; channel.validSequence++; channel.validAt = at; channel.validRate.add(at);
      this.onSample(this.snapshot());
    }
    // Only the latest sample per source is retained.
  }
  snapshot() {
    const at = this.now();
    return { generation: this.generation, state: this.state, secureContext: this.win.isSecureContext === true,
      units: { acceleration: 'm/s²', rotationRate: 'degrees/s', orientation: 'degrees' },
      screenAngleDegrees: this.screenAngle,
      channels: Object.fromEntries(Object.entries(this.channels).map(([name, c]) => [name, {
        permission: c.permission, events: c.events, invalid: c.invalid, missing: c.missing,
        latest: c.latest && structuredClone(c.latest), validSequence: c.validSequence,
        effectiveEventHz: c.elapsedMs > 0 ? c.intervals * 1000 / c.elapsedMs : null,
        interrupted: this.state === 'RUNNING' && ['granted', 'not-required-by-api'].includes(c.permission)
          && at - (c.lastAt ?? this.startedAt) >= this.interruptionMs,
        observedFields: c.latest ? Object.entries(name === 'motion' ? {
          ...Object.fromEntries(Object.entries(c.latest.acceleration).map(([k,v]) => ['acceleration.'+k,v])),
          ...Object.fromEntries(Object.entries(c.latest.accelerationIncludingGravity).map(([k,v]) => ['accelerationIncludingGravity.'+k,v])),
          ...Object.fromEntries(Object.entries(c.latest.rotationRate).map(([k,v]) => ['rotationRate.'+k,v]))
        } : c.latest.angles).filter(([,v]) => v !== null).map(([k]) => k) : [],
      }])) };
  }
  resetMetrics() {
    for (const c of Object.values(this.channels)) {
      c.rawRate.reset(); c.validRate.reset(); c.validSamples = 0; c.events = 0; c.invalid = 0; c.missing = 0;
      c.rawCallbacks = 0; c.duplicates = 0; c.timingRejected = 0; c.missingAxes = {}; c.invalidAxes = {};
    }
  }
  metrics() {
    const at = this.now();
    const availability = group => {
      const values = Object.values(group || {}); const n = values.filter(finite).length;
      return n === 0 ? 'unavailable' : n === values.length ? 'present' : 'partial';
    };
    return { captureRevision: 't010-2-coarsened-clock', state: this.state, measurementClock: 'monotonic callback receipt',
      channels: Object.fromEntries(Object.entries(this.channels).map(([name, c]) => [name, {
        permission: c.permission, captured: c.events, rawCallbacks: c.rawCallbacks, validSamples: c.validSamples, rawAcquisition: c.rawRate.metrics(), validUnique: c.validRate.metrics(),
        duplicateOrOutOfOrder: c.duplicates, timingRejected: c.timingRejected,
        invalidFields: c.invalid, missingFields: c.missing, missingAxes: { ...c.missingAxes }, invalidAxes: { ...c.invalidAxes },
        ageMs: c.validAt === null ? null : Math.max(0, at - c.validAt),
        availability: name === 'motion' ? Object.fromEntries(['acceleration', 'accelerationIncludingGravity', 'rotationRate'].map(g => [g, availability(c.latest?.[g])])) : { orientation: availability(c.latest?.angles) },
      }])) };
  }
  inputState() {
    // O(1) readiness; sorting bounded metric windows belongs to diagnostics only.
    const at = this.now();
    const usable = Object.values(this.channels).filter(c => ['granted', 'not-required-by-api'].includes(c.permission));
    const fresh = c => c.validSamples >= 3 && c.validAt !== null && at - c.validAt < this.interruptionMs;
    if (!usable.length) return 'SENSOR_UNAVAILABLE';
    if (usable.every(fresh)) {
      const m = this.channels.motion.latest, o = this.channels.orientation.latest;
      const full = usable.length === 2 && [m?.acceleration, m?.accelerationIncludingGravity, m?.rotationRate, o?.angles].every(g => g && Object.values(g).every(finite));
      return full ? 'INPUT_READY' : 'INPUT_LIMITED';
    }
    if (this.state !== 'RUNNING' || at - this.startedAt >= this.interruptionMs) return 'NO_SENSOR_INPUT';
    return 'PREPARING_SENSORS';
  }
  emit() { this.notify(this.snapshot()); }
}
