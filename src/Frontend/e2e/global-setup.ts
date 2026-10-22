import { request } from '@playwright/test';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { API_KEY, API_PORT, repoDir } from './pile.mjs';

const TITRES = ['Les Baigneuses', 'Portrait de femme', 'Nature morte aux pommes', 'Paysage de neige', 'La Liseuse', 'Vase décoré'];

/**
 * 24 notices : en-tête réel du fichier Joconde, titres variés (dont des œuvres sans titre), trois domaines, quatre
 * techniques, trois périodes, trois musées, un lien de site et des droits d'auteur pour certaines.
 */
function notices(): string {
  const fixture = path.join(repoDir, 'src', 'Backend', 'OpenJoconde.Infrastructure.Tests', 'Fixtures', 'joconde_extrait_reel.csv');
  const colonnes = readFileSync(fixture, 'utf8').split(/\r?\n/)[0].split('|');
  const ligne = (i: number) => {
    const valeurs: Record<string, string> = {
      reference: `E2E${String(i).padStart(5, '0')}`,
      titre: i % 6 === 0 ? '' : `${TITRES[i % TITRES.length]} ${i}`,
      description: `Description de la notice ${i}`,
      domaine: `domaine ${i % 3}`,
      materiaux_techniques: `technique ${i % 4}`,
      periode_de_creation: `période ${i % 3}`,
      auteur: `AUTEUR${i % 5} Prénom`,
      nom_officiel_musee: `musée ${i % 3}`,
      lien_site_associe: i % 2 === 0 ? 'https://example.org/musee' : '',
      artiste_sous_droits: i % 4 === 0 ? 'oui' : '',
      date_entree_dans_le_domaine_public: i % 4 === 0 ? '2080' : ''
    };
    return colonnes.map(c => `"${(valeurs[c.toLowerCase()] ?? '').replace(/"/g, '""')}"`).join('|');
  };
  return [colonnes.join('|'), ...Array.from({ length: 24 }, (_, i) => ligne(i + 1))].join('\n');
}

// Charge les notices de test dans la base jetable par la route d'import
export default async function globalSetup() {
  const api = await request.newContext({ baseURL: `http://localhost:${API_PORT}` });
  const response = await api.post('/api/import/from-file', {
    headers: { 'X-Api-Key': API_KEY },
    multipart: { file: { name: 'joconde_e2e.csv', mimeType: 'text/csv', buffer: Buffer.from(notices(), 'utf8') } }
  });
  if (!response.ok()) throw new Error(`Import des données de test refusé : ${response.status()} ${await response.text()}`);
  await api.dispose();
}
