#!/bin/sh

if [ ! -d .git ];then
    echo "This must be ran from repository root"
    exit 1
fi

if [ ! -d bin ];then
    echo "Can't find binaries, did you build the bot using Visual Studio?"
    exit 1
fi

echo "Enter version"
read version

target=wm-bot_"$version"

echo "Going to create $target.tar.gz, press enter to continue"
read p

mkdir "$target" || exit 1
cp bin/*.exe "$target/"
cp bin/*.dll "$target/"
mkdir "$target/modules"
cp bin/mods/WMBot.*.dll "$target/modules/"

tar -zcf "$target.tar.gz" $target
