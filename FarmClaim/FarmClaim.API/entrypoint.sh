#!/bin/bash
set -e

echo "Starting FarmClaim API..."
exec dotnet FarmClaim.API.dll
