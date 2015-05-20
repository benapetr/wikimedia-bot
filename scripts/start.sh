#!/bin/bash

port[1]=6667
port[2]=6668
port[3]=6669
port[4]=6660
port[5]=6661

if [ ! -n "$BH" ]; then
    echo '$BH is not defined, did you call source settings.sh?'
    exit 1
fi

if [ `whoami` != "$botuser" ];then
    echo "You are `whoami` but you need to switch to be $botuser!!"
    exit 2
fi

cd "$BH" || exit 1

# let's check if bouncers are running or not
# we have 5 bouncers now

current_bouncer=0

while [ $current_bouncer -lt $bouncers ]
do
    current_bouncer=`expr $current_bouncer + 1`
    if [ ! -f "$BH/bouncer_$current_bouncer.pid" ];then
        echo "INFO: Starting bouncer $current_bouncer"
        nohup $BH/scripts/start_bouncer.sh $current_bouncer ${port[$current_bouncer]} 2>/dev/null &
    fi
done

sleep 2

# first we check if bot is running or not
if [ -f "$BH/wmib.pid" ];then
    echo "Error: bot is already running, you need to stop it first. If you are absolutely sure it's not running, then clean the pid file (wmib.pid in $BH)"
    exit 1
fi

echo "Starting bot core in nohup... (this can be a little spammy you know)"
nohup $BH/restart.sh 2>/dev/null &
echo "That's all folks"

exit 0
