#!/usr/bin/env bash
# Nightly Postgres -> Cloudflare R2 backup.
# Run from the repo root (has docker-compose.yml + .env). Schedule via host cron.
#
#   0 3 * * *  /opt/hydra/scripts/db-backup.sh >> /var/log/hydra-backup.log 2>&1
#
# Retention: set a lifecycle rule on the R2 bucket to expire the "backups/"
# prefix after N days (Cloudflare dashboard -> R2 -> bucket -> Settings).
set -euo pipefail

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/.."   # repo root
set -a; . ./.env; set +a

STAMP="$(date -u +%Y%m%d-%H%M%SZ)"
FILE="hydra-${STAMP}.sql.gz"
TMP="/tmp/${FILE}"

echo "==> Dumping database '${POSTGRES_DB}'..."
docker compose exec -T postgres \
  pg_dump -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" --no-owner --clean --if-exists \
  | gzip -9 > "${TMP}"
echo "    wrote ${TMP} ($(du -h "${TMP}" | cut -f1))"

echo "==> Uploading to s3://${CLOUDFLARER2__BUCKETNAME}/backups/${FILE} ..."
docker run --rm \
  -e AWS_ACCESS_KEY_ID="${CLOUDFLARER2__ACCESSKEYID}" \
  -e AWS_SECRET_ACCESS_KEY="${CLOUDFLARER2__SECRETACCESSKEY}" \
  -e AWS_DEFAULT_REGION=auto \
  -v /tmp:/data \
  amazon/aws-cli \
  --endpoint-url "https://${CLOUDFLARER2__ACCOUNTID}.r2.cloudflarestorage.com" \
  s3 cp "/data/${FILE}" "s3://${CLOUDFLARER2__BUCKETNAME}/backups/${FILE}"

rm -f "${TMP}"
echo "==> Backup complete: backups/${FILE}"
