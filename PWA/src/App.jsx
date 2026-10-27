import { useEffect, useRef, useState } from 'react';
import ReadyControl from './controller/ReadyControl.jsx';
import {FixedScreen,PagedText} from './controller/FixedScreen.jsx';
import HealthProbe from './connection/HealthProbe.jsx';
import { ControllerSession } from './controller/session.js';
import { screens } from './controller/presentation.js';

function Controller() {
  const model = useRef(null);
  const [view, setView] = useState(null);
  const [diagnostic, setDiagnostic] = useState(null);
  const [copyStatus, setCopyStatus] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [profileStatus, setProfileStatus] = useState(null);
  const [panel,setPanel]=useState('control');
  const [pin, setPin] = useState('');
  const [pinStatus, setPinStatus] = useState('');
  useEffect(() => {
    const session = new ControllerSession({ notify: setView });
    model.current = session; setView(session.snapshot());
    const incoming=new URLSearchParams(location.hash.slice(1)).get('room');
    const roomTimer=incoming?window.setTimeout(()=>{setPin(incoming);setPinStatus('Conectando…');void session.joinWithPin(incoming).then(result=>{if(result!=='JOINED'&&result!=='REDIRECTING'&&!session.disposed)setPinStatus('No se pudo entrar. Revisa el código e inténtalo de nuevo.');});},0):null;
    return () => { if(roomTimer!==null)window.clearTimeout(roomTimer);model.current = null; void session.dispose().catch(() => {}); };
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
  if (state === 'INPUT_READY') return <ReadyControl view={view} copyDiagnostic={copyDiagnostic} copyStatus={copyStatus} diagnostic={diagnostic}/>;
  const tabs=['control','help','data'];
  const names={control:'Control',help:'Ayuda',data:'Datos'};
  return <FixedScreen title="GORILLA ESCAPE" nav={tabs.map(tab=><button key={tab} aria-current={panel===tab?'page':undefined} onClick={()=>setPanel(tab)}>{names[tab]}</button>)}>
    {panel==='control' && <div className="fixed-control" data-feedback={feedback}>
      <img className="fixed-art" src="/brand/island-route.png" alt=""/>
      <div className="fixed-status" role="status" aria-live="polite"><h2>{title}</h2><p className="primary-description">{description}</p></div>
      <p className="fixed-safety">Sujeta el teléfono con ambas manos.</p>
      {state === 'NO_ADMISSION' && <form className="pin-form" onSubmit={async e => {
        e.preventDefault();
        if (pin.trim().length === 4) {
          setPinStatus('Conectando…');
          const res = await model.current.joinWithPin(pin);
          if (res !== 'JOINED' && res !== 'REDIRECTING') {
            setPinStatus(res === 'PIN_INVALID' ? 'Código no válido' : res === 'SESSION_FULL' ? 'Sala llena' : res === 'PIN_RATE_LIMITED' ? 'Demasiados intentos. Espera un minuto.' : res === 'ROOM_NOT_FOUND' ? 'La sala no está disponible.' : 'No se pudo conectar');
          }
        }
      }}>
        <label htmlFor="room-pin" className="pin-label">O ingresa el código de sala (4 caracteres):</label>
        <div className="pin-input-group">
          <input
            id="room-pin"
            type="text"
            className="pin-input"
            value={pin}
            maxLength={4}
            autoCapitalize="characters"
            autoComplete="off"
            autoCorrect="off"
            spellCheck="false"
            placeholder="ABCD"
            onChange={e => {
              setPin(e.target.value.toUpperCase().replace(/[^2-9A-HJKMNP-Z]/g, '').slice(0, 4));
              setPinStatus('');
            }}
            disabled={pinStatus === 'Conectando…'}
          />
          <button type="submit" className="primary pin-submit" disabled={pin.length !== 4 || pinStatus === 'Conectando…'}>
            {pinStatus === 'Conectando…' ? '…' : 'ENTRAR'}
          </button>
        </div>
        {pinStatus && pinStatus !== 'Conectando…' && <p role="alert" className="brief-status">{pinStatus}</p>}
      </form>}
      {(label || canRetryPermission) && <button className="primary" onClick={()=>{
        setDiagnostic(null);setCopyStatus('');
        if(state==='CONNECT')void model.current.connect();else void model.current.activate();
      }}>{label || 'REINTENTAR PERMISO'}</button>}
    </div>}
    {panel==='help' && <div className="fixed-section">
      <h2>Ayuda de conexión</h2>
      <PagedText text={`${description}\n\nMantén el juego abierto en la PC. Sujeta el teléfono con ambas manos y conserva esta pantalla visible. No lo lances ni lo agites.`}/>
      {view?.network==='NETWORK_READY' && <button onClick={()=>setPanel('profile')}>Tu nombre en la isla</button>}
      {state==='ERROR' && view?.network==='NOT_CONNECTED' && <button onClick={()=>setPanel('recovery')}>Revisar conexión</button>}
    </div>}
    {panel==='recovery' && <div className="fixed-section"><h2>Revisar conexión</h2><PagedText text="Comprueba la misma Wi-Fi, la confianza HTTPS y que el juego siga abierto. Si la PC responde, escanea un QR nuevo."/>
        <button disabled={view?.connectionCheck==='CHECKING'} onClick={()=>void model.current.checkConnection()}>COMPROBAR CONEXIÓN</button>
        <p role="status" className="brief-status">{view?.connectionCheck==='READY'?'La PC responde. Escanea un QR nuevo.':view?.connectionCheck==='CHECKING'?'Comprobando…':['UNREACHABLE','INSECURE'].includes(view?.connectionCheck)?'No se pudo verificar. Revisa la red y la confianza HTTPS.':''}</p>
        {!view?.publicTunnel && <a href="/prepare" target="_blank" rel="noreferrer">Preparar este teléfono</a>}
    </div>}
    {panel==='profile' && <div className="fixed-section">
      <h2>Tu nombre en la isla</h2>
      <label>Nombre del náufrago<input value={displayName} maxLength={40} autoComplete="off" onChange={e=>setDisplayName(e.target.value)} disabled={Boolean(profileStatus)}/></label>
      <button disabled={!displayName.trim() || Boolean(profileStatus)} onClick={async()=>{setProfileStatus('SAVING');setProfileStatus(await model.current.createProfile(displayName));setDisplayName('');}}>Guardar nombre</button>
      <p className="brief-status" role="status">{profileStatus==='SAVED'?'Nombre guardado.':profileStatus==='SAVING'?'Guardando…':profileStatus?'No se confirmó el guardado. Puedes jugar sin nombre.':''}</p>
    </div>}
    {panel==='data' && <div className="fixed-section">
      <h2>Datos y diagnóstico</h2>
      {diagnostic ? <PagedText label="Diagnóstico técnico" text={JSON.stringify(diagnostic,null,2)}/> : <PagedText text="Copia el diagnóstico para revisar la conexión y los sensores. Las mediciones técnicas no son necesarias para jugar."/>}
      <button onClick={()=>void copyDiagnostic()}>Copiar diagnóstico</button>
      {view?.network==='NETWORK_READY' && !diagnostic && <div className="measurement-actions">
        <button disabled={view?.measurement?.status==='MEASURING'} onClick={()=>model.current.startMeasurement('rest')}>Reposo · 15 s</button>
        <button disabled={view?.measurement?.status==='MEASURING'} onClick={()=>model.current.startMeasurement('gentle')}>Movimiento · 25 s</button>
      </div>}
      <p role="status" className="brief-status">{copyStatus || (view?.measurement?.status==='MEASURING'?'Medición en curso…':'')}</p>
    </div>}
  </FixedScreen>;
}
export default function App() {
  return new URLSearchParams(location.search).get('view') === 'health' ? <HealthProbe/> : <Controller/>;
}
