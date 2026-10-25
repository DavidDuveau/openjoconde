# Image du front : fichiers statiques servis par nginx, qui relaie /api vers l'API (même origine, pas de CORS).
# Contexte de build : la racine du dépôt (le build lit CHANGELOG.md).
#   docker build -f docker/front.Dockerfile -t openjoconde-front .
# Fichiers statiques : construits une fois, quelle que soit l'architecture de l'image
FROM --platform=$BUILDPLATFORM node:22-alpine AS build
WORKDIR /app/src/Frontend
COPY src/Frontend/package.json src/Frontend/package-lock.json ./
RUN npm ci
COPY CHANGELOG.md /app/CHANGELOG.md
COPY src/Frontend ./
# Adresse relative : l'API est atteinte par le même domaine que le front
ENV VUE_APP_API_URL=/api
RUN npm run build

FROM nginxinc/nginx-unprivileged:alpine AS runtime
# Correctifs de sécurité de la base Alpine publiés depuis la construction de l'image nginx (contrôlé par Trivy en CI)
USER root
RUN apk upgrade --no-cache && mkdir -p /etc/nginx/realip
USER nginx
# Adresse de l'API, lue au démarrage (modèle nginx traité par envsubst)
ENV API_UPSTREAM=api:8080
COPY docker/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/src/Frontend/dist /usr/share/nginx/html
EXPOSE 8080
