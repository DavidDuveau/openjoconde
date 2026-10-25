#!/usr/bin/env bash
# Construit les images de l'API et du front, refuse celles qui ont une vulnérabilité haute ou critique corrigeable (Trivy),
# vérifie la construction pour amd64 et arm64, puis les publie si --publier est donné.
# Usage, depuis la racine du dépôt : scripts/publier-images.sh <étiquette> [--publier]
#   <étiquette>  : vX.Y.Z pour une release, ou un nom libre (essai) ; l'étiquette « latest » est ajoutée avec --publier
#   REGISTRE     : préfixe des images, par défaut ghcr.io/<propriétaire du dépôt en minuscules>
# Sans --publier, rien n'est envoyé : c'est le mode de la CI sur les pull requests.
# Avec --publier : être connecté au registre (docker login) et disposer d'un builder buildx multi-architectures.
set -euo pipefail

etiquette=${1:?Usage : $0 <étiquette> [--publier]}
publier=false
[ "${2:-}" = "--publier" ] && publier=true

proprietaire=${GITHUB_REPOSITORY_OWNER:-davidduveau}
registre=${REGISTRE:-ghcr.io/${proprietaire,,}}
depot=${GITHUB_REPOSITORY:-DavidDuveau/openjoconde}
plateformes=linux/amd64,linux/arm64

for service in api front; do
  image=$registre/openjoconde-$service

  # Image amd64 chargée dans le démon local : c'est celle que Trivy analyse
  docker buildx build --load --platform linux/amd64 -f docker/$service.Dockerfile -t "$image:analyse" .
  docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:0.58.0 image \
    --quiet --severity HIGH,CRITICAL --ignore-unfixed --exit-code 1 "$image:analyse"

  etiquettes=(-t "$image:$etiquette")
  if $publier; then
    etiquettes+=(-t "$image:latest" --push)
  fi
  # Sans --push ni --load, une construction multi-architecture vérifie seulement qu'elle aboutit
  docker buildx build --platform "$plateformes" -f docker/$service.Dockerfile \
    --label "org.opencontainers.image.source=https://github.com/$depot" \
    --label "org.opencontainers.image.version=$etiquette" \
    "${etiquettes[@]}" .
  echo "$image : analysée et construite pour $plateformes$($publier && echo ', publiée sous '"$etiquette"' et latest')"
done
