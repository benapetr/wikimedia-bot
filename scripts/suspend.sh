#!/bin/sh

if [ ! -n "$BH" ]; then
    echo '$BH is not defined, did you call source settings.sh?'
    exit 1
fi

cd "$BH" || exit 1

