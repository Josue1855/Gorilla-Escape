import { gamePresentation, gameStats } from './gamePresentation.js';
export default function ReadyControl({ view, copyDiagnostic, copyStatus, diagnostic, actions }) {
  const game=gamePresentation(view.gameContext,view.playerId),stats=gameStats(view.gameContext);
  return <main className="controller-shell" data-game={game.game}>
    <header className="brand"><div><p className="eyebrow">GORILLA ESCAPE · CONTROL CONECTADO</p><h1>{game.title}</h1></div></header>
    <section className="connection-panel" aria-labelledby="game-heading">
      <div role="status" aria-live="polite"><h2 id="game-heading">{game.heading}</h2><p>{game.instruction}</p></div>
      {stats && <section aria-label="Estado de la partida">
        {stats.score!==undefined&&<h2>Puntos: {stats.score}</h2>}
        {stats.progress!==undefined&&<><p>Progreso: {Math.round(stats.progress*100)} %</p><progress max="1" value={stats.progress} aria-label="Progreso del juego"/></>}
        {stats.balance!==undefined&&<><p>Equilibrio: {Math.round(stats.balance*100)}</p><meter min="-1" max="1" value={stats.balance} aria-label="Equilibrio en la PC"/></>}
        {stats.elapsed!==undefined&&<p>Tiempo: {stats.elapsed.toFixed(1)} s</p>}
        {stats.distance!==undefined&&<p>Recorrido: {stats.distance.toFixed(1)} m</p>}
      </section>}
      <div aria-label="Controles del juego">{actions}</div>
      <aside className="safety"><strong>Sujeta firmemente tu teléfono.</strong><span>Mantén visibles las manos y mira la PC.</span></aside>
    </section>
    <details><summary>Ayuda y conexión</summary><button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button><p role="status">{copyStatus}</p>{diagnostic&&<pre tabIndex="0">{JSON.stringify(diagnostic,null,2)}</pre>}</details>
  </main>;
}
