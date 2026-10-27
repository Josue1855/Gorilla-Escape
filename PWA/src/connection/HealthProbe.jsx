import { useEffect, useRef, useState } from 'react';
import {FixedScreen,PagedText} from '../controller/FixedScreen.jsx';
import { checkServer } from './health.js';

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
  return <FixedScreen title="Comprobar conexión" nav={<button onClick={check} disabled={state==='checking'}>{state==='checking'?'Comprobando…':'Comprobar ahora'}</button>}>
    <div className="fixed-section"><h2>Conexión con la PC</h2><PagedText text={`${message}\n\nContexto seguro: ${window.isSecureContext?'sí':'no'}. HTTPS: ${location.protocol==='https:'?'sí':'no'}.\nService worker: ${worker}\n${diagnostic?JSON.stringify(diagnostic,null,2):''}\nNo se solicitan sensores ni cámara. Ante un error de certificado, revisa la confianza con el operador.`}/></div>
  </FixedScreen>;
}
