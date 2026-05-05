# HydraStack

Booking API for restaurants and boat services on Hydra island.

## Stack

- ASP.NET Core 8 Web API
- PostgreSQL 16
- Redis 7
- Docker / Docker Compose

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Quickstart

```bash
cp .env.example .env
# Edit .env and set real values for all variables

make dev        # start everything locally (API + Postgres + Redis, ports exposed)
# OR
make run        # start API only (expects external Postgres/Redis via env vars)
```

Swagger UI is available at `http://localhost:8080/swagger` when `ASPNETCORE_ENVIRONMENT=Development`.

## Make targets

| Target | Description |
|--------|-------------|
| `run` | `docker compose up -d` — starts the API only |
| `dev` | `docker compose --profile dev up -d` — starts API + Postgres + Redis with host ports exposed |
| `logs` | Tail logs for the API container |
| `migrate` | Run EF Core migrations inside the running API container |
| `nuke` | **WARNING: destroys all local data.** Tears down volumes, rebuilds images from scratch, and restarts in dev mode |

## API

- Base URL: `http://localhost:8080/api/v1`
- Swagger: `http://localhost:8080/swagger`
- Auth: JWT Bearer — obtain a token via `POST /api/v1/auth/login` and pass it as `Authorization: Bearer <token>`
