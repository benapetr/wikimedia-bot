#!/bin/bash

RED=$(tput setaf 1)
WARN=$(tput setaf 3)
GREEN=$(tput setaf 2)
NORMAL=$(tput sgr0)
target=`pwd`
parameters=/verbosity:quiet
source=/opt/repo/wikimedia-bot

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

if [ ! -d "$target" ];then
  echo "Can't find $target"
  exit 1
fi

cd "$target" || exit 1

if [ -f wmib.log ];then
  if [ "`tail -80 wmib.log | grep -cE '\[ERROR\]|\[WARNING\]'`" -gt 0 ];then
    echo "Please check your log at wmib.log there are some errors:"
    tail -80 wmib.log | grep -E '\[ERROR\]|\[WARNING\]'
    exit 1
  fi
fi

if [ ! -d "$source" ];then
  echo "Can't find $source"
  exit 1
fi

cd "$source" || exit 1
text "Cleaning the source code"

make forced-clean > /dev/null || exit 1

ok
text "Building the new code..."
make > /dev/null || exit 1
ok

text "Stopping the bot"
touch "$target/restart.lock" || exit 1
if [ -f "$target/wmib.pid" ];then
  kill `cat "$target/wmib.pid"` || exit 1
  sleep 2
fi
x=0
while [ -f "$target/wmib.pid" ]
do
        x=`expr $x + 1`
        if [ "$x" -gt 10 ];then
                fail
                exit 1
        fi
        sleep 2
done
ok

if [ ! -d "$target/configuration" ];then
  mkdir "$target/configuration"
fi

text "Updating the binary file"
cp "$source/bin/wmib.exe" "$target/wmib.exe" || exit 1
cp $source/bin/*.mdb "$target/"
cp "$source/configuration/sites" "$target/sites" || exit 1
cp "$source/configuration/linkie" "$target/configuration/linkie" || exit 1
cp `ls $source/bin/modules_debug/*.dll | grep -v Plugins.dll` "$target/" || exit 1
cp "$source"/bin/*.dll "$target/" || exit 1
cp "$source"/bin/modules_debug/*Plugins.dll "$target/modules" || exit 1
cp "$source"/bin/debug/modules/*Plugins.dll "$target/modules" || exit 1
ok

text "Restarting the bot"
rm "$target/restart.lock" || exit 1
ok
