const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, 'profile.js'), 'utf8');
function load(store = new Map(), blocked = false) {
  const elements = {}, events = {};
  const el = id => elements[id] ||= { value: '', hidden: false, textContent: '', listeners: {},
    addEventListener(type, handler) { this.listeners[type] = handler; } };
  const context = {
    localStorage: {
      getItem: key => store.get(key) || null,
      setItem(key, value) { if (blocked) throw Error('blocked'); store.set(key, value); },
      removeItem: key => store.delete(key)
    },
    sessionStorage: { removeItem() {} },
    document: { getElementById: el },
    navigator: {}, Event: class { constructor(type) { this.type = type; } },
    confirm: () => true,
    addEventListener(type, fn) { events[type] = fn; },
    dispatchEvent(event) { events[event.type]?.(event); },
    setTimeout
  };
  context.window = context;
  vm.runInNewContext(source, context);
  return { el, store, context, events };
}
(async () => {
  let app = load();
  assert.equal(app.el('profileDelete').hidden, true);
  assert.equal(app.context.JungleProfile.save('  Ana   María <> '), true);
  assert.equal(JSON.parse(app.store.get('jungle.profile.v1')).name, 'Ana María');
  assert.equal(app.el('playerName').value, 'Ana María');
  app = load(app.store);
  assert.match(app.el('profileSummary').textContent, /Ana María/);
  app.el('profileName').value = 'Luis';
  app.el('profileForm').listeners.submit({ preventDefault() {} });
  assert.equal(JSON.parse(app.store.get('jungle.profile.v1')).name, 'Luis');
  const before = app.store.get('jungle.profile.v1');
  assert.equal(app.context.JungleProfile.save('   '), false);
  assert.equal(app.store.get('jungle.profile.v1'), before);
  app.el('profileDelete').listeners.click();
  assert.equal(app.store.has('jungle.profile.v1'), false);
  assert.equal(app.el('playerName').value, '');
  assert.equal(load(app.store).el('profileDelete').hidden, true);
  const input = { files: [{ size: 35, text: async () => '{"version":1,"name":"José"}' }], value: 'profile.json' };
  await app.el('profileImport').listeners.change({ target: input });
  assert.equal(JSON.parse(app.store.get('jungle.profile.v1')).name, 'José');
  input.files = [{ size: 99999, text: async () => '{}' }];
  await app.el('profileImport').listeners.change({ target: input });
  assert.match(app.el('profileStatus').textContent, /inválido/);
  assert.equal(JSON.parse(app.store.get('jungle.profile.v1')).name, 'José');
  const unavailable = load(new Map(), true);
  assert.equal(unavailable.context.JungleProfile.save('Ana'), true);
  assert.match(unavailable.el('profileStatus').textContent, /bloqueó/);
  console.log('PASS: profile create/read/update/delete, reload, invalid import, blocked storage.');
})().catch(error => { console.error(error); process.exitCode = 1; });
