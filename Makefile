# Use docker compose as default command
COMPOSE ?= docker compose

# --- Commands ---
.PHONY: run rerun

# Start project normally
run:
	$(COMPOSE) up -d

# Full reset: stop, remove volumes, rebuild, start
rerun:
	$(COMPOSE) down -v
	$(COMPOSE) build --no-cache
	$(COMPOSE) up -d
