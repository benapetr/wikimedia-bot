#!/bin/sh

p="/p:Configuration=Release"
folder=Release

if [ "$1" = "--debug" ]; then
    folder=Debug
    p="/p:Configuration=Debug"
fi

echo "Checking all required packages..."

if [ "`which xbuild`" = "" ];then
	echo "mono-xbuild is not installed!"
	exit 1
fi

xbuild "$p" || exit 1

if [ ! -d bin/Debug ];then
    mkdir bin/Debug
fi

if [ ! -d bin/Release/configuration ];then
    cp -r configuration "bin/Release/configuration"
fi

if [ ! -f "bin/Debug/wmib.exe" ];then
    cp bin/Release/wmib.exe bin/Debug
fi

if [ ! -d bin/Debug/configuration ];then
    cp -r configuration bin/Debug/configuration
fi

cp *.dll bin/Release
cp *.dll bin/Debug

echo "Everything was built, you can start bot by typing"
echo "this is terminal:"
echo cd bin/Debug
echo mono wmib.exe
