import { useState } from 'react';
import { checkServer } from './connection/health.js';

export default function App() {
  const [state, setState] = useState('idle');
  const [message, setMessage] = useState('Abre el juego en la PC para preparar tu control.');
  async function check() {
    setState('checking');
    try {
      await checkServer();
      setMessage('La PC responde. La conexión a la partida estará disponible próximamente.');
      setState('ready');
    } catch {
      setMessage('No pudimos contactar con la PC. Comprueba la conexión y vuelve a intentarlo.');
      setState('error');
    }
  }
  return <main>
    <p className="eyebrow">GORILIMPIADAS</p>
    <h1>Tu movimiento.<br />Tu victoria.</h1>
    <p>Bienvenido a Gorilla Escape.</p>
    <section aria-label="Estado del control">
      <h2>Prepara tu control</h2>
      <p role="status" aria-live="polite">{message}</p>
      <button onClick={check} disabled={state === 'checking'}>
        {state === 'checking' ? 'Comprobando…' : 'Comprobar conexión'}
      </button>
    </section>
    <p className="safety">Sujeta siempre el teléfono. Despeja el espacio antes de moverte.</p>
  </main>;
}
