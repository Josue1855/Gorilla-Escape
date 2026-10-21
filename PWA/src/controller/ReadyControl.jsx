import { gamePresentation } from './gamePresentation.js';
export default function ReadyControl({ view, copyDiagnostic, copyStatus, diagnostic }) {
  const game = gamePresentation(view.gameContext, view.playerId);
  return <main className="island-controller" data-game={game.game} data-phase={game.phase || "Ready"}>
    <header className="control-top"><span className="connected-dot"/> CONTROL CONECTADO <span>GORILLA ESCAPE</span></header>
    <p className="eyebrow">LA ISLA TE PONE A PRUEBA</p>
    <h1 style={{ whiteSpace: 'pre-line' }}>{game.title}</h1>
    <p className="control-instruction" role="status" aria-live="polite"><strong>{game.heading}</strong><br/>{game.instruction}</p>
    <div className="river-control" aria-hidden="true">
      <div className="river-bank bank-left"/><div className="river-bank bank-right"/>
      <div className="river-streak streak-one"/><div className="river-streak streak-two"/>
      <div className="control-log"/>
      <div className="castaway-control" style={{ transform: `translateX(${(view.controlTilt || 0) * 85}px) rotate(${(view.controlTilt || 0) * 16}deg)` }}>
        <span className="castaway-head"/><span className="castaway-body"/><span className="castaway-arms"/>
      </div>
      <div className="balance-track"><span style={{ left: `${50 + (view.controlTilt || 0) * 42}%` }}/></div>
    </div>
    <p className="tilt-instruction"><span>←</span> INCLINA CON SUAVIDAD <span>→</span></p>
    <p className="control-caption">Tu inclinación se muestra aquí.<br/>El equilibrio y el resultado se ven en la PC.</p>
    <details className="control-help"><summary>Ayuda y conexión</summary>
      <p>Mantén este navegador visible y la misma Wi-Fi. Sujeta el teléfono; no lo lances ni lo agites.</p>
      <button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button>
      <p role="status">{copyStatus}</p>
      {diagnostic && <pre tabIndex="0">{JSON.stringify(diagnostic, null, 2)}</pre>}
    </details>
  </main>;
}
