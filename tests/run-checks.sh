#!/bin/zsh
# Automated in-game checks of Colonist Bar Plus (squad work tab, orders, combat posts, medical, rules, members,
# hotkeys, save round trip). Usage: tests/run-checks.sh [game args...]
# Starts a separate quick test game (own save data folder), runs every check once, writes PASS/FAIL lines to
# $WORK/results.txt, quits. Prints the lines that are not PASS; exit code 1 when a check failed or the run broke.
# Deploy Colonist Bar Plus first (deploy.sh): the checks run against the installed mod.
source "$(dirname "$0")/common.sh"

results="$WORK/results.txt"
rm -f "$results"
write_modsconfig "mrbabilo.radiusui.colonistbarplus.tests"
install_helper ColonistBarPlusTests RCBPlusTests
"$EXE" -quicktest -rcbtest="$results" -savedatafolder="$SANDBOX" -logFile "$WORK/checks.log" \
	-screen-fullscreen 0 -screen-width 1600 -screen-height 1000 "$@" > "$WORK/checks.stdout" 2>&1 &
pid=$!
for i in $(seq 1 150); do grep -q "# END" "$results" 2>/dev/null && break; kill -0 $pid 2>/dev/null || break; sleep 2; done
wait_exit $pid 15
restore_prefs
remove_helpers

echo "== checks ($results)"
grep -v "^PASS" "$results" 2>/dev/null || echo "no results: see $WORK/checks.log"
grep -E "Exception" "$WORK/checks.log" | head -5
grep -q "# END" "$results" 2>/dev/null && grep -q ", fail 0" "$results"
