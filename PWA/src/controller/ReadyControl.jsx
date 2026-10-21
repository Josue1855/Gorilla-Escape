export default function ReadyControl({ view, copyDiagnostic, copyStatus, diagnostic }) {
  return <main className="island-controller">
    <header className="control-top"><span className="connected-dot"/> CONTROL CONECTADO <span>GORILLA ESCAPE</span></header>
    <p className="eyebrow">LA ISLA TE PONE A PRUEBA</p>
    <h1>DESAFÍO<br/>DEL TRONCO</h1>
    <p className="control-instruction">Mira la PC para calibrar y comenzar.<br/>Sostén el teléfono vertical y con ambas manos.</p>
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
      <p>Mantén Safari visible y la misma Wi-Fi. Sujeta el teléfono; no lo lances ni lo agites.</p>
      <button className="secondary" onClick={copyDiagnostic}>Copiar diagnóstico</button>
      <p role="status">{copyStatus}</p>
      {diagnostic && <pre tabIndex="0">{JSON.stringify(diagnostic, null, 2)}</pre>}
    </details>
  </main>;
}
