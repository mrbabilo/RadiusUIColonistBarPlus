#!/bin/zsh
# Shared settings for the in-game test scripts. Sourced, not run.

TESTS=$(cd "$(dirname "$0")" && pwd)
GAME="/Volumes/BABILOGAMES/JEUX EN COURS/RimWorld.app"
MODS="$GAME/Mods"
EXE="$GAME/Contents/MacOS/RimWorld by Ludeon Studios"
# Work folder: sandbox save data (-savedatafolder), logs, results, command files.
WORK=${RCBTEST_WORK:-${TMPDIR:-/tmp}/rcbtest}
SANDBOX="$WORK/sandbox"
PLIST_BAK="$WORK/ludeon.rimworld.plist.bak"

[ -d "$MODS" ] || { echo "Mods folder not found (BABILOGAMES volume not mounted?): $MODS" >&2; exit 1; }
mkdir -p "$SANDBOX/Config"
# The checks read the game's English messages: a fresh sandbox would start in the system language.
[ -f "$SANDBOX/Config/Prefs.xml" ] || printf '<?xml version="1.0" encoding="utf-8"?>\n<PrefsData>\n  <langFolderName>English</langFolderName>\n</PrefsData>\n' > "$SANDBOX/Config/Prefs.xml"

# Unity keeps the window size and fullscreen flag in the player's own preferences, shared with their game:
# save them once, put them back after every run.
[ -f "$PLIST_BAK" ] || defaults export ludeon.rimworld "$PLIST_BAK"

restore_prefs() {
	defaults import ludeon.rimworld "$PLIST_BAK"
}

# install_helper <folder under tests/> <assembly name>: builds the helper mod against the installed Colonist Bar
# Plus and copies it into Mods for this run.
install_helper() {
	local src="$TESTS/$1" asm=$2 dest="$MODS/RCBPlus-$1"
	dotnet build "$src/Source/$asm.csproj" -c Release -nologo -v q \
		-o "$WORK/build-$1" -p:BaseIntermediateOutputPath="$WORK/obj-$1/" >/dev/null || { echo "$1 build failed" >&2; exit 1; }
	rm -rf "$dest"
	mkdir -p "$dest/Assemblies"
	cp -R "$src/About" "$dest/"
	cp "$WORK/build-$1/$asm.dll" "$dest/Assemblies/"
}

remove_helpers() {
	rm -rf "$MODS/RCBPlus-ColonistBarPlusTests" "$MODS/RCBPlus-Scene"
}

# write_modsconfig "<package ids after Colonist Bar Plus>": Harmony, Core, the framework, Colonist Bar Plus, then
# the listed ids (test helpers, DPA), in that load order.
write_modsconfig() {
	local mods="<li>brrainz.harmony</li><li>ludeon.rimworld</li><li>astryl.radiusui.framework</li><li>mrbabilo.radiusui.colonistbarplus</li>" m
	for m in ${=1}; do mods="$mods<li>$m</li>"; done
	cat > "$SANDBOX/Config/ModsConfig.xml" <<XML
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData><version>1.6.4633 rev1253</version><activeMods>$mods</activeMods>
<knownExpansions><li>ludeon.rimworld</li><li>ludeon.rimworld.royalty</li><li>ludeon.rimworld.ideology</li><li>ludeon.rimworld.biotech</li><li>ludeon.rimworld.anomaly</li><li>ludeon.rimworld.odyssey</li></knownExpansions></ModsConfigData>
XML
}

# Waits for the game process to exit, killing it after the given number of seconds.
wait_exit() {
	local pid=$1 limit=$2 i
	for i in $(seq 1 $limit); do kill -0 $pid 2>/dev/null || return 0; sleep 1; done
	kill $pid 2>/dev/null
	sleep 2
}

# send <file> <ack file> <words...>: writes "<id> <words>" to the command file and waits for the matching ack.
send() {
	local cmd=$1 ack=$2 id=$(date +%s%N) a i
	shift 2
	echo "$id $*" > "$cmd"
	for i in $(seq 1 150); do
		[ -f "$ack" ] && a=$(cat "$ack") && [ "${a%% *}" = "$id" ] && { echo "${a#* }"; return 0; }
		sleep 0.2
	done
	echo "TIMEOUT ($*)"; return 1
}
