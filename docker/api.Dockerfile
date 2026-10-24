# Image de l'API. Contexte de build : la racine du dépôt.
#   docker build -f docker/api.Dockerfile -t openjoconde-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.config ./
COPY src/Backend src/Backend
RUN dotnet publish src/Backend/OpenJoconde.API/OpenJoconde.API.csproj -c Release -o /app --no-self-contained

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
