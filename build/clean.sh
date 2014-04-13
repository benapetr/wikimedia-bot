#!/bin/sh
force=0

if [ x"$1" = x"--force" ];then
  force=1
fi

if [ -d bin ];then
if [ $force -eq 0 ];then
  if [ -d bin/configuration ];then
    echo "Not removing bin because there is a data folder, use make forced-clean if you really want to delete it"
    exit 1
  fi
fi

rm -rv bin
fi

