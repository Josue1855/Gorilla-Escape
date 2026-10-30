import { controlProfile, games, gamePresentation, gameStats } from './gamePresentation.js';

export default function ReadyControl({ view, copyDiagnostic, copyStatus, diagnostic, actions, onCommand, onActivate }) {
  const game=gamePresentation(view.gameContext,view.playerId),stats=gameStats(view.gameContext);
  const profile=controlProfile(view.gameContext,view.playerId);
  const balance=game.game==='log-balance',cameraOnly=profile.valid&&profile.screen==='game'&&!profile.phoneRequired;
  const ready=view.state==='INPUT_READY';
  const command=(action,target='')=>void onCommand?.(action,target);
  return <main className="controller-shell" data-game={game.game} data-phase={game.phase || 'Ready'}>
    <header className="brand"><div><p className="eyebrow">GORILLA ESCAPE · {view.playerId ? `JUGADOR ${view.playerId}` : 'CONTROL CONECTADO'}</p><h1>{game.title}</h1></div></header>
    <section className="connection-panel" aria-labelledby="game-heading">
      <div role="status" aria-live="polite"><h2 id="game-heading">{game.heading}</h2><p>{game.instruction}</p></div>
      {profile.valid&&profile.game==='lobby'&&<>
        {profile.canNavigate&&<nav className="game-menu" aria-label="Elegir juego en la PC">{Object.entries(games).map(([id,[title]])=><button key={id} className="game-card" disabled={view.commandPending} onClick={()=>command('select-game',id)}><strong>{title}</strong><span>{id==='log-balance'?'Solo celular':['coconut-slice','jungle-keeper'].includes(id)?'Solo cámara':'Celular + cámara'}</span></button>)}</nav>}
        <p className="control-hint">El menú funciona sin activar sensores. Puedes elegir el próximo desafío desde aquí.</p>
      </>}
      {profile.canNavigate&&['lobby','bowling'].includes(game.game)&&<fieldset className="hand-choice"><legend>Mano con la que lanzas</legend>{['right','left'].map(hand=><button key={hand} className="secondary" aria-pressed={profile.hand===hand} disabled={view.commandPending} onClick={()=>command('set-hand',hand)}>{hand==='right'?'Derecha':'Izquierda'}</button>)}</fieldset>}
      {balance&&<section className="balance-control" aria-label="Control del equilibrio">
        <p>Inclinación de tu teléfono</p><div className="tilt-meter"><div className="tilt-track"><span style={{left:`${50+(ready?Math.max(-1,Math.min(1,view.controlTilt||0)):0)*42}%`}}/></div><div className="score-labels"><span>Izquierda</span><span>Centro</span><span>Derecha</span></div></div>
        {!ready&&<p role="status">Mantén el teléfono vertical y quieto para preparar el control.</p>}
      </section>}
      {cameraOnly&&<aside className="camera-only"><strong>Juega con las manos libres</strong><p>Puedes bloquear el celular y dejarlo a un lado. La cámara de la PC controla esta prueba. Al desbloquear, recuperaremos tu sesión automáticamente.</p></aside>}
      {profile.phoneRequired&&!ready&&<div role="status" className="sensor-recovery"><p>{view.state==='PERMISSION_DENIED'?'Permite el movimiento para usar este control.':'Esperando sensores recientes del teléfono.'}</p><button className="primary" onClick={onActivate}>ACTIVAR / REVISAR SENSORES</button></div>}
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
      <p role="status">{view.commandStatus}</p>
      {profile.phoneRequired&&<aside className="safety"><strong>Sujeta firmemente tu teléfono.</strong><span>Mueve la mano con suavidad y mira la PC.</span></aside>}
    </section>
    <details><summary>Ayuda y conexión</summary><button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button><p role="status">{copyStatus}</p>{diagnostic&&<pre tabIndex="0">{JSON.stringify(diagnostic,null,2)}</pre>}</details>
  </main>;
}
