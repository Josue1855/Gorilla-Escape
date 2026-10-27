export const games = {
  bowling: ['BANANA BOWLING','Apunta con el teléfono y realiza un lanzamiento suave.'],
  boxing: ['BOXEO CON GORILA','Mantén ambas manos visibles y realiza los golpes frente a la cámara.'],
  baseball: ['JUNGLE BASEBALL','Espera la pelota y realiza el movimiento de bateo.'],
  drums: ['JUNGLE DRUMS','Sigue el ritmo con ambas manos visibles.'],
  'coconut-slice': ['COCONUT SLICE','Corta con tus manos y evita los obstáculos.'],
  whack: ['GORILLA WHACK','Apunta con la mano y realiza un golpe hacia abajo.'],
  'log-balance': ['DESAFÍO DEL TRONCO','Inclina suavemente el teléfono y frena la inercia.'],
};
const displayPhases = { Ready:'Prepárate', Countdown:'La prueba comienza', Playing:'¡A jugar!', Paused:'Juego en pausa', Won:'¡Prueba superada!', Lost:'Inténtalo de nuevo', Finished:'Prueba terminada' };
function validV2(context) {
  return context?.version === 2 && Object.hasOwn(games,context.gameId) && Object.hasOwn(displayPhases,context.phase) && Number.isInteger(context.playerId) && context.playerId>=1 && context.playerId<=4;
}
const phases = {
  Ready: ['Prepara tu equilibrio', 'Sujeta el teléfono cómodamente. El control se prepara automáticamente.'],
  Countdown: ['La prueba comienza', 'Sostén el teléfono vertical y mira la PC.'],
  Crossing: ['Cruza el río', 'Inclina suavemente para corregir tu equilibrio.'],
  Paused: ['La isla espera', 'La partida está en pausa. Reanuda desde la PC.'],
  Won: ['¡Prueba superada!', 'Los gorilas reconocen tu victoria. Continúa en la PC.'],
  Fell: ['El río ganó esta vez', 'Recupera una posición cómoda e inténtalo de nuevo.'],
};
export function gamePresentation(context, playerId) {
  if(context?.version===2){
    if(!validV2(context))return {game:'lobby',title:'GORILLA ESCAPE',heading:'Control preparado',instruction:'Elige una prueba en la PC.',active:false};
    const mine=context.playerId===playerId;
    return {game:context.gameId,phase:context.phase,title:games[context.gameId][0],heading:mine?displayPhases[context.phase]:'Espera tu turno',
      instruction:mine?(context.phase==='Paused'?'Revisa la PC, la cámara y la conexión del teléfono.':games[context.gameId][1]):'Otro náufrago está jugando. Mira la PC.',active:mine&&context.phase==='Playing'};
  }
  if (context?.version !== 1 || context.gameId !== 'log-balance' || !phases[context.phase]
      || !Number.isInteger(context.playerId) || context.playerId < 1 || context.playerId > 4) {
    return { game: 'lobby', title: 'LA ISLA\nTE ESPERA', heading: 'Control preparado', instruction: 'Elige una prueba en la PC. Tu teléfono cambiará con el juego.', active: false };
  }
  const [heading, instruction] = phases[context.phase];
  const spectator = context.playerId !== playerId;
  return { game: 'log-balance', title: 'DESAFÍO\nDEL TRONCO', heading: spectator ? 'Observa la prueba' : heading,
    instruction: spectator ? 'Otro náufrago está jugando. Espera tu turno en la PC.' : instruction,
    active: !spectator && context.phase === 'Crossing', phase: context.phase };
}

export function gameStats(context) {
  if(context?.version===2){
    const stats=context.stats;if(!validV2(context)||!stats||Object.keys(stats).length!==10)return null;
    const bounds={score:[0,1e9],progress:[0,1],elapsed:[0,3600],balance:[-1,1],distance:[0,10000]};const values={};
    for(const [key,[min,max]] of Object.entries(bounds)){
      const flag='has'+key[0].toUpperCase()+key.slice(1);
      if(typeof stats[flag]!=='boolean'||!Number.isFinite(stats[key])||stats[key]<min||stats[key]>max)return null;
      if(stats[flag])values[key]=stats[key];
    }
    return values;
  }
  if (context?.gameId !== 'log-balance' || !context.stats) return null;
  const bounds = { balance: [-1,1], tendency: [-1,1], progress: [0,1], distance: [0,10000],
    elapsed: [0,3600], roll: [-3600,3600], speed: [0,100], wind: [-100,100] };
  for (const [key,[min,max]] of Object.entries(bounds)) {
    const value = context.stats[key];
    if (!Number.isFinite(value) || value < min || value > max) return null;
  }
  return context.stats;
}
