#!/usr/bin/env bash
# Construit les images Docker de l'API et du front puis vérifie qu'elles fonctionnent ensemble, sans base de données :
# utilisateur non root, front servi, repli de l'application monopage, /api relayé jusqu'à l'API.
# Usage, depuis la racine du dépôt : scripts/verifier-images.sh
set -euo pipefail

reseau=oj-verif-$$
nettoyer() {
  docker rm -f oj-verif-api oj-verif-front >/dev/null 2>&1 || true
  docker network rm "$reseau" >/dev/null 2>&1 || true
}
trap nettoyer EXIT

docker build -f docker/api.Dockerfile -t openjoconde-api:verif .
docker build -f docker/front.Dockerfile -t openjoconde-front:verif .

uid=$(docker run --rm --entrypoint id openjoconde-api:verif -u)
[ "$uid" != "0" ] || { echo "L'API tourne en root"; exit 1; }
uid=$(docker run --rm --entrypoint id openjoconde-front:verif -u)
[ "$uid" != "0" ] || { echo "Le front tourne en root"; exit 1; }

docker network create "$reseau" >/dev/null
docker run -d --name oj-verif-api --network "$reseau" --network-alias api \
  -e ConnectionStrings__DefaultConnection="Host=db-absente;Database=x;Username=x" openjoconde-api:verif >/dev/null
docker run -d --name oj-verif-front --network "$reseau" -p 8088:8080 -e API_UPSTREAM=api:8080 openjoconde-front:verif >/dev/null

attendre() { # url, motif attendu dans la réponse
  for _ in $(seq 1 30); do
    if curl -fsS "$1" | grep -q "$2"; then return 0; fi
    sleep 2
  done
  echo "Echec : $1 ne contient pas « $2 »"
  docker logs oj-verif-api | tail -20
  docker logs oj-verif-front | tail -20
  return 1
}

attendre http://localhost:8088/ '<div id="app"'
attendre http://localhost:8088/artworks '<div id="app"'
attendre http://localhost:8088/api/health/ping pong
echo "Images vérifiées"
