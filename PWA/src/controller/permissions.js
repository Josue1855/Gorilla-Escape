// Probe API presence and permission only. Hardware/signal quality belong to T010.
const APIs = { motion: 'DeviceMotionEvent', orientation: 'DeviceOrientationEvent' };
export function permissionCapabilities(win) {
  return Object.fromEntries(Object.entries(APIs).map(([name, key]) => [name,
    !win[key] ? 'api-absent' : typeof win[key].requestPermission === 'function'
      ? 'request-required' : 'not-required-by-api']));
}
export function requestControlPermissions(win) {
  const capabilities = permissionCapabilities(win);
  if (win.isSecureContext !== true) return Promise.resolve({ secure: false, capabilities,
    permissions: { motion: 'insecure-context', orientation: 'insecure-context' } });
  // Both calls happen synchronously within the click's transient activation, before any await.
  const requests = Object.entries(APIs).map(([name, key]) => {
    const capability = capabilities[name];
    if (capability !== 'request-required') return Promise.resolve([name, capability]);
    try {
      return Promise.resolve(win[key].requestPermission()).then(value =>
        [name, value === 'granted' || value === 'denied' ? value : 'permission-error'],
      () => [name, 'permission-error']);
    } catch { return Promise.resolve([name, 'permission-error']); }
  });
  return Promise.all(requests).then(entries => ({ secure: true, capabilities,
    permissions: Object.fromEntries(entries) }));
}
export function permissionState(report) {
  const values = Object.values(report.permissions);
  if (!report.secure) return 'ERROR';
  if (values.includes('denied')) return 'PERMISSION_DENIED';
  if (values.includes('permission-error')) return 'ERROR';
  if (values.includes('api-absent')) return 'SENSOR_UNAVAILABLE';
  return 'PERMISSION_GRANTED'; // Never INPUT_READY: no sensor events have been verified.
}
