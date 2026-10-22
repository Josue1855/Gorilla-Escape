import { gamePresentation, gameStats } from './gamePresentation.js';
export default function ReadyControl({ view, copyDiagnostic, copyStatus, diagnostic }) {
  const game = gamePresentation(view.gameContext, view.playerId);
  const stats = gameStats(view.gameContext);
  return <main className="island-controller" data-game={game.game} data-phase={game.phase || "Ready"}>
    <header className="control-top"><span className="connected-dot"/> CONTROL CONECTADO <span>GORILLA ESCAPE</span></header>
    <p className="eyebrow">LA ISLA TE PONE A PRUEBA</p>
    <h1 style={{ whiteSpace: 'pre-line' }}>{game.title}</h1>
    <p className="control-instruction" role="status" aria-live="polite"><strong>{game.heading}</strong><br/>{game.instruction}</p>
    <div className="river-control" aria-hidden="true">
      <div className="river-bank bank-left"/><div className="river-bank bank-right"/>
      <div className="river-streak streak-one"/><div className="river-streak streak-two"/>
      <div className="control-log"/>
      <div className="castaway-control" style={{ transform: `translateX(${(stats?.balance ?? view.controlTilt ?? 0) * 85}px) rotate(${(stats?.balance ?? view.controlTilt ?? 0) * 16}deg)` }}>
        <span className="castaway-head"/><span className="castaway-body"/><span className="castaway-arms"/>
      </div>
      <div className="balance-track"><span style={{ left: `${50 + (stats?.balance ?? view.controlTilt ?? 0) * 42}%` }}/></div>
    </div>
    {stats && <section className="game-score" aria-label="Estado real de la partida">
      <div className="score-labels"><span>← IZQUIERDA</span><span>CENTRO</span><span>DERECHA →</span></div>
      <div className="score-balance" aria-label={`Equilibrio ${Math.round(stats.balance*100)}`}>
        <i className="score-center"/><i className="score-tendency" style={{left:`${50+stats.tendency*48}%`}}/>
        <i className="score-support" style={{left:`${50+stats.balance*48}%`}}/>
      </div>
      <small>Blanco: apoyo · Ámbar: tendencia</small>
      <h2>Recorrido {Math.round(stats.progress*100)} %</h2>
      <progress max="1" value={stats.progress} aria-label="Avance del cruce"/>
      <p>{stats.distance.toFixed(1)} m · {stats.elapsed.toFixed(1)} s<br/>
      Giro {Math.round(stats.roll)}° · Paso {stats.speed.toFixed(2)} m/s · Viento {stats.wind>=0?'→':'←'}</p>
    </section>}
    <p className="tilt-instruction"><span>←</span> INCLINA CON SUAVIDAD <span>→</span></p>
    <p className="control-caption">{stats ? "El panel muestra el estado real de tu partida." : "Tu inclinación se muestra aquí. Esperando el estado de la PC…"}</p>
    <details className="control-help"><summary>Ayuda y conexión</summary>
      <p>Mantén este navegador visible y la misma Wi-Fi. Sujeta el teléfono; no lo lances ni lo agites.</p>
      <button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button>
      <p role="status">{copyStatus}</p>
      {diagnostic && <pre tabIndex="0">{JSON.stringify(diagnostic, null, 2)}</pre>}
    </details>
  </main>;
}
