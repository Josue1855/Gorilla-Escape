import { useEffect, useRef, useState } from 'react';
import HealthProbe from './connection/HealthProbe.jsx';
import { ControllerSession } from './controller/session.js';
import { screens } from './controller/presentation.js';

function GorillaMark() {
  return <svg className="gorilla-mark" viewBox="0 0 120 100" aria-hidden="true">
    <path fill="currentColor" d="M20 25 37 8h46l17 17 12 37-20 30H28L8 62z"/>
    <path fill="var(--core-sand)" d="m30 38 15-10 15 8 15-8 15 10-3 17-8 6 9 10-12 13H44L32 71l9-10-8-6z"/>
    <path fill="var(--core-forest)" d="M41 42h11v7H41zm27 0h11v7H68zM49 61h22l-4 7H53zm-4 14h30v4H45z"/>
  </svg>;
}
function Controller() {
  const model = useRef(null);
  const [view, setView] = useState(null);
  const [diagnostic, setDiagnostic] = useState(null);
  const [copyStatus, setCopyStatus] = useState('');
  useEffect(() => {
    const session = new ControllerSession({ notify: setView });
    model.current = session; setView(session.snapshot());
    return () => { model.current = null; void session.dispose().catch(() => {}); };
  }, []);
  const state = view?.state || 'NO_ADMISSION';
  const [title, description, label, feedback] = screens[state];
  async function copyDiagnostic() {
    const report = model.current.diagnostic();
    setDiagnostic(report);
    try { await navigator.clipboard.writeText(JSON.stringify(report, null, 2)); setCopyStatus('Diagnóstico copiado.'); }
    catch { setCopyStatus('Copia el diagnóstico que aparece abajo.'); }
  }
  const canRetryPermission = state === 'ERROR' && view?.network === 'NETWORK_READY';
  return <main className="controller-shell">
    <header className="brand"><GorillaMark/><div><p className="eyebrow">GORILIMPIADAS</p><h1>GORILLA<br/>ESCAPE</h1></div></header>
    <p className="edition">GORILLA CORE · TU TELÉFONO, TU CONTROL</p>
    <section className="connection-panel" data-feedback={feedback} aria-labelledby="connection-title">
      <div className="status-symbol" aria-hidden="true">{feedback === 'connecting' ? '…' : feedback === 'success' ? '✓' : feedback === 'error' ? '×' : '◆'}</div>
      <div role="status" aria-live="polite" aria-atomic="true"><h2 id="connection-title">{title}</h2><p>{description}</p></div>
      {view?.network === 'NETWORK_READY' && <p className="connection-confirmation">Conexión local establecida</p>}
      <aside className="safety" aria-label="Seguridad"><strong>Sujeta firmemente tu teléfono.</strong><span>No lo sueltes ni lo lances durante el juego.</span></aside>
      {(label || canRetryPermission) && <button className="primary" onClick={() => {
        setDiagnostic(null); setCopyStatus('');
        if (state === 'CONNECT') void model.current.connect();
        else void model.current.activate();
      }}>{label || 'REINTENTAR PERMISO'}</button>}
    </section>
    <footer><p>En la misma Wi-Fi que la PC.<br/>Sin instalar una aplicación.</p>
      <button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button>
      <p role="status" className="copy-status">{copyStatus}</p>
      {diagnostic && <details open><summary>Diagnóstico técnico</summary><pre tabIndex="0">{JSON.stringify(diagnostic, null, 2)}</pre></details>}
    </footer>
  </main>;
}
export default function App() {
  return new URLSearchParams(location.search).get('view') === 'health' ? <HealthProbe/> : <Controller/>;
}
