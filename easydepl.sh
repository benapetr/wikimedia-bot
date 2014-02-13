#!/bin/bash

RED=$(tput setaf 1)
WARN=$(tput setaf 3)
GREEN=$(tput setaf 2)
NORMAL=$(tput sgr0)
target=/mnt/share/beta
parameters=/verbosity:quiet
source=/mnt/share/wikimedia-bot

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
  if [ "`tail -80 wmib.log | grep -cE '\[ERROR\]|\[WARNING\]'`" -qt 0 ];then
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

text "Building module wmib_rc"
cd plugins/wmib_rc || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module wmib_logs"
cd ../wmib_log || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module htmldump"
cd ../htmldump || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module blah"
cd ../slap || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module wmib_notify"
cd ../wmib_notify || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module seen"
cd ../seen || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module linkie"
cd ../Linkie || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module labs"
cd ../labs || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module op"
cd ../op || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module netcat"
cd ../NetCat || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module request"
cd ../request || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module wmib_infobot"
cd ../wmib_infobot || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module thanks"
cd ../Thanks || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module wmib_infobot"
cd ../wmib_infobot || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module wmib_rafeed"
cd ../wmib_rafeed || exit 1
xbuild > /dev/null || exit 1
ok
text "Building module wmib_statistics"
cd ../wmib_statistics || exit 1
xbuild > /dev/null || exit 1
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

text "Updating the binary file"

cp "$source/bin/Debug/wmib.exe" "$target/wmib.exe" || exit 1
cp "$source/plugins/seen/bin/Debug/plugin.dll" "$target/modules/seen.bin" || exit 1
cp "$source/plugins/htmldump/bin/Debug/htmldump.dll" "$target/modules/htmldump.bin" || exit 1
cp "$source/plugins/wmib_log/bin/Debug/wmib_log.dll" "$target/modules/wmib_logs.bin" || exit 1
cp "$source/plugins/labs/bin/Debug/labs.dll" "$target/modules/labs.bin" || exit 1
cp "$source/plugins/Linkie/bin/Debug/Linkie.dll" "$target/modules/linkie.bin" || exit 1
cp "$source/plugins/op/bin/Debug/op.dll" "$target/modules/op.bin" || exit 1
cp "$source/plugins/slap/bin/Debug/slap.dll" "$target/modules/slap.bin" || exit 1
cp "$source/plugins/wmib_infobot/bin/Debug/wmib_infobot.dll" "$target/modules/wmib_infobot.bin" || exit 1
cp "$source/plugins/Thanks/bin/Debug/Thanks.dll" "$target/modules/thanks.bin" || exit 1
cp "$source/plugins/wmib_rc/bin/Debug/wmib_rc.dll" "$target/modules/wmib_rc.bin" || exit 1
cp "$source/plugins/wmib_statistics/bin/Debug/wmib_statistics.dll" "$target/modules/wmib_statistics.bin" || exit 1
cp "$source/plugins/request/bin/Debug/request.dll" "$target/modules/request.bin" || exit 1
cp "$source/plugins/NetCat/bin/Debug/NetCat.dll" "$target/modules/NetCat.bin" || exit 1
cp "$source/plugins/wmib_rafeed/bin/Debug/wmib_rafeed.dll" "$target/modules/wmib_rafeed.bin" || exit 1
cp "$source/plugins/wmib_notify/bin/Debug/wmib_notify.dll" "$target/modules/wmib_notify.bin" || exit 1
ok
text "Restarting the bot"
rm "$target/restart.lock" || exit 1
ok
