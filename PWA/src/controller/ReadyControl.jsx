import { useState } from 'react';
import { controlProfile, games, gamePresentation, gameStats } from './gamePresentation.js';

export default function ReadyControl({ view, copyDiagnostic, copyStatus, diagnostic, actions, onCommand, onActivate, playerName, onSetName, onForfeit, onToggleAway }) {
  const game=gamePresentation(view.gameContext,view.playerId),stats=gameStats(view.gameContext);
  const profile=controlProfile(view.gameContext,view.playerId);
  const balance=game.game==='log-balance',cameraOnly=profile.valid&&profile.screen==='game'&&!profile.phoneRequired;
  const ready=!view.isAway&&view.state==='INPUT_READY';
  const command=(action,target='')=>void onCommand?.(action,target);
  const [editingName, setEditingName] = useState(false);
  const [nameInput, setNameInput] = useState(playerName || '');
  const [isSavingName, setIsSavingName] = useState(false);
  const [nameFeedback, setNameFeedback] = useState('');

  return <main className="controller-shell" data-game={game.game} data-phase={game.phase || 'Ready'}>
    <header className="brand"><div><p className="eyebrow">GORILLA ESCAPE · {view.playerId ? `JUGADOR ${view.playerId}` : 'CONTROL CONECTADO'}</p><h1>{game.title}</h1></div></header>
    <div className="player-identity-bar">
      <span className="player-badge">👤 {playerName || `Jugador ${view.playerId || 1}`}</span>
      <div className="identity-actions">
        <button type="button" className="edit-name-pill" onClick={() => { setEditingName(!editingName); setNameInput(playerName || ''); setNameFeedback(''); }}>
          {editingName ? 'Cerrar' : 'Editar nombre'}
        </button>
        {profile.canNavigate && profile.screen === 'game' && (
          <button type="button" className="pause-pill-btn" disabled={view.commandPending} onClick={() => command(game.phase === 'Paused' ? 'resume' : 'pause')}>
            {game.phase === 'Paused' ? '▶️ Reanudar' : '⏸️ Pausa'}
          </button>
        )}
      </div>
    </div>
    {editingName && <form className="inline-name-edit" onSubmit={async e => {
      e.preventDefault();
      const trimmed = (nameInput || '').normalize('NFC').trim();
      if (!trimmed) return;
      setIsSavingName(true);
      const res = await onSetName?.(trimmed);
      setIsSavingName(false);
      if (res === 'SAVED') {
        setNameFeedback('✓ Guardado en la PC');
        setEditingName(false);
      } else {
        setNameFeedback(res === 'INVALID' ? 'Usa de 1 a 40 letras, números o signos simples.' : 'La PC no confirmó el nombre. Comprueba la conexión y vuelve a intentarlo.');
      }
    }}>
      <input aria-label="Tu nombre en la partida" value={nameInput} maxLength={40} placeholder="Tu nombre" onChange={e => setNameInput(e.target.value)} disabled={isSavingName} autoFocus />
      <button className="primary" type="submit" disabled={!nameInput.trim() || isSavingName}>
        {isSavingName ? '...' : 'Guardar'}
      </button>
      {nameFeedback && <p role="status" className="name-feedback">{nameFeedback}</p>}
    </form>}
    {view.isAway && <aside className="camera-only" role="status"><strong>ESTÁS AUSENTE</strong><p>Tu control no envía movimientos. Pulsa «VOLVER A LA PARTIDA» cuando estés listo.</p></aside>}
    <section className="connection-panel" aria-labelledby="game-heading">
      <div role="status" aria-live="polite"><h2 id="game-heading">{game.heading}</h2><p>{game.instruction}</p></div>
      {profile.screen === 'game' && game.phase === 'Paused' && (
        <aside className="mobile-pause-overlay-card" role="status" aria-label="Juego en pausa">
          <h3>⏸️ JUEGO EN PAUSA</h3>
          <p>La partida está detenida. Revisa en la PC si falta cámara o sensores. Podrás reanudar cuando los dispositivos estén listos.</p>
          {profile.canNavigate && <div className="control-actions">
            <button className="primary" disabled={view.commandPending} onClick={() => command('resume')}>▶️ REANUDAR</button>
            <button className="secondary" disabled={view.commandPending} onClick={() => command('restart')}>🔄 REINICIAR</button>
            <button className="secondary" disabled={view.commandPending} onClick={() => command('menu')}>🏠 VOLVER AL MENÚ</button>
          </div>}
        </aside>
      )}
      {Boolean(view.playerId && view.gameContext?.playerId && view.playerId !== view.gameContext.playerId) && (
        <aside className="waiting-turn-card" aria-label="Turno de juego">
          <strong>TURNO ACTUAL: JUGADOR {view.gameContext.playerId}</strong>
          <p>Prueba en curso: {game.title} · Tu turno: Siguiente</p>
        </aside>
      )}
      {profile.valid&&profile.game==='lobby'&&<>
        {profile.canNavigate&&<nav className="game-menu" aria-label="Elegir juego en la PC">{Object.entries(games).map(([id,[title]])=><button key={id} className="game-card" disabled={view.commandPending} onClick={()=>command('select-game',id)}><strong>{title}</strong><span>{id==='log-balance'?'Solo celular':['coconut-slice','jungle-keeper'].includes(id)?'Solo cámara':'Celular + cámara'}</span></button>)}</nav>}
        <p className="control-hint">{profile.screen === 'connection' ? 'Comprueba en la PC que la cámara y los controles estén listos antes de comenzar.' : 'El menú funciona sin activar sensores. Puedes elegir el próximo desafío desde aquí.'}</p>
      </>}
      {profile.canNavigate&&['lobby','bowling'].includes(game.game)&&<fieldset className="hand-choice"><legend>Mano con la que lanzas</legend>{['right','left'].map(hand=><button key={hand} className="secondary" aria-pressed={profile.hand===hand} disabled={view.commandPending} onClick={()=>command('set-hand',hand)}>{hand==='right'?'Derecha':'Izquierda'}</button>)}</fieldset>}
      {balance&&<section className="balance-control" aria-label="Control del equilibrio">
        <p>Inclinación de tu teléfono</p><div className="tilt-meter"><div className="tilt-track"><span style={{left:`${50+(ready?Math.max(-1,Math.min(1,view.controlTilt||0)):0)*42}%`}}/></div><div className="score-labels"><span>Izquierda</span><span>Centro</span><span>Derecha</span></div></div>
        {!ready&&<p role="status">Mantén el teléfono vertical y quieto para preparar el control.</p>}
      </section>}
      {cameraOnly&&<aside className="camera-only"><strong>Juega con las manos libres</strong><p>Puedes bloquear el celular y dejarlo a un lado. La cámara de la PC controla esta prueba. Al desbloquear, recuperaremos tu sesión automáticamente.</p></aside>}
      {!view.isAway&&profile.phoneRequired&&!ready&&<div role="status" className="sensor-recovery"><p>{view.state==='PERMISSION_DENIED'?'Permite el movimiento para usar este control.':'Esperando sensores recientes del teléfono.'}</p><button className="primary" onClick={onActivate}>ACTIVAR / REVISAR SENSORES</button></div>}
      {stats&&<section className="game-score" aria-label="Estado de la partida">
        {stats.score!==undefined&&<h2>{game.game==='bowling'?'Pinos':'Puntos'}: {stats.score}</h2>}
        {stats.progress!==undefined&&<><p>{balance?'Avance del recorrido':'Progreso'}: {Math.round(stats.progress*100)} %</p><progress max="1" value={stats.progress} aria-label={balance?'Avance del recorrido':'Progreso del juego'}/></>}
        {balance&&stats.balance!==undefined&&<><p>Equilibrio real en la PC</p><meter min="-1" max="1" value={stats.balance} aria-label="Equilibrio en la PC"/></>}
        {stats.elapsed!==undefined&&<p>Tiempo: {stats.elapsed.toFixed(1)} s</p>}
        {stats.distance!==undefined&&<p>Recorrido: {stats.distance.toFixed(1)} m</p>}
      </section>}
      {ready&&['bowling','baseball'].includes(game.game)&&<div className="control-actions">{actions}</div>}
      {profile.canNavigate&&game.phase==='Ready'&&['log-balance','coconut-slice','jungle-keeper','whack'].includes(game.game)&&<button className="primary" disabled={view.commandPending||(profile.phoneRequired&&!ready)} onClick={()=>command('start')}>COMENZAR PRUEBA</button>}
      {profile.canNavigate&&profile.screen==='game'&&<div className="control-actions"><button className="secondary" disabled={view.commandPending} onClick={()=>command('menu')}>VOLVER AL MENÚ</button><button className="secondary" disabled={view.commandPending} onClick={()=>command('restart')}>NUEVA PARTIDA</button></div>}
      <div className="control-actions" style={{display:'flex',gap:'8px',justifyContent:'center',marginTop:'12px',flexWrap:'wrap'}}>
        <button className="secondary" type="button" onClick={onToggleAway}>{view.isAway?'▶️ VOLVER A LA PARTIDA':'⏸️ ESTARÉ AUSENTE'}</button>
        <button className="secondary" type="button" style={{color:'#ff6b6b'}} onClick={()=>{if(window.confirm('¿Seguro que deseas salir de la partida? Dejarás de controlar esta partida. La PC conservará los resultados ya confirmados.'))onForfeit?.();}}>🚪 SALIR DE LA PARTIDA</button>
      </div>
      <p role="status">{view.commandStatus}</p>
      {profile.phoneRequired&&<aside className="safety"><strong>Sujeta firmemente tu teléfono.</strong><span>Mueve la mano con suavidad y mira la PC.</span></aside>}
    </section>
    <details><summary>Ayuda y conexión</summary><button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button><p role="status">{copyStatus}</p>{diagnostic&&<pre tabIndex="0">{JSON.stringify(diagnostic,null,2)}</pre>}</details>
  </main>;
}
