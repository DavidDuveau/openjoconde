// Pile de test de bout en bout : base jetable, API sur 5101, front construit puis servi sur 8101.
// Ne touche ni à la base ni aux ports de développement.
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import path from 'node:path';

export const API_PORT = 5101;
export const FRONT_PORT = 8101;
export const API_KEY = 'e2e-key';
export const DATABASE = 'openjoconde_e2e';

// Les commandes se lancent depuis src/Frontend (npm run e2e)
export const frontendDir = process.cwd();
export const repoDir = path.resolve(frontendDir, '..', '..');

const windowsPsql = 'C:/Program Files/PostgreSQL/17/bin/psql.exe';

// Le mot de passe de postgres vient de E2E_PG_PASSWORD (CI) ou de pgpass.conf (poste de développement)
export function connectionString() {
  const password = process.env.E2E_PG_PASSWORD ? `;Password=${process.env.E2E_PG_PASSWORD}` : '';
  return `Host=localhost;Port=5432;Username=postgres;Database=${DATABASE}${password}`;
}

export function psql(sql) {
  const exe = process.env.E2E_PSQL || (existsSync(windowsPsql) ? windowsPsql : 'psql');
  const env = { ...process.env };
  if (process.env.E2E_PG_PASSWORD) env.PGPASSWORD = process.env.E2E_PG_PASSWORD;
  const result = spawnSync(exe, ['-w', '-h', 'localhost', '-U', 'postgres', '-d', 'postgres', '-v', 'ON_ERROR_STOP=1', '-c', sql], {
    env,
    encoding: 'utf8'
  });
  if (result.status !== 0) throw new Error(`psql a échoué : ${result.stderr || result.error}`);
}

export function dropDatabase() {
  psql(`DROP DATABASE IF EXISTS ${DATABASE} WITH (FORCE)`);
}

// ICU fr-FR comme en production : l'ordre des titres en dépend
export function createDatabase() {
  dropDatabase();
  psql(`CREATE DATABASE ${DATABASE} TEMPLATE template0 ENCODING 'UTF8' LOCALE_PROVIDER icu ICU_LOCALE 'fr-FR'`);
}
