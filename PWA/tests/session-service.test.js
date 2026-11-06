import test, { describe, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import { ControllerSession } from '../src/controller/session.js';

// Simulación (Mock) del entorno navegador (Window y Document)
const createMockWindow = (extra = {}) => {
  const w = new EventTarget();
  const doc = new EventTarget();
  doc.visibilityState = 'visible';
  return Object.assign(w, {
    document: doc,
    isSecureContext: true,
    setInterval,
    clearInterval,
    setTimeout,
    clearTimeout,
    ...extra
  });
};

// Simulación (Mock) del cliente de red/transporte (equivalente a @Mock de Mockito)
const createMockClient = () => ({
  state: 'STOPPED',
  calls: 0,
  fetchCalls: 0,
  identity: { peerId: 'peer-01', resumeToken: 'TOKEN_SECRETO_PRUEBA' },
  async connect() {
    this.calls++;
    this.state = 'RUNNING';
  },
  async close() {
    this.state = 'STOPPED';
  },
  diagnostic() {
    return { stage: 'READY', code: null };
  }
});

describe('Pruebas unitarias de ControllerSession en Gorilla Escape PWA', () => {
  const url = 'https://example.invalid/#sessionId=test&admission=UNIT_SECRET';
  let mockClient;
  let session;

  beforeEach(() => {
    mockClient = createMockClient();
  });

  test('Prueba unitaria para consultar estado y verificar conexión (equivalente a findAll / consulta)', async () => {
    // Given (Dado un entorno con datos simulados en el Mock del cliente)
    let requestedPath = null;
    mockClient.fetcher = async (path) => {
      mockClient.fetchCalls++;
      requestedPath = path;
      return {
        ok: true,
        json: async () => ({
          status: 'ok',
          service: 'gorilla-escape-local',
          secure: true
        })
      };
    };
    session = new ControllerSession({ win: createMockWindow(), url, client: mockClient });

    // When (Cuando se ejecuta el método de consulta)
    await session.checkConnection();
    const estadoActual = session.snapshot();

    // Then (Entonces se validan los resultados y el número de invocaciones al Mock)
    assert.notEqual(estadoActual, null);
    assert.equal(estadoActual.connectionCheck, 'READY');
    assert.equal(requestedPath, '/api/health');
    assert.equal(mockClient.fetchCalls, 1);
    await session.dispose();
  });

  test('Prueba unitaria para crear y registrar el perfil del jugador (createProfile / setPlayerName)', async () => {
    // Given (Dado un Mock que simula la respuesta exitosa de persistencia POST)
    let payloadEnviado = null;
    mockClient.fetcher = async (path, options) => {
      mockClient.fetchCalls++;
      payloadEnviado = { path, body: JSON.parse(options.body) };
      return {
        ok: true,
        status: 201,
        json: async () => ({ id: 'player-101', displayName: 'Gorila Alfa' })
      };
    };
    session = new ControllerSession({ win: createMockWindow(), url, client: mockClient });
    await session.connect();

    // When (Cuando se ejecuta el método de creación)
    const resultado = await session.createProfile(' Gorila Alfa ');

    // Then (Entonces se valida la persistencia y los atributos enviados)
    assert.equal(resultado, 'SAVED');
    assert.notEqual(payloadEnviado, null);
    assert.equal(payloadEnviado.path, '/mobile/profile');
    assert.equal(payloadEnviado.body.displayName, 'Gorila Alfa');
    assert.equal(mockClient.fetchCalls, 1);
    await session.dispose();
  });
});

