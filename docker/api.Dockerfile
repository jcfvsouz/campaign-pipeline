# Build context is the repository root (see docker-compose.yml) — components/ and api/ must
# both be present so api/nuget.config's local feed (../components/nupkgs, relative to api/)
# resolves to the packages this stage builds first.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY components/ components/
COPY api/ api/

RUN dotnet build components/Components.slnx -c Release

RUN dotnet publish api/src/Promo.Api/Promo.Api.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Promo.Api.dll"]
