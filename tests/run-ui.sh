#!/bin/zsh
# Interactive UI run: builds a squad scene, then serves commands sent with tests/cmd.sh (open the order card, switch
# tabs, inject clicks, take screenshots…). Usage: tests/run-ui.sh [game args...]
# Returns when the game exits ("tests/cmd.sh quit"). Injected right clicks and wheel only reach the game's own
# windows partly: check those with the real mouse.
source "$(dirname "$0")/common.sh"

write_modsconfig "mrbabilo.radiusui.colonistbarplus.tests"
install_helper ColonistBarPlusTests RCBPlusTests
rm -f "$WORK/cmd.txt" "$WORK/ack.txt"
"$EXE" -quicktest -rcbui="$WORK" -savedatafolder="$SANDBOX" -logFile "$WORK/ui.log" \
	-screen-fullscreen 0 -screen-width 1600 -screen-height 1000 "$@" > "$WORK/ui.stdout" 2>&1 &
pid=$!
for i in $(seq 1 90); do [ -f "$WORK/ack.txt" ] && break; sleep 2; done
cat "$WORK/ack.txt" 2>/dev/null; echo
wait_exit $pid 86400
restore_prefs
remove_helpers
echo "ui run closed"
