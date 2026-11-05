#!/bin/sh
# Build with Docker (on the NAS or any machine with Docker), no .NET SDK needed.
# Usage:   sh build.sh
# Result:  ./dist/StreamJelly_1.6.0.0/  (copy this folder into the Jellyfin plugins directory)
set -e
cd "$(dirname "$0")"
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet publish Jellyfin.Plugin.StreamJelly.csproj -c Release -o /src/bin/publish
OUT=dist/StreamJelly_1.6.0.0
rm -rf "$OUT" && mkdir -p "$OUT"
cp bin/publish/Jellyfin.Plugin.StreamJelly.dll "$OUT"/
cp meta.json "$OUT"/
cp assets/thumb.png "$OUT"/
echo "Done: $OUT"
ls -l "$OUT"
