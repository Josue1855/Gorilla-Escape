const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, 'profile.js'), 'utf8');
function load(store = new Map(), blocked = false) {
  const elements = {}, events = {};
  const el = id => elements[id] ||= { value: '', hidden: false, textContent: '', listeners: {},
    style: {}, children: [], replaceChildren() { this.children = []; }, appendChild(child) { this.children.push(child); }, addEventListener(type, handler) { this.listeners[type] = handler; } };
  const context = {
    localStorage: {
      getItem: key => store.get(key) || null,
      setItem(key, value) { if (blocked) throw Error('blocked'); store.set(key, value); },
      removeItem: key => store.delete(key)
    },
    sessionStorage: { removeItem() {} },
    document: { getElementById: el, querySelector: el, createElement: () => ({ textContent: '' }) },
    navigator: {}, Event: class { constructor(type) { this.type = type; } },
    confirm: () => true,
    addEventListener(type, fn) { events[type] = fn; },
    dispatchEvent(event) { events[event.type]?.(event); },
    setInterval() {}, setTimeout
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
    app.el('profileAvatar').value = 'playful';
  app.el('profileColor').value = 'blue';
  app.el('pointerSensitivity').value = '1.4';
  app.el('controlVibration').checked = false;
  app.context.JungleProfile.save('José');
  const customized = JSON.parse(app.store.get('jungle.profile.v1'));
  assert.equal(customized.avatar, 'playful');
  assert.equal(customized.color, 'blue');
  assert.equal(customized.settings.sensitivity, 1.4);
  assert.equal(customized.settings.vibration, false);
  app = load(app.store);
  assert.equal(app.context.JungleProfile.settings().sensitivity, 1.4);
  assert.equal(app.el('controlVibration').checked, false);
  const result = {id:'round-1',game:'WhackAMole',score:500,time:Date.now()};
  app = load(new Map([['jungle.profile.v1', JSON.stringify({...customized, results:[result]})]]));
  assert.match(app.el('profileRecords').children[0].textContent, /500/);
  assert.match(app.el('profileResults').children[0].textContent, /Whack-a-Mole/);
  console.log('PASS: profile CRUD, migration, settings, avatars, records, reload, invalid import, blocked storage.');
})().catch(error => { console.error(error); process.exitCode = 1; });

