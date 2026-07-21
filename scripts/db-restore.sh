#!/usr/bin/env bash
# Restore a Postgres backup pulled from Cloudflare R2.
#   Usage: scripts/db-restore.sh hydra-YYYYMMDD-HHMMSSZ.sql.gz
# WARNING: this overwrites the current database (dump was taken with --clean).
set -euo pipefail

[ $# -eq 1 ] || { echo "Usage: $0 <backup-filename.sql.gz>"; exit 1; }
FILE="$1"
TMP="/tmp/${FILE}"

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/.."   # repo root
set -a; . ./.env; set +a

echo "==> Downloading backups/${FILE} from R2..."
docker run --rm \
  -e AWS_ACCESS_KEY_ID="${CLOUDFLARER2__ACCESSKEYID}" \
  -e AWS_SECRET_ACCESS_KEY="${CLOUDFLARER2__SECRETACCESSKEY}" \
  -e AWS_DEFAULT_REGION=auto \
  -v /tmp:/data \
  amazon/aws-cli \
  --endpoint-url "https://${CLOUDFLARER2__ACCOUNTID}.r2.cloudflarestorage.com" \
  s3 cp "s3://${CLOUDFLARER2__BUCKETNAME}/backups/${FILE}" "/data/${FILE}"

echo "==> Restoring into '${POSTGRES_DB}' (overwrites current data)..."
gunzip -c "${TMP}" | docker compose exec -T postgres psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}"

rm -f "${TMP}"
echo "==> Restore complete."
