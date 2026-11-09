#!/usr/bin/env bash
# Installs the latest PolitiCities build into the local Cities: Skylines mods
# folder on SteamOS/Linux. Run on the Steam Deck (Desktop Mode terminal or SSH):
#
#   curl -fsSL https://raw.githubusercontent.com/JoeRourke123/PolitiCities/main/scripts/install-on-steamdeck.sh | bash
#
# or with a local zip:
#
#   ./install-on-steamdeck.sh /path/to/PolitiCities-v0.1.0.zip
#
# or via a GitHub release download URL / gh CLI artifact:
#
#   POLITICITIES_URL=https://github.com/JoeRourke123/PolitiCities/releases/download/v0.1.0/PolitiCities-v0.1.0.zip ./install-on-steamdeck.sh
set -euo pipefail

REPO="JoeRourke123/PolitiCities"
MOD="PolitiCities"

log() { printf '\033[1;32m[politicities]\033[0m %s\n' "$*"; }
err() { printf '\033[1;31m[politicities]\033[0m %s\n' "$*" >&2; }

resolve_mods_dir() {
  local native="$HOME/.local/share/Colossal Order/Cities_Skylines/Addons/Mods"
  if [ -d "$HOME/.local/share/Colossal Order/Cities_Skylines" ] || [ -d "$native" ]; then
    echo "$native"
    return 0
  fi
  local proton
  proton=$(find "$HOME/.local/share/Steam/steamapps/compatdata/255710" -type d -name Mods -path "*Addons*" 2>/dev/null | head -1 || true)
  if [ -n "$proton" ]; then
    echo "$proton"
    return 0
  fi
  return 1
}

fetch_zip() {
  local out="$1"
  if [ -n "${SCRIPT_ARG_ZIP:-}" ] && [ -f "$SCRIPT_ARG_ZIP" ]; then
    cp "$SCRIPT_ARG_ZIP" "$out"
    log "using local zip: $SCRIPT_ARG_ZIP"
    return 0
  fi
  if [ -n "${POLITICITIES_URL:-}" ]; then
    log "downloading $POLITICITIES_URL"
    curl -fsSL "$POLITICITIES_URL" -o "$out"
    return 0
  fi
  if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
    log "downloading latest Build artifact via gh"
    local run
    run=$(gh run list --repo "$REPO" --branch main --workflow Build --limit 1 --json databaseId -q '.[0].databaseId')
    gh run download "$run" --repo "$REPO" -n "$MOD" -D "$(dirname "$out")"
    mv "$(dirname "$out")/$MOD.zip" "$out" 2>/dev/null || true
    [ -f "$out" ] && return 0
  fi
  log "trying latest GitHub release asset"
  local url
  url=$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" | grep -o '"browser_download_url": *"[^"]*"' | head -1 | cut -d'"' -f4 || true)
  if [ -n "$url" ]; then
    log "downloading $url"
    curl -fsSL "$url" -o "$out"
    return 0
  fi
  err "could not obtain a build zip. Options:"
  err "  1) pass a local zip:  $0 /path/to/PolitiCities-*.zip"
  err "  2) set POLITICITIES_URL to a release/artifact URL"
  err "  3) tag a release on GitHub (git tag v0.1.0 && git push origin v0.1.0) so the Release workflow attaches the zip"
  return 1
}

SCRIPT_ARG_ZIP="${1:-}"

MODS_DIR=$(resolve_mods_dir) || { err "could not find a Cities: Skylines Addons/Mods folder (game not installed for this user, or not run yet once?)"; exit 1; }
log "mods folder: $MODS_DIR"
mkdir -p "$MODS_DIR"

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

ZIP="$TMP/politicities.zip"
fetch_zip "$ZIP"

log "extracting"
unzip -q "$ZIP" -d "$TMP/x"

DLL_DIR=$(find "$TMP/x" -name "$MOD.dll" -printf '%h\n' | head -1 || true)
[ -n "$DLL_DIR" ] || { err "no $MOD.dll found inside the zip"; exit 1; }
log "found $MOD.dll in $(basename "$DLL_DIR")"

log "installing to $MODS_DIR/$MOD"
mkdir -p "$MODS_DIR/$MOD"
cp -rf "$DLL_DIR/." "$MODS_DIR/$MOD/"

if [ ! -f "$MODS_DIR/$MOD/$MOD.dll" ]; then
  err "install failed: $MODS_DIR/$MOD/$MOD.dll missing"
  exit 1
fi
log "installed files:"
ls -la "$MODS_DIR/$MOD"

cat <<EOF

--------------------------------------------------------------
PolitiCities installed. To enable it in-game (one-time):
  1. Launch Cities: Skylines, choose PLAY (not Resume)
  2. Main menu -> Content Manager -> Mods tab
  3. Tick "PolitiCities" (and any other mods you want)
  4. CitiesHarmony (workshop item 2040656402) is auto-subscribed
     at runtime by the mod; subscribe manually if you run with
     -noWorkshop.
  5. Load/new a city, press P for the PolitiCities council panel.
Enable state is stored by the game in userGameState.cgs /
per-save ModsList XML, so it is done in the Content Manager GUI
rather than by this script.
--------------------------------------------------------------
EOF
