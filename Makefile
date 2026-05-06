.PHONY: run dev logs migrate nuke seed

run:
	docker compose up -d

# WARNING: destroys all local data
nuke:
	docker compose --profile dev down -v
	docker compose --profile dev build --no-cache
	docker compose --profile dev up -d

dev:
	docker compose --profile dev up -d

logs:
	docker compose logs -f hydra.api

migrate:
	docker compose exec hydra.api dotnet ef database update

seed:
	docker compose exec hydra.api dotnet Hydra.Api.dll --seed
