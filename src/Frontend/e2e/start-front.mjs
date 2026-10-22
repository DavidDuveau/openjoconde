// Construit le front contre l'API de test puis le sert (SPA) sur le port de test
import { spawn, spawnSync } from 'node:child_process';
import { API_PORT, FRONT_PORT, frontendDir } from './pile.mjs';

// Le build du front attend que l'API soit prête : les deux builds ensemble pèsent trop sur un poste à 8 Go
for (let essai = 0; essai < 300; essai++) {
  try {
    if ((await fetch(`http://localhost:${API_PORT}/api/health`)).ok) break;
  } catch {
    // l'API démarre encore
  }
  await new Promise(resolve => setTimeout(resolve, 1000));
}

const env = { ...process.env, VUE_APP_API_URL: `http://localhost:${API_PORT}/api` };
const options = { cwd: frontendDir, env, stdio: 'inherit', shell: true };

const build = spawnSync('npx', ['vite', 'build', '--outDir', 'dist-e2e'], options);
if (build.status !== 0) process.exit(build.status ?? 1);

const server = spawn('npx', ['serve', '-s', 'dist-e2e', '-l', String(FRONT_PORT)], options);
server.on('exit', code => process.exit(code ?? 0));
