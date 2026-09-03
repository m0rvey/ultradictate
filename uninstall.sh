#!/bin/bash

set -euo pipefail

APP_PATH="/Applications/UltraDictate.app"
AGENT_LABEL="com.local.ultradictate.agent"
AGENT_PLIST="$HOME/Library/LaunchAgents/$AGENT_LABEL.plist"

/bin/launchctl bootout "gui/$UID/$AGENT_LABEL" >/dev/null 2>&1 || true
/usr/bin/pkill -x UltraDictate >/dev/null 2>&1 || true
rm -f "$AGENT_PLIST"

if [[ -w /Applications ]]; then
    rm -rf "$APP_PATH"
else
    sudo rm -rf "$APP_PATH"
fi

printf 'UltraDictate удалён. История и локальная модель сохранены.\n'
