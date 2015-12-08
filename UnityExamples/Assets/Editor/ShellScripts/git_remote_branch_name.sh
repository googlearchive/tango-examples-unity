#!/bin/bash
function git.branch {
    git show-ref | grep `git rev-parse HEAD` | grep remotes | sed -e 's/.*remotes.origin.//'
}
git.branch
