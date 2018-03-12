#!/bin/bash

source build.sh

DEPLOY_PATH=${DEPLOY_PATH:-/var/www/scriptureforge.org/machine/}

rsync -vprogzlt --chown=root:www-data --delete-during --rsh="ssh -v -i $DEPLOY_CREDENTIALS" $BUILD_OUTPUT/package root@$DEPLOY_DESTINATION:$DEPLOY_PATH || exit 1

ssh root@$DEPLOY_DESTINATION "service machine-web-api restart"

