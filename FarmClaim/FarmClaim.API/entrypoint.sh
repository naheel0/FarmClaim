#!/bin/bash
set -e

if getent hosts sqlserver >/dev/null 2>&1; then
  echo "Waiting for SQL Server..."
  until nc -z sqlserver 1433 2>/dev/null; do
    sleep 2
  done
  echo "SQL Server is ready."
fi

echo "Starting FarmClaim API..."
exec dotnet FarmClaim.API.dll
