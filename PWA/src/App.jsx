import { useEffect, useRef, useState } from 'react';
import ReadyControl from './controller/ReadyControl.jsx';
import HealthProbe from './connection/HealthProbe.jsx';
import { ControllerSession } from './controller/session.js';
import { screens } from './controller/presentation.js';
import { controlProfile } from './controller/gamePresentation.js';

function GorillaMark() {
  return <svg className="gorilla-mark" viewBox="0 0 120 100" aria-hidden="true">
    <path fill="currentColor" d="M20 25 37 8h46l17 17 12 37-20 30H28L8 62z"/>
    <path fill="var(--core-sand)" d="m30 38 15-10 15 8 15-8 15 10-3 17-8 6 9 10-12 13H44L32 71l9-10-8-6z"/>
    <path fill="var(--core-forest)" d="M41 42h11v7H41zm27 0h11v7H68zM49 61h22l-4 7H53zm-4 14h30v4H45z"/>
  </svg>;
}
function GameAction({ session, action, children }) {
  const release = () => session.current?.setActionPressed(false, action);
  return <button className="secondary" style={{ touchAction: 'none' }}
    onPointerDown={event => { event.currentTarget.setPointerCapture(event.pointerId); session.current?.setActionPressed(true, action); }}
    onPointerUp={release} onPointerCancel={release} onLostPointerCapture={release} onBlur={release}
    onKeyDown={event => { if ([' ', 'Enter'].includes(event.key)) { event.preventDefault(); session.current?.setActionPressed(true, action); } }}
    onKeyUp={event => { if ([' ', 'Enter'].includes(event.key)) { event.preventDefault(); release(); } }}>
    {children}
  </button>;
}
function Controller() {
  const model = useRef(null);
  const [view, setView] = useState(null);
  const [diagnostic, setDiagnostic] = useState(null);
  const [copyStatus, setCopyStatus] = useState('');
  const [displayName, setDisplayName] = useState(() => {
    try { return localStorage.getItem('gorilla-player-name') || ''; } catch { return ''; }
  });
  const [nameStatus, setNameStatus] = useState(null);

  const savePlayerName = async (name) => {
    const trimmed = (name || '').normalize('NFC').trim();
    if (!trimmed) return 'INVALID';
    setNameStatus('SAVING');
    const status = await model.current?.setPlayerName(trimmed);
    setNameStatus(status);
    if (status === 'SAVED') {
      setDisplayName(trimmed);
      try { localStorage.setItem('gorilla-player-name', trimmed); } catch {}
    }
    return status;
  };

  useEffect(() => {
    if (view?.network === 'NETWORK_READY' && displayName.trim() && nameStatus === null && model.current) {
      void savePlayerName(displayName.trim());
    }
  }, [view?.network, displayName, nameStatus]);

  useEffect(() => {
    const session = new ControllerSession({ notify: setView });
    model.current = session;
    window.controller = session;
    window.client = session.client;
    setView(session.snapshot());
    if(session.savedResume || session.hasAdmission) void session.connect();
    return () => {
      window.controller = null;
      window.client = null;
      model.current = null;
      void session.dispose().catch(() => {});
    };
  }, []);
  const state = view?.state || 'NO_ADMISSION';
  const [title, description, label, feedback] = screens[state]||screens.ERROR;
  async function copyDiagnostic() {
    const report = model.current.diagnostic();
    setDiagnostic(report);
    try { await navigator.clipboard.writeText(JSON.stringify(report, null, 2)); setCopyStatus('Diagnóstico copiado.'); }
    catch { setCopyStatus('Copia el diagnóstico que aparece abajo.'); }
  }
  const canRetryPermission = state === 'ERROR' && view?.network === 'NETWORK_READY';
  const hasSavedName = nameStatus === 'SAVED' || (displayName.trim().length > 0 && nameStatus !== 'INVALID' && nameStatus !== 'UNAVAILABLE');
  if(hasSavedName && view?.network==='NETWORK_READY'&&(state==='INPUT_READY'||controlProfile(view.gameContext,view.playerId).valid))
    return <ReadyControl view={view} copyDiagnostic={copyDiagnostic} copyStatus={copyStatus} diagnostic={diagnostic} playerName={displayName} onSetName={savePlayerName} onCommand={(action,target)=>model.current?.command(action,target)} onActivate={()=>void model.current?.activate()} onForfeit={()=>void model.current?.forfeit()} onToggleAway={()=>void model.current?.setAway(!view?.isAway)} actions={<GameAction session={model} action="recenter">CENTRAR APUNTADO</GameAction>}/>;
  return <main className="controller-shell">
    <header className="brand"><GorillaMark/><div><p className="eyebrow">GORILIMPIADAS</p><h1>GORILLA<br/>ESCAPE</h1></div></header>
    <p className="edition">GORILLA CORE · TU TELÉFONO, TU CONTROL</p>
    <section className="connection-panel" data-feedback={feedback} aria-labelledby="connection-title">
      <div className="status-symbol" aria-hidden="true">{feedback === 'connecting' ? '…' : feedback === 'success' ? '✓' : feedback === 'error' ? '×' : '◆'}</div>
      <div role="status" aria-live="polite" aria-atomic="true"><h2 id="connection-title">{title}</h2><p>{description}</p></div>
      {view?.network === 'NETWORK_READY' && <p className="connection-confirmation">Conexión local establecida</p>}
      {state==='DISCONNECTED'&&<button className="primary" onClick={()=>void model.current.reconnect()}>RECONECTAR MI SESIÓN</button>}
      {state === 'ERROR' && view?.network === 'NOT_CONNECTED' && <div className="connection-recovery">
        <p>La conexión falló antes de recibir una respuesta de la PC. Comprueba este navegador antes de volver a escanear.</p>
        <button className="primary" disabled={view?.connectionCheck === 'CHECKING'} onClick={() => void model.current.checkConnection()}>{view?.connectionCheck === 'CHECKING' ? 'COMPROBANDO…' : 'COMPROBAR CONEXIÓN'}</button>
        {view?.connectionCheck === 'READY' && <p role="status">La PC responde desde este navegador. Cierra esta pestaña y escanea un QR nuevo en la PC.</p>}
        {['UNREACHABLE', 'INSECURE'].includes(view?.connectionCheck) && <p role="status">Este navegador no pudo verificar la conexión. Comprueba la misma Wi-Fi y que el juego siga abierto. Si usas conexión local, revisa la confianza del certificado; la PC ofrece Configurar este teléfono.</p>}
        {!view?.publicTunnel && <a className="prepare-link" href="/prepare" target="_blank" rel="noreferrer">Ver preparación de este teléfono</a>}
      </div>}
      {view?.network === 'NETWORK_READY' && !hasSavedName && <form className="profile-setup" onSubmit={async event => {
        event.preventDefault();
        await savePlayerName(displayName);
      }}>
        <label>¿Cómo te llamas en la partida?<input value={displayName} maxLength={40} autoComplete="nickname" autoFocus placeholder="Escribe tu nombre o apodo" onChange={e => setDisplayName(e.target.value)} disabled={nameStatus === 'SAVING'}/></label>
        <button className="primary" type="submit" disabled={!displayName.trim() || nameStatus === 'SAVING'}>{nameStatus === 'SAVING' ? 'GUARDANDO…' : 'ENTRAR AL JUEGO'}</button>
        <p role="status">{nameStatus === 'INVALID' ? 'Usa hasta 40 letras, números o signos simples.' : nameStatus === 'UNAVAILABLE' ? 'No se pudo guardar tu nombre. Comprueba la conexión e inténtalo de nuevo.' : 'Tu nombre aparecerá en la pantalla del juego.'}</p>
      </form>}
      <aside className="safety" aria-label="Seguridad"><strong>Sujeta firmemente tu teléfono.</strong><span>No lo sueltes ni lo lances durante el juego.</span></aside>
      {(label || canRetryPermission) && (state === 'CONNECT' || hasSavedName) && <button className="primary" onClick={() => {
        setDiagnostic(null); setCopyStatus('');
        if (state === 'CONNECT') void model.current.connect();
        else void model.current.activate();
      }}>{label || 'REINTENTAR PERMISO'}</button>}
    </section>
    <footer>
      {/android/i.test(navigator.userAgent) && (
        <a className="secondary open-in-app" style={{ display: 'block', textAlign: 'center', margin: '8px 0', textDecoration: 'none', padding: '10px', background: 'rgba(229, 189, 117, 0.15)', borderRadius: '8px', color: '#e5bd75' }}
           href={`intent://${location.host}${location.pathname}${location.search}${location.hash}#Intent;scheme=https;package=com.gorillaescape.gorilla_companion;end`}>
          Abrir en Gorilla Companion (APK)
        </a>
      )}
      <p>En la misma Wi-Fi que la PC.<br/>Sin instalar una aplicación.</p>
      <button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button>
      {view?.network === 'NETWORK_READY' && <details><summary>Medición técnica de sensores</summary>
        <p>Sujeta el teléfono. No lo lances ni lo agites con fuerza.</p>
        <button className="secondary" disabled={view?.measurement?.status === 'MEASURING'} onClick={() => model.current.startMeasurement('rest')}>Medir reposo · 15 s</button>
        <button className="secondary" disabled={view?.measurement?.status === 'MEASURING'} onClick={() => model.current.startMeasurement('gentle')}>Medir movimiento suave · 25 s</button>
        <p role="status">{view?.measurement?.status === 'MEASURING' ? 'Medición en curso. Mantén esta página visible.' : view?.measurement?.status === 'COMPLETE' ? 'Medición terminada. Copia el diagnóstico antes de iniciar otra.' : view?.measurement?.status === 'INTERRUPTED' ? 'Medición interrumpida.' : 'Las métricas aparecen al copiar el diagnóstico.'}</p>
      </details>}
      <p role="status" className="copy-status">{copyStatus}</p>
      {diagnostic && <details open><summary>Diagnóstico técnico</summary><pre tabIndex="0">{JSON.stringify(diagnostic, null, 2)}</pre></details>}
    </footer>
  </main>;
}
export default function App() {
  return new URLSearchParams(location.search).get('view') === 'health' ? <HealthProbe/> : <Controller/>;
}
