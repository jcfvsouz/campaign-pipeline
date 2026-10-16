# docker/

Brings up the whole pipeline — `Promo.Api`, `Promo.Worker`, SQL Server, RabbitMQ, and Mailhog — as five containers wired together, so the whole system in the [project root README](../README.md)'s architecture diagram runs with one command.

```bash
cd docker
docker compose up --build
```

## Files

- `docker-compose.yml` — the five services, plus a one-shot `db-init` container.
- `api.Dockerfile` / `worker.Dockerfile` — multi-stage builds for each host. Build context is the **repository root**, not `docker/` or `api/`/`worker/` — both Dockerfiles `COPY components/` alongside their own solution and run `dotnet build components/Components.slnx` first, so `api/`'s and `worker/`'s `nuget.config` (`../components/nupkgs`, relative to each) finds the packages it needs without reaching out to nuget.org for them.
- `../.dockerignore` (repo root) — keeps `bin/`/`obj/` out of the build context; without it, stale local build output would get copied into the image ahead of the real `dotnet build`/`publish` steps.

## Why a `db-init` container instead of running migrations from the app

Neither `Promo.Api` nor `Promo.Worker` creates its own schema — that's intentionally a manual step today (see `api/README.md`). `db-init` is the container-equivalent of that manual step: it waits for SQL Server's healthcheck, then runs `api/database/schema.sql` (which creates the `CampaignPipeline` database itself if it doesn't exist yet) and `seed.sql` with `sqlcmd`, then exits. `promo-api`/`promo-worker` both declare `depends_on: db-init: condition: service_completed_successfully`, so they never start against a database that doesn't have the tables yet.

It isn't idempotent — rerunning `docker compose up` against an existing `sqlserver-data` volume re-runs both scripts, which fail loudly (tables/rows already exist) but are swallowed (the container always exits `0`) so they don't block the services that depend on it. Start clean with:

```bash
docker compose down -v
```

## Configuration

Both app containers get their `ConnectionStrings:SqlServer`/`RabbitMq:Host` (and the Worker's `Smtp:Host`) overridden via environment variables (ASP.NET Core's `__` double-underscore convention) pointing at the other services' **container names** (`sqlserver`, `rabbitmq`, `mailhog`) instead of `localhost` — everything else (ports, exchange name, queue name, credentials) matches each service's own `appsettings.json` defaults, since those defaults were already written to match this compose file's services.

The SQL Server SA password (`YourStrong!Passw0rd`) and RabbitMQ's `guest`/`guest` are the same placeholder dev credentials already baked into `api/`'s and `worker/`'s `appsettings.json` — fine for a local demo, not meant to go anywhere near a real deployment.

If a port here is already taken by something else on your machine (a locally installed RabbitMQ, another container, any app bound to one of these ports), remap it in a `docker-compose.override.yml` next to `docker-compose.yml` — Compose merges it automatically, and it's gitignored since the right remap is specific to your machine. List-type fields like `ports` merge by concatenation, not replacement, by default; force a replacement with the `!override` YAML tag:

```yaml
services:
  rabbitmq:
    ports: !override
      - "5673:5672"
      - "15673:15672"
```

`promo-api`/`promo-worker` both run with `restart: unless-stopped` — a transient dependency race at startup (RabbitMQ's healthcheck can report healthy a moment before its AMQP listener is actually accepting connections, especially on a slow/loaded host) gets retried automatically instead of leaving the container dead with nothing to bring it back.

This stack is memory-hungry: SQL Server alone wants a solid chunk of RAM to get through its first-run initialization, on top of RabbitMQ, the two .NET apps, and Docker Desktop's own VM. On an 8GB host with other things running, SQL Server's startup can slow down enormously (trivial file copies taking minutes instead of seconds) and the healthcheck can flap between `starting`/`unhealthy`/`healthy` before settling — give it room, or bring services up in stages (`docker compose up -d sqlserver mailhog rabbitmq`, wait for `sqlserver` healthy, then `docker compose up -d` for the rest) rather than everything at once.

## Ports exposed to the host

| Service | Port(s) | What's there |
|---|---|---|
| `promo-api` | 8080 | the API; `/swagger` for interactive docs |
| `rabbitmq` | 5672, 15672 | AMQP, and the management UI (`guest`/`guest`) |
| `mailhog` | 1025, 8025 | SMTP, and the web UI showing every e-mail `Promo.Worker` sent |
| `sqlserver` | 1433 | so a local SQL client can inspect the database directly |

## Status

Verified end to end against a real Docker Desktop install: all five containers reach `healthy`/`running`, `db-init` completes successfully, and `POST /campaigns` → `POST /campaigns/{id}/publish` produces exactly three e-mails in Mailhog (one per buyer in the seeded VIP Customers group), with `Promo.Worker`'s own log confirming the dispatch.
