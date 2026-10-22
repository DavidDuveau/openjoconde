// Prépare la base puis lance l'API de test (Playwright arrête ce processus et ses enfants à la fin)
import { spawn, spawnSync } from 'node:child_process';
import path from 'node:path';
import { API_KEY, API_PORT, connectionString, createDatabase, repoDir } from './pile.mjs';

const infrastructure = path.join(repoDir, 'src', 'Backend', 'OpenJoconde.Infrastructure');
const api = path.join(repoDir, 'src', 'Backend', 'OpenJoconde.API');
const artifacts = path.join(repoDir, '.e2e', 'artifacts');

createDatabase();

const env = {
  ...process.env,
  ConnectionStrings__DefaultConnection: connectionString(),
  Security__ApiKey: API_KEY,
  Cors__Origins__0: `http://localhost:${process.env.E2E_FRONT_PORT || 8101}`,
  ASPNETCORE_ENVIRONMENT: 'Development',
  // Variables MSBuild : build, ef et run partagent les mêmes artefacts, sans toucher aux bin/obj de développement
  UseArtifactsOutput: 'true',
  ArtifactsPath: artifacts
};

const options = { cwd: repoDir, env, stdio: 'inherit' };
const build = spawnSync('dotnet', ['build', api, '--nologo', '-v', 'q'], options);
if (build.status !== 0) process.exit(build.status ?? 1);

const migrate = spawnSync(
  'dotnet',
  ['ef', 'database', 'update', '--project', infrastructure, '--startup-project', api, '--no-build', '--connection', connectionString()],
  options
);
if (migrate.status !== 0) process.exit(migrate.status ?? 1);

const server = spawn(
  'dotnet',
  ['run', '--project', api, '--no-build', '--no-launch-profile', '--urls', `http://localhost:${API_PORT}`],
  options
);
server.on('exit', code => process.exit(code ?? 0));
