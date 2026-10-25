#!/usr/bin/env bash
# Démarre la pile avec Caddy devant (docker-compose.https.yml, domaine « localhost ») et vérifie : HTTPS servi, API relayée,
# front non publié directement, et limite de requêtes non contournable par un en-tête X-Forwarded-For falsifié.
# Usage, depuis la racine du dépôt : scripts/verifier-https.sh
set -euo pipefail

export POSTGRES_PASSWORD=essai_verif_https
export API_KEY=cle_verif_https
export DOMAINE=localhost
export HTTP_PORT=8183
export HTTPS_PORT=8444
projet=oj-verif-https
compose() { docker compose -p "$projet" -f docker-compose.yml -f docker-compose.https.yml "$@"; }

nettoyer() { compose down -v >/dev/null 2>&1 || true; }
trap nettoyer EXIT

compose up -d --build --wait --wait-timeout 240
url=https://localhost:$HTTPS_PORT

attendre() { # url, motif attendu dans la réponse
  for _ in $(seq 1 30); do
    if curl -fsSk "$1" | grep -q "$2"; then return 0; fi
    sleep 2
  done
  echo "Echec : $1 ne contient pas « $2 »"
  compose logs --tail 30
  return 1
}

attendre "$url/" '<div id="app"'
attendre "$url/api/health/ping" pong

# Le front n'est joignable que par Caddy : son port n'est pas publié
if [ -n "$(docker port "$(compose ps -q front)")" ]; then
  echo "Le port du front est publié"; exit 1
fi

# 25 écritures refusées (400 ou 401) avec une adresse falsifiée différente à chaque fois : si l'en-tête était cru,
# chacune aurait sa propre limite et aucune ne serait refusée pour excès de requêtes (429)
refusees=0
for i in $(seq 1 25); do
  code=$(curl -sk -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' \
    -H "X-Forwarded-For: 203.0.113.$i" -d '{}' "$url/api/museums")
  [ "$code" = "429" ] && refusees=$((refusees + 1))
done
[ "$refusees" -gt 0 ] || { echo "Aucune requête limitée : l'adresse falsifiée a été crue"; exit 1; }
echo "Pile HTTPS vérifiée ($refusees requêtes limitées sur 25)"
