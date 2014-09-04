#!/bin/sh

if [ ! -n "$BH" ]; then
    echo '$BH is not defined, did you call source settings.sh?'
    exit 1
fi

cd "$BH" || exit 1

if [ -f "wmib.pid" ];then
# kill the core
  touch "restart.lock" || exit 1
  kill `cat "wmib.pid"` || exit 1
  sleep 2
  x=0
  while [ -f "wmib.pid" ]
  do
          x=`expr $x + 1`
          if [ "$x" -gt 10 ];then
                  echo "Unable to terminate the bot core process"
                  exit 1
          fi
          sleep 2
  done
  echo "Successfuly killed bot core"
fi

current_bouncer=0
while [ $current_bouncer -lt $bouncers ]
do
    current_bouncer=`expr $current_bouncer + 1`
    if [ -f "bouncer_$current_bouncer.pid" ];then
        echo "Closing bouncer $current_bouncer"
        kill `cat bouncer_$current_bouncer.pid`
        sleep 2
        if [ -f "bouncer_$current_bouncer.pid" ];then
            echo "failed, killing it"
            kill -9   `cat bouncer_$current_bouncer.pid` || exit 1
            if [ -f "bouncer_$current_bouncer.pid" ];then
                rm "bouncer_$current_bouncer.pid" || exit 1
            fi
        fi
    fi
done
echo "Cleaning up restart scripts and all other processes that were running as $botuser"
killall -9 -u $botuser
rm restart.lock
echo "Everything is down"

