#!/bin/zsh
# Sends one command to the Scene helper (run-scene.sh) and prints its ack.
# Verbs: colonists <n>, raid <n>, speed <0-4>, shot <file.png>, quit. State is dumped to $WORK/scene-state.json.
source "$(dirname "$0")/common.sh"
send "$WORK/scene.txt" "$WORK/scene-ack.txt" "$@"
