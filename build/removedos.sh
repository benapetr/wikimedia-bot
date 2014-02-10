#!/bin/bash

for file in `find . | grep -E '\.cs$'`
do
	dos2unix "$file" 
done
