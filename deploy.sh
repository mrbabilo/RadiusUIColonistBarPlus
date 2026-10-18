#!/bin/sh
# Build RadiusColonistBar and publish this folder (minus sources and docs) to RimWorld's Mods folder.
set -eu

HERE=$(cd "$(dirname "$0")" && pwd)
GAME="/Volumes/BABILOGAMES/JEUX EN COURS/RimWorld.app"
MODS="$GAME/Mods"
DEST="$MODS/RadiusUIColonistBarPlus"

[ -d "$MODS" ] || { echo "Dossier Mods du jeu introuvable (volume BABILOGAMES non monté ?) : $MODS" >&2; exit 1; }

# Build out of tree so Source/ never gets bin/ or obj/.
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
dotnet build "$HERE/Source/RadiusColonistBar.csproj" -c Release -nologo -v q \
	-o "$TMP/build" -p:BaseIntermediateOutputPath="$TMP/obj/"

cp "$TMP/build/RadiusColonistBar.dll" "$HERE/Assemblies/"
rsync -a --delete --exclude Source/ --exclude docs/ --exclude tests/ --exclude deploy.sh --exclude .DS_Store --exclude .git/ --exclude .gitignore --exclude .superpowers/ --exclude .remember/ "$HERE/" "$DEST/"
echo "Publié : $DEST"
