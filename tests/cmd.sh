#!/bin/zsh
# Sends one command to ColonistBarPlusTests (run-ui.sh, or run-scene.sh) and prints its ack.
# Verbs (screen coordinates in UI pixels):
#   squadall                 every colonist into one squad (the squad the other verbs act on)
#   opencard <x> <y>         order card anchored at x, y        closecard, opencc, closeall
#   settab <0|1|2>           Combat, Rules, Work                setworknow <0|1>, setmanual <0|1>
#   setprio <pawn> <0-4>     plant cutting priority of a member (by short label)
#   draft, undraft, assault <mode 0-2>, stopassault, deselect
#   click|rclick <x> <y>, wheel <x> <y> <delta>, hover <x> <y>, hoveroff
#   fluency <tab 0|1|2> [frames]  smoothness probe, card closed vs open; result in $WORK/fluency-tab<n>.txt
#   shot <file.png>, quit
# State is dumped to $WORK/state.json.
source "$(dirname "$0")/common.sh"
send "$WORK/cmd.txt" "$WORK/ack.txt" "$@"
