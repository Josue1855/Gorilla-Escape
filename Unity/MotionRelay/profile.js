(() => {
  'use strict';
  const key = 'jungle.profile.v1';
  const $ = id => document.getElementById(id);
  const status = $('profileStatus');
  let profile = null;
  let storageAvailable = true;

  function cleanName(value) {
    return typeof value === 'string'
      ? value.replace(/[<>\x00-\x1f\x7f]/g, '').trim().replace(/\s+/g, ' ').slice(0, 20)
      : '';
  }
  function render() {
    $('profileSummary').textContent = profile ? 'Tu perfil · ' + profile.name : 'Todavía no has creado un perfil.';
    $('profileName').value = profile?.name || '';
    $('profileSave').textContent = profile ? 'GUARDAR CAMBIOS' : 'CREAR PERFIL';
    $('profileDelete').hidden = !profile;
    $('profileExport').hidden = !profile;
    if (profile) $('playerName').value = profile.name;
  }
  function save(value) {
    const name = cleanName(value);
    if (!name) { status.textContent = 'Escribe un nombre de 1 a 20 caracteres.'; return false; }
    const next = { version: 1, name };
    try {
      localStorage.setItem(key, JSON.stringify(next));
      storageAvailable = true;
    } catch {
      storageAvailable = false;
    }
    profile = next;
    render();
    status.textContent = storageAvailable
      ? 'Perfil guardado en este navegador.'
      : 'El navegador bloqueó el guardado. Puedes jugar y exportar el perfil para conservarlo.';
    return true;
  }
  try {
    const saved = JSON.parse(localStorage.getItem(key) || 'null');
    if (saved?.version === 1 && cleanName(saved.name)) profile = { version: 1, name: cleanName(saved.name) };
  } catch {
    status.textContent = 'No se pudo leer el perfil. Puedes crear uno o importar tu archivo.';
  }
  render();
  window.JungleProfile = { save };
  $('profileForm').addEventListener('submit', event => {
    event.preventDefault();
    save($('profileName').value);
  });
  $('profileDelete').addEventListener('click', () => {
    if (!window.confirm('¿Borrar tu perfil de este navegador? Tu control se desconectará.')) return;
    try {
      localStorage.removeItem(key);
      sessionStorage.removeItem('jungleName');
    } catch {
      status.textContent = 'El navegador impidió borrar los datos. Prueba desde los ajustes del sitio.';
      return;
    }
    profile = null;
    $('playerName').value = '';
    render();
    window.dispatchEvent(new Event('jungle-profile-deleted'));
    status.textContent = 'Perfil local eliminado. Los archivos exportados se borran desde tu celular.';
  });
  $('profileExport').addEventListener('click', () => {
    if (!profile) return;
    const url = URL.createObjectURL(new Blob([JSON.stringify(profile, null, 2)], { type: 'application/json' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = 'jungle-perfil.json';
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    status.textContent = 'Guarda el archivo para importar tu perfil en el próximo enlace.';
  });
  $('profileImport').addEventListener('change', async event => {
    const file = event.target.files?.[0];
    if (!file) return;
    try {
      if (file.size > 4096) throw new Error('size');
      const imported = JSON.parse(await file.text());
      if (imported?.version !== 1 || !cleanName(imported.name)) throw new Error('format');
      save(imported.name);
    } catch {
      status.textContent = 'Archivo inválido. Elige un perfil JSON exportado desde Jungle Party.';
    } finally {
      event.target.value = '';
    }
  });
  // Keep the room in this tab only, never in the install URL or offline cache.
  if ('serviceWorker' in navigator && window.isSecureContext) {
    navigator.serviceWorker.register('/sw.js').catch(() => {
      status.textContent = 'El perfil funciona. No se pudo activar la instalación y el modo sin conexión.';
    });
  }
  let installPrompt;
  window.addEventListener('beforeinstallprompt', event => {
    event.preventDefault();
    installPrompt = event;
    $('installPwa').hidden = false;
  });
  $('installPwa').addEventListener('click', async () => {
    if (!installPrompt) return;
    await installPrompt.prompt();
    await installPrompt.userChoice;
    installPrompt = null;
    $('installPwa').hidden = true;
  });
  window.addEventListener('appinstalled', () => { $('installPwa').hidden = true; });
})();
