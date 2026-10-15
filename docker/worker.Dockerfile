# Build context is the repository root (see docker-compose.yml) — components/ and worker/ must
# both be present so worker/nuget.config's local feed (../components/nupkgs, relative to
# worker/) resolves to the packages this stage builds first.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY components/ components/
COPY worker/ worker/

RUN dotnet build components/Components.slnx -c Release

RUN dotnet publish worker/src/Promo.Worker/Promo.Worker.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Promo.Worker.dll"]
