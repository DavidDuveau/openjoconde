#!/usr/bin/env bash
# Démarre la pile docker-compose (base, API, front) avec des secrets jetables et vérifie qu'elle répond :
# base saine avec le collationnement ICU français, API saine, front servi, /api relayé, routes d'administration protégées.
# Usage, depuis la racine du dépôt : scripts/verifier-compose.sh
set -euo pipefail

export POSTGRES_PASSWORD=essai_verif_compose
export API_KEY=cle_verif_compose
export FRONT_PORT=8087
projet=oj-verif-compose
compose() { docker compose -p "$projet" "$@"; }

nettoyer() {
  compose down -v >/dev/null 2>&1 || true
}
trap nettoyer EXIT

compose up -d --build --wait --wait-timeout 180

attendre() { # url, motif attendu dans la réponse
  for _ in $(seq 1 30); do
    if curl -fsS "$1" | grep -q "$2"; then return 0; fi
    sleep 2
  done
  echo "Echec : $1 ne contient pas « $2 »"
  compose logs --tail 30
  return 1
}

attendre http://localhost:$FRONT_PORT/ '<div id="app"'
attendre http://localhost:$FRONT_PORT/api/health/ping pong
# Santé complète : la base répond et le schéma a été créé par l'API au démarrage
attendre http://localhost:$FRONT_PORT/api/health Healthy

fournisseur=$(compose exec -T db psql -U openjoconde -tAc "select datlocprovider::text || ':' || datlocale from pg_database where datname = 'openjoconde'")
[ "$fournisseur" = "i:fr-FR" ] || { echo "Collationnement inattendu : $fournisseur"; exit 1; }

code=$(curl -s -o /dev/null -w '%{http_code}' -X POST http://localhost:$FRONT_PORT/api/sync/start)
[ "$code" = "401" ] || { echo "POST /api/sync/start sans clé a répondu $code au lieu de 401"; exit 1; }
# Sauvegarde puis restauration : un marqueur modifié après la sauvegarde retrouve sa valeur d'origine
sql() { compose exec -T db psql -U openjoconde -tAc "$1"; }
export COMPOSE_PROJECT_NAME=$projet
archive=$(mktemp -d)/essai.dump
sql "create table oj_marqueur(v text); insert into oj_marqueur values ('avant')" > /dev/null
scripts/sauvegarder-base.sh "$archive"
sql "update oj_marqueur set v = 'apres'" > /dev/null
scripts/restaurer-base.sh "$archive" --oui
[ "$(sql 'select v from oj_marqueur')" = "avant" ] || { echo "La restauration n'a pas rétabli la sauvegarde"; exit 1; }
attendre http://localhost:$FRONT_PORT/api/health Healthy
echo "Pile docker-compose vérifiée"
