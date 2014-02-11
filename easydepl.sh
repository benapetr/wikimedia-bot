#!/bin/bash

RED=$(tput setaf 1)
WARN=$(tput setaf 3)
GREEN=$(tput setaf 2)
NORMAL=$(tput sgr0)
target=/mnt/share/beta
source=/mnt/share/wikimedia_bot

ok()
{
    printf '%s%s%s\n' "$GREEN" "[OK]" "$NORMAL"
}

fail()
{
    printf '%s%s%s\n' "$RED" "[FAIL]" "$NORMAL"
}

text()
{
    MSG="$1"
    let COL=$(tput cols)-20-${#MSG}+${#GREEN}+${#NORMAL}
    printf '%s%*s' "$MSG" $COL
}


cd "$source" || exit 1
echo "Cleaning the source code"

make forced-clean || exit 1

echo "Building the new code..."

make || exit 1

text "Stopping the bot"

touch "$target/restart.lock" || exit 1
if [ -f "$target/wmib.pid" ];then
  kill `cat "$target/wmib.pid"` || exit 1
  sleep 2
fi
if [ -f "$target/wmib.pid" ];then
  fail
  exit 1
fi

ok

text "Updating the binary file"
cp "$source/bin/Debug/wmib.exe" "$target/wmib.exe" || exit 1
ok
text "Restarting the bot"
rm "$target/restart.lock" || exit 1
ok
