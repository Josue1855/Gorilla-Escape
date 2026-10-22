const phases = {
  Ready: ['Prepara tu equilibrio', 'Sujeta el teléfono cómodamente. El control se prepara automáticamente.'],
  Countdown: ['La prueba comienza', 'Sostén el teléfono vertical y mira la PC.'],
  Crossing: ['Cruza el río', 'Inclina suavemente para corregir tu equilibrio.'],
  Paused: ['La isla espera', 'La partida está en pausa. Reanuda desde la PC.'],
  Won: ['¡Prueba superada!', 'Los gorilas reconocen tu victoria. Continúa en la PC.'],
  Fell: ['El río ganó esta vez', 'Recupera una posición cómoda e inténtalo de nuevo.'],
};
export function gamePresentation(context, playerId) {
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
  if (context?.gameId !== 'log-balance' || !context.stats) return null;
  const bounds = { balance: [-1,1], tendency: [-1,1], progress: [0,1], distance: [0,10000],
    elapsed: [0,3600], roll: [-3600,3600], speed: [0,100], wind: [-100,100] };
  for (const [key,[min,max]] of Object.entries(bounds)) {
    const value = context.stats[key];
    if (!Number.isFinite(value) || value < min || value > max) return null;
  }
  return context.stats;
}
