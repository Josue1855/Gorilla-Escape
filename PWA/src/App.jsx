import { useEffect, useRef, useState } from 'react';
import { checkServer } from './connection/health.js';

export default function App() {
  const [state, setState] = useState('idle');
  const [message, setMessage] = useState('Abre la dirección HTTPS que muestra el operador de la PC.');
  const [diagnostic, setDiagnostic] = useState(null);
  const checking = useRef(false);
  const [worker, setWorker] = useState('pendiente');
  useEffect(() => {
    if (!('serviceWorker' in navigator)) { setWorker('no disponible'); return; }
    let active = true;
    const update = () => { if (active) setWorker(navigator.serviceWorker.controller ? 'controlando el shell' : 'activo; recarga para comprobar control'); };
    navigator.serviceWorker.ready.then(update).catch(() => { if (active) setWorker('no disponible'); });
    navigator.serviceWorker.addEventListener('controllerchange', update);
    return () => { active = false; navigator.serviceWorker.removeEventListener('controllerchange', update); };
  }, []);
  async function check() {
    if (checking.current) return;
    checking.current = true;
    setState('checking'); setDiagnostic(null);
    try {
      const started = performance.now();
      const health = await checkServer();
      if (location.protocol === 'https:' && (!window.isSecureContext || !health.secure)) throw new Error('Insecure transport');
      setDiagnostic({ ...health, elapsedMs: performance.now() - started });
      setMessage('Java respondió a esta petición. Esta comprobación no te incorpora a una partida.');
      setState('responding');
    } catch {
      setMessage('No podemos contactar con la PC. Comprueba el Wi-Fi, la dirección y que Java siga abierto. Vuelve a comprobar.');
      setState('error');
    } finally { checking.current = false; }
  }
  return <main>
    <p className="eyebrow">GORILIMPIADAS · PRUEBA LAN</p>
    <h1>Comprueba tu conexión</h1>
    <section aria-label="Estado de la comprobación">
      <p role="status" aria-live="polite">{message}</p>
      <button onClick={check} disabled={state === 'checking'}>
        {state === 'checking' ? 'Comprobando…' : 'Comprobar Java ahora'}
      </button>
      <p>Contexto seguro: {window.isSecureContext ? 'sí' : 'no'} · HTTPS: {location.protocol === 'https:' ? 'sí' : 'no'}</p>
      <p>Service worker: {worker}</p>
      {diagnostic && <div>
        <p>Servidor: <code>{diagnostic.serverInstanceId}</code></p>
        <p>Petición recibida: {diagnostic.requestNumber} · respuesta en {diagnostic.elapsedMs.toFixed(1)} ms</p>
        <p>Resultado puntual de esta petición; pulsa el botón para verificar de nuevo.</p>
      </div>}
    </section>
    <p>No se solicitan sensores ni cámara. Si el navegador muestra un error de certificado, pide al operador revisar la confianza antes de continuar.</p>
    <p className="safety">Sujeta siempre el teléfono.</p>
  </main>;
}
