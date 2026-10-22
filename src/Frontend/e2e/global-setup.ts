import { request } from '@playwright/test';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { API_KEY, API_PORT, repoDir } from './pile.mjs';
import { NOMBRE, Notice, NOTICES } from './jeu';

/**
 * Notices de test (voir jeu.ts) : en-tête réel du fichier Joconde, titres variés (dont des œuvres sans titre), trois
 * domaines, quatre techniques, trois périodes, trois musées, un lien de site et des droits d'auteur pour certaines.
 */
function csv(): string {
  const fixture = path.join(repoDir, 'src', 'Backend', 'OpenJoconde.Infrastructure.Tests', 'Fixtures', 'joconde_extrait_reel.csv');
  const colonnes = readFileSync(fixture, 'utf8').split(/\r?\n/)[0].split('|');
  const ligne = (notice: Notice) => {
    const i = notice.numero;
    const valeurs: Record<string, string> = {
      reference: notice.reference,
      titre: notice.titre,
      description: `Description de la notice ${i}`,
      domaine: notice.domaine,
      materiaux_techniques: notice.technique,
      periode_de_creation: notice.periode,
      auteur: `AUTEUR${i % 5} Prénom`,
      nom_officiel_musee: notice.musee,
      lien_site_associe: i % 2 === 0 ? 'https://example.org/musee' : '',
      artiste_sous_droits: i % 4 === 0 ? 'oui' : '',
      date_entree_dans_le_domaine_public: i % 4 === 0 ? '2080' : ''
    };
    return colonnes.map(c => `"${(valeurs[c.toLowerCase()] ?? '').replace(/"/g, '""')}"`).join('|');
  };
  return [colonnes.join('|'), ...NOTICES.map(ligne)].join('\n');
}

// Charge les notices de test dans la base jetable par la route d'import
export default async function globalSetup() {
  const api = await request.newContext({ baseURL: `http://localhost:${API_PORT}` });
  const response = await api.post('/api/import/from-file', {
    headers: { 'X-Api-Key': API_KEY },
    multipart: { file: { name: 'joconde_e2e.csv', mimeType: 'text/csv', buffer: Buffer.from(csv(), 'utf8') } }
  });
  if (!response.ok()) throw new Error(`Import des données de test refusé : ${response.status()} ${await response.text()}`);

  // Un import partiel fausserait tous les scénarios sans qu'aucun ne le dise : on le refuse ici
  const total = (await (await api.get('/api/artworks?pageSize=1')).json()).totalItems;
  if (total !== NOMBRE) throw new Error(`Import des données de test incomplet : ${total} notices en base au lieu de ${NOMBRE}`);
  await api.dispose();
}
