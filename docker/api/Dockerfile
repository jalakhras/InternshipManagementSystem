# API host and database migrator.
#
# Build context is the repository root, because central package management
# (Directory.Packages.props) and common.props sit there and every project imports
# them. Build from the root:
#
#   docker build -f docker/api/Dockerfile -t ims-api .
#   docker build -f docker/api/Dockerfile --target migrator -t ims-migrator .

# ---------------------------------------------------------------------------
# restore
#
# Project files first, on their own layer. They change far less often than source
# does, so an edit to a .cs file reuses a cached restore instead of pulling the
# whole ABP dependency tree again.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY NuGet.Config Directory.Packages.props common.props ./
COPY src/InternshipManagementSystem.Domain.Shared/*.csproj        src/InternshipManagementSystem.Domain.Shared/
COPY src/InternshipManagementSystem.Domain/*.csproj               src/InternshipManagementSystem.Domain/
COPY src/InternshipManagementSystem.Application.Contracts/*.csproj src/InternshipManagementSystem.Application.Contracts/
COPY src/InternshipManagementSystem.Application/*.csproj          src/InternshipManagementSystem.Application/
COPY src/InternshipManagementSystem.EntityFrameworkCore/*.csproj  src/InternshipManagementSystem.EntityFrameworkCore/
COPY src/InternshipManagementSystem.HttpApi/*.csproj              src/InternshipManagementSystem.HttpApi/
COPY src/InternshipManagementSystem.HttpApi.Client/*.csproj       src/InternshipManagementSystem.HttpApi.Client/
COPY src/InternshipManagementSystem.HttpApi.Host/*.csproj         src/InternshipManagementSystem.HttpApi.Host/
COPY src/InternshipManagementSystem.DbMigrator/*.csproj           src/InternshipManagementSystem.DbMigrator/

RUN dotnet restore src/InternshipManagementSystem.HttpApi.Host/InternshipManagementSystem.HttpApi.Host.csproj \
 && dotnet restore src/InternshipManagementSystem.DbMigrator/InternshipManagementSystem.DbMigrator.csproj

# ---------------------------------------------------------------------------
# build
# ---------------------------------------------------------------------------
FROM restore AS build
COPY src/ src/

RUN dotnet publish src/InternshipManagementSystem.HttpApi.Host/InternshipManagementSystem.HttpApi.Host.csproj \
    -c Release --no-restore -o /app/host

RUN dotnet publish src/InternshipManagementSystem.DbMigrator/InternshipManagementSystem.DbMigrator.csproj \
    -c Release --no-restore -o /app/migrator

# ---------------------------------------------------------------------------
# runtime base
#
# `app` is the non-root account the .NET base images ship with. Everything the
# process writes has to be somewhere it owns: Serilog opens Logs/logs.txt on the
# first log line, the blob provider writes uploads under App_Data, and OpenIddict's
# development certificates go under $HOME. A root-owned working directory turns all
# three into a crash at startup rather than a permissions warning.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is here for the container healthcheck and nothing else. The image has no
# shell utility that speaks HTTP, and a healthcheck that cannot run means an
# orchestrator can never tell "starting" from "wedged".
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

ENV DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_HTTP_PORTS=8080 \
    HOME=/home/app

RUN mkdir -p /app/Logs /app/App_Data/blobs /app/keys /home/app \
 && chown -R app:app /app /home/app

EXPOSE 8080
USER app

# ---------------------------------------------------------------------------
# final — the API host
# ---------------------------------------------------------------------------
FROM runtime AS final
COPY --from=build --chown=app:app /app/host ./

HEALTHCHECK --interval=15s --timeout=5s --start-period=90s --retries=10 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "InternshipManagementSystem.HttpApi.Host.dll"]

# ---------------------------------------------------------------------------
# migrator — the same code, run once to create the schema and seed
#
# A separate image rather than migrate-on-start: with more than one API replica,
# migrate-on-start races itself, and a migration that fails takes the whole
# deployment down with a stack trace buried in application logs. As its own
# short-lived container it either exits 0 or it does not, and nothing else starts
# until it has.
# ---------------------------------------------------------------------------
FROM runtime AS migrator
COPY --from=build --chown=app:app /app/migrator ./

ENTRYPOINT ["dotnet", "InternshipManagementSystem.DbMigrator.dll"]
