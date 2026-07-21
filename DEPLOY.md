# Deploying the Hydra / Local Bee API

The API ships as a Docker Compose stack (Caddy + .NET API + Postgres + Redis).
Caddy terminates TLS and is the only thing exposed to the internet. These steps
target a plain Ubuntu 24.04 VPS (Contabo, Hetzner, etc.).

## 0. Prerequisites
- A domain with an **A record** `api.yourdomain.com` → your server's public IP.
- A server (2 vCPU / 4–8 GB RAM, NVMe, EU location). Ubuntu 22.04 or 24.04.

## 1. Harden the box (run as root on first login)
```bash
# create a sudo user and copy your SSH key to it
adduser deploy && usermod -aG sudo deploy
rsync --archive --chown=deploy:deploy ~/.ssh /home/deploy   # or ssh-copy-id from your laptop

# host firewall — Contabo has no cloud firewall, so use ufw
apt update && apt install -y ufw
ufw allow OpenSSH && ufw allow 80 && ufw allow 443
ufw --force enable

# lock down SSH: disable root login + password auth (do this AFTER confirming key login works)
sed -i 's/^#\?PermitRootLogin.*/PermitRootLogin no/; s/^#\?PasswordAuthentication.*/PasswordAuthentication no/' /etc/ssh/sshd_config
systemctl restart ssh
```
Re-connect as `deploy` before closing the root session, to confirm key login works.

## 2. Install Docker
```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER   # log out/in so this takes effect
```

## 3. Get the code and configure
```bash
sudo mkdir -p /opt/hydra && sudo chown $USER /opt/hydra
git clone <repo-url> /opt/hydra && cd /opt/hydra
bash scripts/setup.sh           # creates .env from .env.example, locks perms
nano .env                       # fill in EVERY value (see below)
```
Required `.env` values:
- `DOMAIN=api.yourdomain.com`
- Strong `POSTGRES_PASSWORD`, `REDIS_PASSWORD`, `JWT_SECRET` (32+ chars)
- `RESEND_API_KEY`, `GOOGLEAUTH__CLIENTID`
- `CLOUDFLARER2__*` (bucket + API token + public domain)
- `CORS_ALLOWED_ORIGINS=https://<your-vercel-frontend-url>` (comma-separated)
- `SUPERADMIN_EMAIL` / `SUPERADMIN_PASSWORD` (your first login)

## 4. Launch
```bash
docker compose up -d --build
docker compose logs -f hydra.api    # watch startup: migrations run, SuperAdmin seeds
```
Caddy fetches the TLS cert automatically once DNS points at the box.

## 5. Verify
```bash
curl https://api.yourdomain.com/health          # -> Healthy
```
Log in from the frontend with your `SUPERADMIN_EMAIL` / `SUPERADMIN_PASSWORD`,
then create venue types and venues.

## 6. Backups (free — Postgres → Cloudflare R2)
The DB is the only irreplaceable state (photos are already on R2, code on GitHub).
```bash
chmod +x scripts/db-backup.sh scripts/db-restore.sh
crontab -e
# nightly at 03:00 UTC:
0 3 * * * /opt/hydra/scripts/db-backup.sh >> /var/log/hydra-backup.log 2>&1
```
Set a **lifecycle rule** on the R2 bucket to expire the `backups/` prefix after
14–30 days (Cloudflare → R2 → bucket → Settings). Restore with:
```bash
scripts/db-restore.sh hydra-YYYYMMDD-HHMMSSZ.sql.gz
```

## 7. Updating / redeploying
```bash
cd /opt/hydra && git pull
docker compose up -d --build       # env-only changes: drop --build
```

## Troubleshooting
- **TLS cert fails:** DNS A record must resolve to this IP, and ports 80/443 open
  (ufw). If the domain is on Cloudflare, keep `api.*` **DNS-only (grey cloud)**.
- **Frontend gets CORS errors:** the frontend origin must be in `CORS_ALLOWED_ORIGINS`.
- **Can't log in on a fresh DB:** check the startup logs seeded the SuperAdmin;
  the email/password come from `SUPERADMIN_EMAIL` / `SUPERADMIN_PASSWORD`.
