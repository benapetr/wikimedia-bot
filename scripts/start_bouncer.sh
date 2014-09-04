#!/bin/sh

if [ ! -n "$BH" ]; then
    echo '$BH is not defined, did you call source settings.sh?'
    exit 1
fi

cd "$BH" || exit 1

if [ $# -lt 2 ];then
    echo "You need to provide 2 parameters"
    exit 1
fi

mono bouncer.exe "$2" chat.freenode.net bouncer_$1.pid >> bnc_$1.log
rm -f bouncer_$1.pid
