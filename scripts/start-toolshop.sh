#!/usr/bin/env bash
#
# Starts the self-hosted Toolshop (docker/toolshop.compose.yml) and returns once it is ready
# for the tests: API up, catalog seeded, UI served. Safe to call when the shop already runs.
#
# Used by the pipeline (job "ui"), by the pre-push hook and by hand:
#   scripts/start-toolshop.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE="$ROOT/docker/toolshop.compose.yml"
API="http://localhost:8091"
UI="http://localhost:4200"

wait_for() { # url, attempts (5 s apart)
  for _ in $(seq 1 "$2"); do
    curl -fsS "$1" >/dev/null 2>&1 && return 0
    sleep 5
  done
  return 1
}

# '"data":[{' requires at least one product, not just an empty "data":[] array.
has_products() { curl -fsS "$API/products" 2>/dev/null | grep -q '"data":\[{'; }

echo "toolshop: starting containers (reused if already up)..."
docker compose -f "$COMPOSE" up -d >/dev/null

echo "toolshop: waiting for the API..."
wait_for "$API/status" 60 || { echo "toolshop: the API did not come up."; exit 1; }

# The database may not be ready the instant the API answers, so seeding is retried until the
# catalog really contains products. A shop that already has data is left untouched.
echo "toolshop: checking the catalog..."
for _ in $(seq 1 6); do
  has_products && break
  echo "toolshop: seeding the database..."
  docker compose -f "$COMPOSE" exec -T laravel-api php artisan migrate:fresh --seed --force >/dev/null 2>&1 || true
  sleep 5
done
if ! has_products; then
  echo "toolshop: the database has no products after seeding."
  echo "          Inspect with: docker compose -f docker/toolshop.compose.yml logs"
  exit 1
fi

# The UI container builds the Angular app on start; until that is done nothing is served.
echo "toolshop: waiting for the UI..."
wait_for "$UI" 180 || { echo "toolshop: the UI did not come up."; exit 1; }

echo "toolshop: ready (UI $UI, API $API)."
