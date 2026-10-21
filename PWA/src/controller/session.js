import { SensorCapture, RateWindow } from '../input/sensors/capture.js';
import { MobileClient, bindCapture } from '../input/mobileClient.js';
import { permissionCapabilities, requestControlPermissions, permissionState } from './permissions.js';

export class ControllerSession {
  constructor({ win = window, url = win.location.href, client = new MobileClient({ source: 'physical' }), notify = () => {}, doc = win.document, now = () => performance.now() } = {}) {
    this.win = win; this.now = now; this.url = url; this.client = client; this.notify = notify;
    const fragment = new URLSearchParams(new URL(url).hash.slice(1));
    this.publicTunnel = new URL(url).hostname.endsWith('.trycloudflare.com');
    this.hasAdmission = Boolean(fragment.get('sessionId') && fragment.get('admission'));
    this.state = this.hasAdmission ? 'CONNECT' : 'NO_ADMISSION';
    this.connectionCheck = null; this.controlTilt = 0; this.controlNeutral = null; this.lastControlPublish = -Infinity;
    this.network = 'NOT_CONNECTED'; this.attempted = false; this.disposed = false;
    this.capabilities = permissionCapabilities(win); this.permissions = null;
    this.pagehide = () => {
      if (!this.disposed) { this.network = 'DISCONNECTED'; this.state = 'DISCONNECTED'; this.publish(); }
      void this.dispose().catch(() => {});
    };
    this.win.addEventListener('pagehide', this.pagehide);
    this.emissionRate = new RateWindow(); this.measurement = null; this.measureTimer = null;
    this.capture = new SensorCapture({ win, doc, now, notify: () => this.updateInput(),
      onSample: snapshot => {
        this.emitSample(snapshot); this.updateInput();
        const gamma = snapshot.channels?.orientation?.latest?.angles?.gamma;
        if (this.state === 'INPUT_READY' && Number.isFinite(gamma)) {
          this.controlNeutral ??= gamma;
          this.controlTilt = Math.max(-1, Math.min(1, (gamma - this.controlNeutral) / 30));
          if (this.now() - this.lastControlPublish >= 60) { this.lastControlPublish = this.now(); this.publish(); }
        }
      } });
    // Reuse acquisition/emission measurements; only bounded aggregates cross the control channel.
    this.client.qaTelemetry = () => {
      const metrics = this.capture.metrics(), rtt = [...(this.client.metrics?.rtt || [])].sort((a,b) => a-b);
      return { qaVersion: 1, motionHz: metrics.channels.motion.validUnique.actualMeasuredHz,
        orientationHz: metrics.channels.orientation.validUnique.actualMeasuredHz,
        emissionHz: this.emissionRate.metrics().actualMeasuredHz,
        rttP50: rtt.length ? rtt[Math.ceil(rtt.length * .5)-1] : null,
        rttP95: rtt.length ? rtt[Math.ceil(rtt.length * .95)-1] : null };
    };
    this.emitSample = bindCapture(this.capture, client, { maximumHz: 50, onEmit: () => this.emissionRate.add(now()) });
  }
  snapshot() { return { state: this.state, network: this.network, connectionCheck: this.connectionCheck, publicTunnel: this.publicTunnel, controlTilt: this.controlTilt, capabilities: { ...this.capabilities }, permissions: this.permissions && { ...this.permissions }, measurement: this.measurement && { mode: this.measurement.mode, status: this.measurement.status } }; }
  publish() { if (!this.disposed) this.notify(this.snapshot()); }
  async connect() {
    if (this.disposed || this.attempted || !this.hasAdmission) return;
    this.attempted = true; this.state = 'CONNECTING'; this.publish();
    try {
      await this.client.connect(this.url);
      if (this.disposed) { await this.client.close(false); return; }
      this.network = 'NETWORK_READY'; this.state = 'PERMISSION_REQUIRED'; this.publish();
      // Observe the existing transport, without implementing reconnection or sensor lifecycle.
      this.monitor = this.win.setInterval(() => {
        if (this.client.state === 'DISCONNECTED' || this.client.state === 'FAILED') {
          this.network = 'DISCONNECTED'; this.state = 'DISCONNECTED'; this.capture.stop(); this.cancelMeasurement();
          this.win.clearInterval(this.monitor); this.publish();
          void this.client.close(false).catch(() => {});
        }
      }, 500);
    } catch {
      if (!this.disposed) { this.state = 'ERROR'; this.network = 'NOT_CONNECTED'; this.publish(); }
    }
  }
  async checkConnection() {
    if (this.disposed || this.connectionCheck === 'CHECKING') return;
    if (!this.win.isSecureContext) { this.connectionCheck = 'INSECURE'; this.publish(); return; }
    this.connectionCheck = 'CHECKING'; this.publish();
    const abort = new AbortController(); const timeout = this.win.setTimeout(() => abort.abort(), 5000);
    try {
      const response = await this.client.fetcher(this.publicTunnel ? '/' : '/api/health', { cache: 'no-store', signal: abort.signal });
      let ready = false;
      if (response.ok && this.publicTunnel) ready = (await response.text()).includes('Gorilla Escape');
      else if (response.ok) {
        const health = await response.json();
        ready = health?.status === 'ok' && health?.service === 'gorilla-escape-local' && health?.secure === true;
      }
      this.connectionCheck = ready ? 'READY' : 'UNREACHABLE';
    } catch { this.connectionCheck = 'UNREACHABLE'; }
    finally { this.win.clearTimeout(timeout); if (!this.disposed) this.publish(); }
  }
  activate() {
    if (this.disposed || this.network !== 'NETWORK_READY' || this.requesting ||
      !['PERMISSION_REQUIRED', 'PERMISSION_DENIED', 'SENSOR_UNAVAILABLE', 'NO_SENSOR_INPUT', 'INPUT_LIMITED', 'ERROR'].includes(this.state)) return;
    this.requesting = true;
    const request = requestControlPermissions(this.win); // Invoke BEFORE notifying/rendering/awaiting.
    this.state = 'REQUESTING_PERMISSION'; this.publish();
    return request.then(async report => {
      if (this.disposed || this.network !== 'NETWORK_READY') return;
      this.permissions = report.permissions; this.capabilities = report.capabilities;
      this.state = permissionState(report);
      if (['PERMISSION_GRANTED', 'SENSOR_UNAVAILABLE'].includes(this.state) && Object.values(report.permissions).some(p => ['granted', 'not-required-by-api'].includes(p))) {
        this.capture.stop();
        await this.capture.start({ permissions: report.permissions });
        if (this.disposed || this.network !== 'NETWORK_READY') { this.capture.stop(); return; }
        this.updateInput();
      }
      this.publish();
    }).finally(() => { this.requesting = false; });
  }
  updateInput() {
    if (this.disposed || this.network !== 'NETWORK_READY' || !['RUNNING', 'SUSPENDED'].includes(this.capture.state)) return;
    const state = this.capture.inputState();
    if (state === 'CONTROL_SUSPENDED') {
      this.cancelMeasurement();
      this.client.latest = null; // Discard unsent pre-background input, leave an already-sent frame alone.
    }
    if (this.state !== state) { this.state = state; this.publish(); }
  }
  transportCounters() {
    const m = this.client.metrics || {};
    return Object.fromEntries(['motionSubmitted', 'motionSent', 'motionAck', 'motionErrors', 'overwritten', 'errors'].map(k => [k, m[k] || 0]));
  }
  startMeasurement(mode) {
    if (this.disposed || this.network !== 'NETWORK_READY' || this.capture.state !== 'RUNNING' || this.measureTimer !== null || !['rest', 'gentle'].includes(mode)) return false;
    this.capture.resetMetrics(); this.emissionRate.reset();
    const start = this.now(), baseline = this.transportCounters(), emissionBaseline = this.emitSample.metrics();
    const generation = this.capture.generation, epoch = this.capture.measurementEpoch;
    this.measurement = { mode, status: 'MEASURING' }; this.publish();
    this.measureTimer = this.win.setTimeout(() => {
      this.measureTimer = null;
      const complete = this.network === 'NETWORK_READY' && this.capture.state === 'RUNNING' && this.capture.generation === generation && this.capture.measurementEpoch === epoch;
      const current = this.transportCounters();
      this.measurement = { mode, status: complete ? 'COMPLETE' : 'INTERRUPTED', durationMs: this.now() - start,
        capture: this.capture.metrics(), emission: this.emissionRate.metrics(), emissionPolicy: { ...this.emitSample.metrics(), emitted: this.emitSample.metrics().emitted - emissionBaseline.emitted, budgetDropped: this.emitSample.metrics().budgetDropped - emissionBaseline.budgetDropped },
        transportDelta: Object.fromEntries(Object.keys(current).map(k => [k, current[k] - baseline[k]])),
        inputState: this.state, source: 'browser events; physical provenance requires operator confirmation' };
      this.publish();
    }, mode === 'rest' ? 15500 : 25000);
    return true;
  }
  cancelMeasurement() {
    if (this.measureTimer !== null) {
      this.win.clearTimeout(this.measureTimer); this.measureTimer = null;
      this.measurement = { ...this.measurement, status: 'INTERRUPTED' };
    }
  }
  diagnostic() {
    // MobileClient owns the bounded, sanitized transport report. Never copy URL/identity/error text.
    return { ...this.client.diagnostic(), controller: this.snapshot(), signalVerified: this.state === 'INPUT_READY', sensors: this.capture.metrics(), emissionPolicy: this.emitSample.metrics(), emission: this.emissionRate.metrics(), transport: this.transportCounters(), measurement: this.measurement && structuredClone(this.measurement) };
  }
  async dispose() {
    if (this.disposed) return;
    this.disposed = true; this.client.qaTelemetry = null; this.win.removeEventListener('pagehide', this.pagehide); this.win.clearInterval(this.monitor); this.capture.stop(); this.cancelMeasurement();
    return this.client.close(false); // Owned transport cleanup only; no new lifecycle policy.
  }
}
