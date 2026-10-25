# Image de l'API. Contexte de build : la racine du dépôt.
#   docker build -f docker/api.Dockerfile -t openjoconde-api .
# Compilation sur l'architecture de la machine de build (pas d'émulation), pour l'architecture de l'image demandée (amd64 ou arm64)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY NuGet.config ./
COPY src/Backend src/Backend
RUN dotnet publish src/Backend/OpenJoconde.API/OpenJoconde.API.csproj -c Release -o /app --no-self-contained \
    -a $([ "$TARGETARCH" = "arm64" ] && echo arm64 || echo x64)

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Dossier de téléchargement du CSV (1,2 Go) : à monter en volume, appartient à l'utilisateur non root
RUN mkdir -p /data/temp && chown -R $APP_UID /data
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Sync__TempDirectory=/data/temp
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
VOLUME /data/temp
ENTRYPOINT ["dotnet", "OpenJoconde.API.dll"]
