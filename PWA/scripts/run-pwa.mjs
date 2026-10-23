import { access } from 'node:fs/promises';
import { spawn, spawnSync } from 'node:child_process';
import process from 'node:process';

const args = new Set(process.argv.slice(2));
const host = valueFor('--host') || process.env.HOST || '0.0.0.0';
const port = valueFor('--port') || process.env.PORT || '4173';
const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';

function valueFor(name) {
  const argument = process.argv.find(value => value === name || value.startsWith(`${name}=`));
  if (!argument) return null;
  if (argument === name) return process.argv[process.argv.indexOf(argument) + 1];
  return argument.slice(name.length + 1);
}

function run(command, commandArgs) {
  const result = spawnSync(command, commandArgs, {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}

if (Number(process.versions.node.split('.')[0]) !== 24) {
  console.error(`Se requiere Node.js 24.x; versión detectada: ${process.version}`);
  process.exit(1);
}

try {
  await access('node_modules');
} catch {
  console.log('Instalando dependencias bloqueadas por package-lock.json…');
  run(npmCommand, ['ci']);
}

if (args.has('--dev')) {
  const child = spawn(npmCommand, ['run', 'dev', '--', '--host', host, '--port', port], {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });
  child.on('exit', code => process.exit(code ?? 0));
} else {
  run(npmCommand, ['run', 'build']);
  const child = spawn(npmCommand, ['run', 'preview', '--', '--host', host, '--port', port], {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });
  child.on('exit', code => process.exit(code ?? 0));
}
