(() => {
  'use strict';
  const key = 'jungle.profile.v1';
  const $ = id => document.getElementById(id);
  const status = $('profileStatus');
  const defaults = { sensitivity: 1, vibration: true };
  const avatars = { gorilla: '🦍', monkey: '🐵', playful: '🐒' };
  const colors = { gold: '#ffd13d', turquoise: '#26e0cf', blue: '#7ccaff' };
  const games = {
    BananaBowling: ['Banana Bowling', 'puntos'], GorillaBoxing: ['Boxeo', 'puntos'],
    JungleBaseball: ['Jungle Baseball', 'puntos'], JungleDrums: ['Jungle Drums', 'puntos'],
    CoconutSlice: ['Coconut Slice', 'puntos'], WhackAMole: ['Whack-a-Mole', 'puntos'],
    CoconutThrow: ['Coconut Throw', 'm'], LogBalance: ['Log Balance', 'puntos'],
    JungleKeeper: ['Jungle Keeper', 'puntos']
  };
  let profile = null;
  let polling = false;
  function cleanName(value) {
    return typeof value === 'string' ? value.replace(/[<>\x00-\x1f\x7f]/g, '').trim().replace(/\s+/g, ' ').slice(0, 20) : '';
  }
  function validResult(item) {
    return item && typeof item.id === 'string' && item.id.length <= 64 &&
      Object.hasOwn(games, item.game) && Number.isFinite(item.score) &&
      Number.isSafeInteger(item.time) && item.time > 0 && item.time <= Date.now() + 60000;
  }
  function normalize(value) {
    if (value?.version !== 1 || !cleanName(value.name)) throw new Error('profile');
    return {
      version: 1, name: cleanName(value.name),
      avatar: Object.hasOwn(avatars, value.avatar) ? value.avatar : 'gorilla',
      color: Object.hasOwn(colors, value.color) ? value.color : 'gold',
      settings: {
        sensitivity: Number.isFinite(value.settings?.sensitivity) ? Math.min(1.6, Math.max(.6, value.settings.sensitivity)) : 1,
        vibration: value.settings?.vibration !== false
      },
      results: Array.isArray(value.results) ? value.results.filter(validResult).slice(-50) : []
    };
  }
  function persist() {
    try {
      localStorage.setItem(key, JSON.stringify(profile));
      return true;
    } catch {
      status.textContent = 'El navegador bloqueó el guardado. Exporta el perfil para conservarlo.';
      return false;
    }
  }
  function renderResults() {
    $('profileResults').replaceChildren();
    $('profileRecords').replaceChildren();
    const results = profile?.results || [];
    const best = {};
    for (const result of results) {
      if (!best[result.game] || best[result.game].score < result.score) best[result.game] = result;
    }
    for (const result of Object.values(best)) {
      const line = document.createElement('p');
      line.textContent = games[result.game][0] + ' · Mejor marca ' + result.score.toLocaleString('es-MX', {maximumFractionDigits: 1}) + ' ' + games[result.game][1];
      $('profileRecords').appendChild(line);
    }
    for (const result of [...results].reverse().slice(0, 10)) {
      const line = document.createElement('li');
      line.textContent = games[result.game][0] + ' · ' + result.score.toLocaleString('es-MX', {maximumFractionDigits: 1}) +
        ' ' + games[result.game][1] + ' · ' + new Date(result.time).toLocaleDateString('es-MX');
      $('profileResults').appendChild(line);
    }
    $('resultsStatus').textContent = results.length ? 'Se conservan hasta 50 resultados; se muestran los 10 más recientes.' : 'Todavía no hay partidas guardadas.';
  }
  function render() {
    $('profileSummary').textContent = profile ? avatars[profile.avatar] + ' ' + profile.name : 'Todavía no has creado un perfil.';
    $('profileName').value = profile?.name || '';
    $('profileAvatar').value = profile?.avatar || 'gorilla';
    $('profileColor').value = profile?.color || 'gold';
    $('pointerSensitivity').value = profile?.settings.sensitivity || 1;
    $('controlVibration').checked = profile?.settings.vibration !== false;
    $('sensitivityValue').textContent = Number($('pointerSensitivity').value).toFixed(1) + '×';
    $('profileSave').textContent = profile ? 'GUARDAR CAMBIOS' : 'CREAR PERFIL';
    $('profileDelete').hidden = $('profileExport').hidden = !profile;
    if (profile) $('playerName').value = profile.name;
    const mark = document.querySelector('.mark');
    mark.textContent = profile ? avatars[profile.avatar] : '🍌';
    mark.style.background = profile ? colors[profile.color] : colors.gold;
    renderResults();
  }
  function save(value) {
    const name = cleanName(value);
    if (!name) { status.textContent = 'Escribe un nombre de 1 a 20 caracteres.'; return false; }
    profile = normalize({
      ...profile, version: 1, name,
      avatar: $('profileAvatar').value,
      color: $('profileColor').value,
      settings: { sensitivity: Number($('pointerSensitivity').value), vibration: $('controlVibration').checked }
    });
    if (persist()) status.textContent = 'Perfil y preferencias guardados.';
    render();
    return true;
  }
  try {
    const saved = JSON.parse(localStorage.getItem(key) || 'null');
    if (saved) profile = normalize(saved);
  } catch { status.textContent = 'No se pudo leer el perfil. Puedes crearlo o importar tu archivo.'; }
  render();
  window.JungleProfile = { save, settings: () => profile?.settings || defaults };
  $('profileForm').addEventListener('submit', event => { event.preventDefault(); save($('profileName').value); });
  $('pointerSensitivity').addEventListener('input', () => {
    $('sensitivityValue').textContent = Number($('pointerSensitivity').value).toFixed(1) + '×';
    if (profile) save(profile.name);
    else status.textContent = 'Crea tu perfil para guardar los ajustes.';
  });
  $('controlVibration').addEventListener('change', () => {
    if (profile) save(profile.name);
    else status.textContent = 'Crea tu perfil para guardar los ajustes.';
  });
  $('profileRecenter').addEventListener('click', () => {
    if (!window.JungleControl?.player().connected) { status.textContent = 'Conecta el control antes de recentrar.'; return; }
    window.JungleControl.recenter();
    status.textContent = 'Puntero recentrado.';
  });
  $('profileDelete').addEventListener('click', () => {
    if (!window.confirm('¿Borrar perfil, preferencias y resultados locales? El control se desconectará.')) return;
    try { localStorage.removeItem(key); sessionStorage.removeItem('jungleName'); }
    catch { status.textContent = 'El navegador impidió borrar los datos. Prueba desde los ajustes del sitio.'; return; }
    profile = null;
    $('playerName').value = '';
    render();
    window.dispatchEvent(new Event('jungle-profile-deleted'));
    status.textContent = 'Datos locales eliminados. Los archivos exportados se borran desde tu celular.';
  });
  $('profileExport').addEventListener('click', () => {
    if (!profile) return;
    const url = URL.createObjectURL(new Blob([JSON.stringify(profile, null, 2)], { type: 'application/json' }));
    const link = document.createElement('a');
    link.href = url; link.download = 'jungle-perfil.json'; link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    status.textContent = 'Guarda el archivo para recuperar perfil, ajustes y resultados en otro enlace.';
  });
  $('profileImport').addEventListener('change', async event => {
    const file = event.target.files?.[0];
    if (!file) return;
    try {
      if (file.size > 65536) throw new Error('size');
      const imported = normalize(JSON.parse(await file.text()));
      if (profile && !window.confirm('¿Reemplazar el perfil actual con el del archivo?')) return;
      profile = imported;
      if (persist()) status.textContent = 'Perfil, ajustes y resultados importados.';
      render();
    } catch { status.textContent = 'Archivo inválido. Elige un perfil JSON exportado desde Jungle Party.'; }
    finally { event.target.value = ''; }
  });
  async function pollResults() {
    const player = window.JungleControl?.player();
    if (!profile || !player?.connected || polling || document.hidden) return;
    const currentProfile = profile;
    polling = true;
    try {
      const response = await fetch('/results?room=' + encodeURIComponent(player.room) + '&player=' + encodeURIComponent(player.player), {
        cache: 'no-store', signal: AbortSignal.timeout(3000)
      });
      if (!response.ok) throw new Error('results');
      const results = await response.json();
      if (profile !== currentProfile || !Array.isArray(results)) return;
      const ids = new Set(profile.results.map(item => item.id));
      const fresh = results.filter(item => validResult(item) && !ids.has(item.id));
      if (fresh.length) {
        profile.results = [...profile.results, ...fresh].sort((a, b) => a.time - b.time).slice(-50);
        persist();
        renderResults();
      }
    } catch { $('resultsStatus').textContent = 'Los resultados guardados siguen disponibles. Esperando conexión con el juego.'; }
    finally { polling = false; }
  }
  setInterval(pollResults, 2500);
  if ('serviceWorker' in navigator && window.isSecureContext) {
    navigator.serviceWorker.register('/sw.js').catch(() => {
      status.textContent = 'El perfil funciona. No se pudo activar la instalación y el modo sin conexión.';
    });
  }
  let installPrompt;
  window.addEventListener('beforeinstallprompt', event => {
    event.preventDefault(); installPrompt = event; $('installPwa').hidden = false;
  });
  $('installPwa').addEventListener('click', async () => {
    if (!installPrompt) return;
    await installPrompt.prompt(); await installPrompt.userChoice;
    installPrompt = null; $('installPwa').hidden = true;
  });
  window.addEventListener('appinstalled', () => { $('installPwa').hidden = true; });
})();
