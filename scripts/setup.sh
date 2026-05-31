#!/usr/bin/env bash
# Initial server setup script for Hetzner deployment.
# Run once as root (or a sudo user) after cloning the repo.
set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/.."
cd "$REPO_DIR"

echo "==> Creating .env from .env.example"
if [ -f .env ]; then
  echo "    .env already exists — skipping copy. Edit it manually if needed."
else
  cp .env.example .env
  echo "    Created .env — fill in all values before starting the stack."
fi

echo "==> Locking down .env permissions (owner read/write only)"
chmod 600 .env
echo "    $(stat -c '%a %n' .env)"

echo "==> Creating logs directory"
mkdir -p Hydra.Api/logs
chmod 755 Hydra.Api/logs

echo ""
echo "Next steps:"
echo "  1. Edit .env with your real values (domain, passwords, API keys, CORS origins)"
echo "  2. Ensure ports 80 and 443 are open in the Hetzner Cloud firewall"
echo "  3. Point your A record to this server's IP address"
echo "  4. Run: docker compose up -d --build"
echo "  5. Watch Caddy obtain a TLS cert: docker compose logs caddy -f"
