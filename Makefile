.PHONY: run rerun

run:
	 docker compose up -d

rerun:
	 docker compose down -v
	 docker compose build --no-cache
	 docker compose up -d
