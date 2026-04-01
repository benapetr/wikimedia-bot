#!/bin/sh

set -eu

configuration="Debug"

if [ "${1:-}" = "--rl" ]; then
    configuration="Release"
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet SDK is not installed"
    exit 1
fi

if [ ! -f libirc/libirc.csproj ]; then
    echo "Missing libirc submodule, run:"
    echo "  git submodule update --init --recursive"
    exit 1
fi

dotnet restore WMBot.sln
dotnet build WMBot.sln -p:Configuration="$configuration"

if [ ! -d bin/configuration ]; then
    cp -r configuration "bin/configuration"
fi

echo "Build completed."
echo "Artifacts are in ./bin"
echo "Run the bot with:"
echo "  dotnet ./bin/wmib.dll"
echo "Run the bouncer with:"
echo "  dotnet ./bin/WMBot.Bouncer.dll"
