import { MobileClient } from '../input/mobileClient.js';
import { permissionCapabilities, requestControlPermissions, permissionState } from './permissions.js';

export class ControllerSession {
  constructor({ win = window, url = win.location.href, client = new MobileClient({ source: 'physical' }), notify = () => {} } = {}) {
    this.win = win; this.url = url; this.client = client; this.notify = notify;
    const fragment = new URLSearchParams(new URL(url).hash.slice(1));
    this.hasAdmission = Boolean(fragment.get('sessionId') && fragment.get('admission'));
    this.state = this.hasAdmission ? 'CONNECT' : 'NO_ADMISSION';
    this.network = 'NOT_CONNECTED'; this.attempted = false; this.disposed = false;
    this.capabilities = permissionCapabilities(win); this.permissions = null;
  }
  snapshot() { return { state: this.state, network: this.network, capabilities: { ...this.capabilities }, permissions: this.permissions && { ...this.permissions } }; }
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
          this.network = 'DISCONNECTED'; this.state = 'DISCONNECTED';
          this.win.clearInterval(this.monitor); this.publish();
          void this.client.close(false).catch(() => {});
        }
      }, 500);
    } catch {
      if (!this.disposed) { this.state = 'ERROR'; this.network = 'NOT_CONNECTED'; this.publish(); }
    }
  }
  activate() {
    if (this.disposed || this.network !== 'NETWORK_READY' || this.requesting ||
      !['PERMISSION_REQUIRED', 'PERMISSION_DENIED', 'SENSOR_UNAVAILABLE', 'ERROR'].includes(this.state)) return;
    this.requesting = true;
    const request = requestControlPermissions(this.win); // Invoke BEFORE notifying/rendering/awaiting.
    this.state = 'REQUESTING_PERMISSION'; this.publish();
    return request.then(report => {
      if (this.disposed || this.network !== 'NETWORK_READY') return;
      this.permissions = report.permissions; this.capabilities = report.capabilities;
      this.state = permissionState(report); this.publish();
    }).finally(() => { this.requesting = false; });
  }
  diagnostic() {
    // MobileClient owns the bounded, sanitized transport report. Never copy URL/identity/error text.
    return { ...this.client.diagnostic(), controller: this.snapshot(), signalVerified: false };
  }
  dispose() {
    this.disposed = true; this.win.clearInterval(this.monitor);
    return this.client.close(false); // Owned transport cleanup only; no new lifecycle policy.
  }
}
