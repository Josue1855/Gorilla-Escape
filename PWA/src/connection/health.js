export async function checkServer(fetchImpl = fetch) {
  const response = await fetchImpl('/api/health', { cache: 'no-store', signal: AbortSignal.timeout(5000) });
  if (!response.ok) throw new Error('Server unavailable');
  const health = await response.json();
  if (health.status !== 'ok' || health.protocolVersion !== 1 || health.service !== 'gorilla-escape-local'
    || !/^[a-f0-9]{8}(-[a-f0-9]{4}){3}-[a-f0-9]{12}$/.test(health.serverInstanceId)
    || !Number.isSafeInteger(health.requestNumber) || health.requestNumber < 1
    || typeof health.secure !== 'boolean') throw new Error('Incompatible server');
  return health;
}
