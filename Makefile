# ---------- Config ----------
COMPOSE ?= docker compose
API_SERVICE ?= hydra.api
DB_SERVICE ?= postgres
REDIS_SERVICE ?= redis

# ---------- Common ----------
.PHONY: up down build rebuild api logs ps test psql redis bash clean clean-volumes

up:            ## Start all services (detached)
	$(COMPOSE) up -d

down:          ## Stop and remove containers (keep volumes)
	$(COMPOSE) down --remove-orphans

build:         ## Build images
	$(COMPOSE) build

rebuild:       ## Rebuild images without cache
	$(COMPOSE) build --no-cache

api:           ## Rebuild only API and restart it
	$(COMPOSE) up -d --no-deps --build $(API_SERVICE)

logs:          ## Tail API logs
	$(COMPOSE) logs -f $(API_SERVICE)

ps:            ## Show running containers and ports
	$(COMPOSE) ps

test:          ## Run unit tests
	dotnet test

psql:          ## Open psql in the Postgres container
	$(COMPOSE) exec $(DB_SERVICE) psql -U app -d hydra

redis:         ## Open redis-cli in the Redis container
	$(COMPOSE) exec $(REDIS_SERVICE) redis-cli

bash:          ## Shell into the API container
	$(COMPOSE) exec $(API_SERVICE) /bin/bash || $(COMPOSE) exec $(API_SERVICE) sh

clean:         ## Remove bin/obj & test outputs from working tree
	git rm -r --cached -f **/bin || true
	git rm -r --cached -f **/obj || true
	git rm -r --cached -f TestResults || true

clean-volumes: ## ⚠️ Stop stack and delete volumes (nukes DB data)
	$(COMPOSE) down -v
