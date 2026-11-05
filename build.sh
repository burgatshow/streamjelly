#!/bin/sh
# Fordítás Dockerrel (a NAS-on vagy bármilyen gépen, ahol van Docker) – .NET SDK telepítése nélkül.
# Használat:  sh build.sh
# Eredmény:   ./dist/StreamJelly_1.5.0.0/  (ezt a mappát kell a Jellyfin plugins könyvtárába másolni)
set -e
cd "$(dirname "$0")"
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet publish Jellyfin.Plugin.StreamJelly.csproj -c Release -o /src/bin/publish
OUT=dist/StreamJelly_1.5.0.0
rm -rf "$OUT" && mkdir -p "$OUT"
cp bin/publish/Jellyfin.Plugin.StreamJelly.dll "$OUT"/
cp meta.json "$OUT"/
cp assets/thumb.png "$OUT"/
echo "Kész: $OUT"
ls -l "$OUT"
