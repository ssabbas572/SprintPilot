#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore SprintPilot.sln
(cd frontend && npm ci)
dotnet run --project src/SprintPilot.Api --launch-profile SprintPilot.Api &
api_pid=$!
trap 'kill "$api_pid" 2>/dev/null || true' EXIT
printf 'Open http://localhost:5176 when Vite is ready. Ctrl+C stops the demo.
'
(cd frontend && npm run dev)
