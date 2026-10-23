# Gorilla Escape · PWA de control

Aplicación React/Vite que convierte el teléfono en el control de **Gorilla
Escape**. El build genera el manifiesto instalable y un service worker con
cache del shell offline; las rutas `/api/` nunca se cachean.

## Requisitos

- Node.js **24.x** (el rango está fijado en `package.json`).
- npm incluido con Node.js.
- Un navegador compatible con PWA, WebRTC y sensores del dispositivo.
- Para probar sensores desde un teléfono real: HTTPS, salvo `localhost`.

No se requieren dependencias globales. Todas las dependencias están declaradas
en [package.json](./package.json) y fijadas en
[package-lock.json](./package-lock.json).

### Dependencias de ejecución

- `react` y `react-dom` 19.3.0.
- `vite` 8.3.2 y `@vitejs/plugin-react` 6.1.2 para desarrollo y build.

## Ejecutar en cualquier sistema con Node.js 24

Desde esta carpeta:

```bash
npm start
```

El lanzador [scripts/run-pwa.mjs](./scripts/run-pwa.mjs):

1. verifica Node.js 24.x;
2. ejecuta `npm ci` si todavía no existe `node_modules`;
3. ejecuta el build de producción, incluido el service worker;
4. publica `dist/` en `0.0.0.0:4173`.

Para elegir otro host o puerto:

```bash
npm start -- --host 127.0.0.1 --port 4173
```

Para desarrollo con recarga:

```bash
npm start -- --dev --host 127.0.0.1 --port 5173
```

En Windows PowerShell, Linux y macOS se usa el mismo comando. Para que un
teléfono en la misma Wi-Fi pueda conectarse, usa `--host 0.0.0.0` y publica el
servidor mediante HTTPS; el proxy de desarrollo conserva `/api` apuntando a
`http://127.0.0.1:8080`.

## Comandos manuales

```bash
npm ci             # instala exactamente el lockfile
npm test           # ejecuta las pruebas Node
npm run build      # genera dist/ y dist/sw.js
npm run preview    # sirve el último build
npm run dev        # servidor Vite de desarrollo
```

## Registro de la PWA

El manifiesto está en [public/manifest.webmanifest](./public/manifest.webmanifest)
y se enlaza desde [index.html](./index.html). En builds de producción,
[src/main.jsx](./src/main.jsx) registra `/sw.js` después de cargar la página.
La instalación se ofrece desde el menú del navegador cuando se sirve por
HTTPS (o desde `localhost`).

La vista técnica está disponible con `?view=health`.

## Ejecutar con Docker

Construir la imagen desde esta carpeta:

```bash
docker build -t gorilla-escape-pwa .
```

Ejecutar el contenedor:

```bash
docker run --rm --name gorilla-escape-pwa -p 4173:8080 gorilla-escape-pwa
```

La PWA estará disponible en <http://localhost:4173>. El contenedor usa una
compilación multi-etapa: Node.js sólo se incluye durante el build y la imagen
final usa Nginx sin privilegios en el puerto interno `8080`.

Para construir para servidores `linux/amd64` desde cualquier plataforma:

```bash
docker buildx build --platform linux/amd64 -t gorilla-escape-pwa .
```
