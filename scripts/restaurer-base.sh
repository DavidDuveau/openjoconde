#!/usr/bin/env bash
# Restaure une sauvegarde dans la base de la pile docker-compose. ÉCRASE les tables existantes.
# Usage, depuis la racine du dépôt : scripts/restaurer-base.sh fichier.dump [--oui]
# Sans --oui, demande une confirmation. L'API est arrêtée pendant la restauration puis relancée.
set -euo pipefail

[ $# -ge 1 ] && [ -f "$1" ] || { echo "Usage : $0 fichier.dump [--oui]"; exit 2; }
fichier=$1
projet=${COMPOSE_PROJECT_NAME:-openjoconde}

if [ "${2:-}" != "--oui" ]; then
  read -r -p "Écraser la base du projet « $projet » avec $fichier ? [o/N] " reponse
  [ "$reponse" = "o" ] || { echo "Annulé"; exit 1; }
fi

# Sans connexion de l'API, pg_restore --clean ne se heurte à aucun verrou
docker compose -p "$projet" stop api
trap 'docker compose -p "$projet" start api' EXIT

docker compose -p "$projet" exec -T db pg_restore -U openjoconde -d openjoconde --clean --if-exists --no-owner < "$fichier"
echo "Base restaurée depuis $fichier"
