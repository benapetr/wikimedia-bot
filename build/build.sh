#!/bin/sh

p="/p:Configuration=Release"

if [ "$1" = "--debug" ]; then
    p="/p:Configuration=Debug"
fi

echo "Checking all required packages..."

if [ "`which xbuild`" = "" ];then
	echo "mono-xbuild is not installed!"
	exit 1
fi

xbuild "$p" || exit 1

if [ ! -d bin/configuration ];then
    cp -r configuration "bin/configuration"
fi

echo "Everything was built, you can start bot by typing"
echo "this is terminal:"
echo cd bin
echo mono wmib.exe

