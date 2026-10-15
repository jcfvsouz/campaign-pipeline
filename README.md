# Campaign Publication & Batch Email Dispatch

> Portfolio project: an event-driven campaign publishing pipeline in .NET 10 — Clean Architecture, RabbitMQ, Docker & Kubernetes, with local NuGet packaging and xUnit tests.

A small, self-contained system that models a real problem from CRM/marketing platforms: a marketer publishes a **campaign** targeted at a **buyer group**, and the platform reacts asynchronously — dispatching a batch of e-mails to every buyer in that group — without making the publish request wait on the sending itself.

This project exists as a portfolio piece to demonstrate, end to end and with working code: Clean Architecture in .NET, an event-driven pipeline (API → message broker → worker), containerization with Docker, and orchestration with Kubernetes. It intentionally mirrors an architecture pattern from my own production experience (a CRM campaign-segmentation system), rebuilt from scratch with synthetic data — no proprietary code or business data involved.

## Why this project

Most portfolio repositories show one thing well — a CRUD API, a UI component, an algorithm exercise. This one is deliberately broader: a single, cohesive scenario used as scaffolding to demonstrate the stacks I work with — Clean Architecture in .NET, SQL Server via Dapper, asynchronous messaging, containerization, and cluster orchestration — inside one small codebase, instead of spreading that evidence across several disconnected toy repos. Every piece here is intentionally small; the point is showing how the pieces fit together end to end, not depth in any single one.

## Architecture

```
                    POST /campaigns/{id}/publish
                              │
                              ▼
                    ┌───────────────────┐
                    │   Campaign API    │   ASP.NET Core, Clean Architecture
                    │  (Promo.Api)      │   SQL Server via Dapper
                    └─────────┬─────────┘
                              │ publishes "CampaignPublished" event
                              ▼
                    ┌───────────────────┐
                    │     RabbitMQ      │   message broker
                    └─────────┬─────────┘
                              │ consumed by
                              ▼
                    ┌───────────────────┐
                    │  Campaign Worker   │   background service
                    │  (Promo.Worker)    │   fetches target buyers, sends batch e-mail
                    └─────────┬─────────┘
                              │ SMTP
                              ▼
                    ┌───────────────────┐
                    │      Mailhog       │   fake SMTP server + web UI —
                    │                    │   lets you *see* every e-mail the
                    └───────────────────┘   Worker sent, with no real inbox involved
```

Everything above the "SMTP" arrow runs as its own container; `docker-compose` wires the five of them together, and the Kubernetes manifests do the same job for a cluster (`kind`/`minikube` locally).

## Repository layout

```
portfolio-campaign-pipeline/
├── components/     # Shared class libraries, packed as local NuGet packages — see components/README.md
├── api/             # Promo.Api — Clean Architecture, publishes CampaignPublished — see api/README.md
├── worker/           # Promo.Worker — consumes the queue, sends batch e-mail via Mailhog — see worker/README.md
├── docker/           # docker-compose.yml wiring API + Worker + RabbitMQ + SQL Server + Mailhog
└── k8s/              # Deployment/Service manifests for a local cluster                       [planned]
```

## Status

| Piece | Status |
|---|---|
| `components/` — Result, FluentResult extensions, SQL Server repository | ✅ done |
| `components/` — RabbitMQ messaging (publisher, consumer, dead-letter queue) | ✅ done |
| `components/` — Components.Hosting (`UseStartup<T>` composition root) | ✅ done |
| `components/` — xUnit tests (builders + fixtures) | ✅ done |
| `components/` — sample console apps per package | ✅ done |
| `api/` — Clean Architecture (Domain/Application/Infrastructure), create + publish campaign endpoints | ✅ done |
| `worker/` — queue consumer, batch e-mail dispatch, xUnit tests | ✅ done |
| `docker/` — compose file for all 5 containers | 🔧 written, not yet verified end to end |
| `k8s/` — manifests for a local cluster | ⏳ planned |

## Running everything with Docker

```bash
cd docker
docker compose up --build
```

This builds `Promo.Api`/`Promo.Worker` from the repository root (each Dockerfile builds `components/` first to populate the local NuGet feed, then publishes its own service against it) and brings up all five containers: SQL Server, RabbitMQ, Mailhog, the API, and the Worker. A one-shot `db-init` container runs `api/database/schema.sql` then `seed.sql` against SQL Server before the API/Worker start — safe to ignore its errors on a second `up` against an existing volume (the schema/seed already exist); run `docker compose down -v` first for a clean slate.

Once everything is healthy:

- API: `http://localhost:8080/swagger`
- RabbitMQ management UI: `http://localhost:15672` (`guest`/`guest`)
- Mailhog UI (every e-mail the Worker sent): `http://localhost:8025`

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/auth/token -H "Content-Type: application/json" -d '{"username":"julio"}' | jq -r .token)

curl -X POST http://localhost:8080/campaigns \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"Black Friday","buyerGroupId":"22222222-2222-2222-2222-222222222222"}'

curl -X POST http://localhost:8080/campaigns/<id-from-above>/publish -H "Authorization: Bearer $TOKEN"
```

Publishing should land three e-mails in Mailhog — one per buyer in the VIP Customers group seeded by `seed.sql` — within a few seconds.

## Tech stack

- **.NET 10**, C#, ASP.NET Core (Minimal APIs / Controllers)
- **SQL Server** (via Dapper), packaged behind an engine-agnostic repository interface
- **RabbitMQ** for asynchronous messaging
- **Mailhog** as a safe, local stand-in for a real SMTP provider
- **Docker** / **docker-compose** for local multi-container orchestration
- **Kubernetes** for cluster orchestration
- **NuGet**, packaged and served from a local folder feed (no nuget.org publish involved)

## Related experience

This project's shape — segment an audience, publish a campaign, process the fan-out asynchronously — is a smaller, public rebuild of production work I led as Tech Lead on a CRM team (campaign segmentation architecture migration, incremental delivery of a personalized-campaigns product). Details of that real-world work are in my résumé and LinkedIn, not in this repository.
