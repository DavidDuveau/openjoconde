#!/usr/bin/env bash
# Sauvegarde la base de la pile docker-compose au format personnalisé de pg_dump (compressé, restaurable table par table).
# Usage, depuis la racine du dépôt : scripts/sauvegarder-base.sh [fichier]
# Sans argument, le fichier est sauvegardes/openjoconde-<date>.dump (dossier ignoré par git).
# Le fichier contient toute la base, y compris les biographies d'artistes : à conserver hors du serveur.
set -euo pipefail

projet=${COMPOSE_PROJECT_NAME:-openjoconde}
fichier=${1:-sauvegardes/openjoconde-$(date +%Y%m%d-%H%M%S).dump}
mkdir -p "$(dirname "$fichier")"

# Écrit dans un fichier temporaire : une sauvegarde interrompue ne remplace jamais une bonne
docker compose -p "$projet" exec -T db pg_dump -U openjoconde -Fc openjoconde > "$fichier.partiel"
# Relit l'archive : un fichier tronqué ou vide est refusé
docker compose -p "$projet" exec -T db pg_restore --list < "$fichier.partiel" > /dev/null
mv "$fichier.partiel" "$fichier"

echo "Sauvegarde écrite : $fichier ($(du -h "$fichier" | cut -f1))"
