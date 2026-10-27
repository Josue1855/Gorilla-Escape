import { useEffect, useRef, useState } from 'react';
import { gamePresentation, gameStats, BalanceCues } from './gamePresentation.js';
import {FixedScreen,PagedText} from './FixedScreen.jsx';

export default function ReadyControl({ view, copyDiagnostic, copyStatus, diagnostic }) {
  const game = gamePresentation(view.gameContext, view.playerId);
  const stats = gameStats(view.gameContext);
  const [panel,setPanel]=useState('control');
  const cues=useRef(new BalanceCues());
  const [vibration,setVibration]=useState(true);
  const danger=stats?.danger ?? 0;
  useEffect(()=>{
    const cue=cues.current.update(game.phase,danger,performance.now(),view.gameContext?.playerId===view.playerId);
    if(vibration && cue && typeof navigator.vibrate==='function' && document.visibilityState!=='hidden') {
      navigator.vibrate(cue==='Fell'?[90,80,140]:cue==='Won'?[40,60,40]:cue==='critical'?55:25);
    }
  },[game.phase,game.active,danger,stats,vibration]);
  const details=stats?`Recorrido ${Math.round(stats.progress*100)} %\nDistancia ${stats.distance.toFixed(1)} m\nTiempo ${stats.elapsed.toFixed(1)} s\nGiro ${Math.round(stats.roll)}°\nPaso ${stats.speed.toFixed(2)} m/s\nViento ${stats.wind>=0?'→':'←'}`:'Esperando los datos de Unity.';
  return <FixedScreen title={game.title.replace('\n',' ')} nav={['control','details','help'].map(tab=><button key={tab} aria-current={panel===tab?'page':undefined} onClick={()=>setPanel(tab)}>{tab==='control'?'Mando':tab==='details'?'Datos':'Ayuda'}</button>)}>
    {panel==='control' && <div className="fixed-control" data-phase={game.phase}>
      <div className="fixed-status" role="status" aria-live="polite"><h2>{game.heading}</h2></div>
      {!stats && <img className="fixed-art" src="/brand/island-route.png" alt=""/>}
      <section className={`compact-score ${game.active && danger>=.6?'score-warning':''}`} aria-label="Equilibrio y progreso">
        <div className="score-labels"><span>← IZQUIERDA</span><span>CENTRO</span><span>DERECHA →</span></div>
        <div className="score-balance" aria-label={stats?`Equilibrio ${Math.round(stats.balance*100)}`:'Inclinación local'}>
          <i className="score-center"/><i className="score-tendency" style={{left:`${50+(stats?.tendency ?? 0)*48}%`}}/>
          <i className="score-support" style={{left:`${50+(stats?.balance ?? view.controlTilt ?? 0)*48}%`}}/>
        </div>
        {stats && <><h3>Recorrido {Math.round(stats.progress*100)} %</h3><progress max="1" value={stats.progress} aria-label="Avance del cruce"/></>}
      </section>
      <p className="fixed-correction" role="status">{game.active && danger>=.6?(stats.balance>0?'← Corrige a la izquierda':'Corrige a la derecha →'):'Inclina con suavidad'}</p>
    </div>}
    {panel==='details' && <div className="fixed-section"><h2>Datos del cruce</h2><PagedText text={`${details}\n\nBlanco: apoyo actual. Ámbar: tendencia. Los datos proceden de Unity; sin telemetría se muestra sólo la inclinación local.`}/>      {typeof navigator.vibrate==='function' && <label className="vibration-option"><input type="checkbox" checked={vibration} onChange={e=>setVibration(e.target.checked)}/> Vibración de equilibrio</label>}
</div>}
    {panel==='help' && <div className="fixed-section"><h2>Ayuda del mando</h2>
      <PagedText label={diagnostic?'Diagnóstico técnico':'Ayuda'} text={diagnostic?JSON.stringify(diagnostic,null,2):`${game.instruction}\n\nMantén este navegador visible. Sujeta el teléfono con ambas manos; no lo lances ni lo agites.`}/>
      <button onClick={copyDiagnostic}>Copiar diagnóstico</button><p className="brief-status" role="status">{copyStatus}</p>
    </div>}
  </FixedScreen>;
}
