export async function checkServer(fetchImpl = fetch) {
  const response = await fetchImpl('/api/health', { signal: AbortSignal.timeout(5000) });
  if (!response.ok) throw new Error('Server unavailable');
  const health = await response.json();
  if (health.status !== 'ok' || health.protocolVersion !== 1) throw new Error('Incompatible server');
  return health;
}
