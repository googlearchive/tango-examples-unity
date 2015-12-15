#!/bin/bash
short_hash=`git rev-parse --short HEAD`

echo `git name-rev $short_hash --tags | awk '{sub("tags/", ""); print $2}'`