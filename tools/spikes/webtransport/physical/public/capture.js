// Isolated probe: browser API values, never gameplay actions or wire-protocol DTOs.
const finite = value => typeof value === 'number' && Number.isFinite(value);
export class SensorCapture {
  constructor({ win = window, doc = document, now = () => performance.now(),
    notify = () => {}, setTimer = (fn, ms) => win.setInterval(fn, ms),
    clearTimer = id => win.clearInterval(id),
    interruptionMs = 2000 } = {}) {
    if (!finite(interruptionMs) || interruptionMs <= 0) throw new RangeError('interruptionMs');
    Object.assign(this, { win, doc, now, notify, setTimer, clearTimer, interruptionMs });
    this.generation = 0; this.timer = null; this.attached = false;
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
        intervals: 0, elapsedMs: 0, lastAt: null, previous: null, latest: null }]));
    this.sequence = 0; this.startedAt = null; this.screenAngle = this.readScreen();
  }
  readScreen() {
    const angle = this.win.screen?.orientation?.angle ?? this.win.orientation;
    return finite(angle) ? angle : null;
  }
  async start() {
    if (this.state === 'REQUESTING' || this.state === 'RUNNING' || this.state === 'SUSPENDED') return false;
    this.stop(); this.reset(); const generation = ++this.generation;
    if (this.win.isSecureContext !== true) { this.state = 'BLOCKED_INSECURE'; this.emit(); return false; }
    this.state = 'REQUESTING'; this.emit();
    // Invoke both methods synchronously from the caller's user gesture, before awaiting.
    const requests = ['motion', 'orientation'].map(name => {
      const api = this.win[name === 'motion' ? 'DeviceMotionEvent' : 'DeviceOrientationEvent'];
      if (!api) return Promise.resolve('api-absent');
      if (typeof api.requestPermission !== 'function') return Promise.resolve('not-required-by-api');
      try { return Promise.resolve(api.requestPermission()).then(value =>
        value === 'granted' || value === 'denied' ? value : 'permission-error', () => 'permission-error'); }
      catch { return Promise.resolve('permission-error'); }
    });
    const permissions = await Promise.all(requests);
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
    this.detach(); this.state = hidden ? 'SUSPENDED' : 'RUNNING';
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
    const clean = value => {
      if (value === null || value === undefined) { channel.missing++; return null; }
      if (!finite(value)) { channel.invalid++; return null; }
      return value;
    };
    const vector = (value, axes) => Object.fromEntries(axes.map(axis => [axis, clean(value?.[axis])]));
    const bounded = (value, low, high) => {
      const result = clean(value);
      if (result !== null && (result < low || result >= high)) { channel.invalid++; return null; }
      return result;
    };
    const interval = name !== 'motion' || event.interval == null ? null : clean(event.interval);
    const validInterval = interval !== null && interval <= 0 ? (channel.invalid++, null) : interval;
    const payload = name === 'motion' ? {
      acceleration: vector(event.acceleration, ['x', 'y', 'z']),
      accelerationIncludingGravity: vector(event.accelerationIncludingGravity, ['x', 'y', 'z']),
      rotationRate: vector(event.rotationRate, ['alpha', 'beta', 'gamma']),
      intervalMs: validInterval,
    } : { angles: { alpha: bounded(event.alpha, 0, 360), beta: bounded(event.beta, -180, 180), gamma: bounded(event.gamma, -90, 90) },
      absolute: typeof event.absolute === 'boolean' ? event.absolute : null };
    if (channel.previous !== null && at > channel.previous) {
      channel.intervals++; channel.elapsedMs += at - channel.previous;
    }
    channel.previous = at; channel.lastAt = at; channel.events++;
    channel.latest = { sequence: ++this.sequence, callbackMonotonicMs: at,
      eventTimestampMs: finite(event.timeStamp) ? event.timeStamp : null,
      eventTimestampMeaning: 'DOM event; hardware capture time unverified',
      screenAngleDegrees: this.screenAngle, ...payload };
    // Only the latest sample per source is retained; no raw history or network output.
  }
  snapshot() {
    const at = this.now();
    return { state: this.state, secureContext: this.win.isSecureContext === true,
      units: { acceleration: 'm/s²', rotationRate: 'degrees/s', orientation: 'degrees' },
      screenAngleDegrees: this.screenAngle,
      channels: Object.fromEntries(Object.entries(this.channels).map(([name, c]) => [name, {
        ...c, effectiveEventHz: c.elapsedMs > 0 ? c.intervals * 1000 / c.elapsedMs : null,
        interrupted: this.state === 'RUNNING' && ['granted', 'not-required-by-api'].includes(c.permission)
          && at - (c.lastAt ?? this.startedAt) >= this.interruptionMs,
        observedFields: c.latest ? Object.entries(name === 'motion' ? {
          ...Object.fromEntries(Object.entries(c.latest.acceleration).map(([k,v]) => ['acceleration.'+k,v])),
          ...Object.fromEntries(Object.entries(c.latest.accelerationIncludingGravity).map(([k,v]) => ['accelerationIncludingGravity.'+k,v])),
          ...Object.fromEntries(Object.entries(c.latest.rotationRate).map(([k,v]) => ['rotationRate.'+k,v]))
        } : c.latest.angles).filter(([,v]) => v !== null).map(([k]) => k) : [],
      }])) };
  }
  emit() { this.notify(this.snapshot()); }
}
