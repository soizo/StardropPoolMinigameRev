#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/StardropPoolMinigameRev.csproj"
BUILD_DIR="$ROOT/bin/Release/net6.0"
PACKAGE_ROOT="$ROOT/build/StardropPoolMinigameRev"
DLL_DIR="$PACKAGE_ROOT/StardropPoolMinigameRev"
CP_DIR="$PACKAGE_ROOT/[CP] StardropPoolMinigameRev"
CP_SOURCE="$ROOT/[CP] StardropPoolMinigameRev"

rm -rf "$PACKAGE_ROOT"
mkdir -p "$DLL_DIR"

dotnet build "$PROJECT" -c Release

cp "$ROOT/manifest.json" "$DLL_DIR/manifest.json"
cp "$BUILD_DIR/StardropPoolMinigameRev.dll" "$DLL_DIR/StardropPoolMinigameRev.dll"
if [ -f "$BUILD_DIR/StardropPoolMinigameRev.pdb" ]; then
  cp "$BUILD_DIR/StardropPoolMinigameRev.pdb" "$DLL_DIR/StardropPoolMinigameRev.pdb"
fi

# The DLL mod keeps a copy of the two PNGs as a cross-platform fallback. Content
# Patcher should provide these as game assets, but the minigame can still render
# if the CP pack is missing or has not registered yet.
mkdir -p "$DLL_DIR/Assets/Tilesheets"
cp "$CP_SOURCE/Assets/Tilesheets/stardropPool.png" "$DLL_DIR/Assets/Tilesheets/stardropPool.png"
cp "$CP_SOURCE/Assets/Tilesheets/stardropPoolFont.png" "$DLL_DIR/Assets/Tilesheets/stardropPoolFont.png"

# SMAPI stops descending into a folder once it finds a manifest.json, so the DLL
# mod and its Content Patcher pack must be sibling subfolders under a parent that
# has no manifest.json.
cp -R "$CP_SOURCE" "$CP_DIR"
find "$PACKAGE_ROOT" -name '.DS_Store' -delete
