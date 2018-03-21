#!/bin/bash

source build.sh

DEPLOY_PATH=${DEPLOY_PATH:-/opt/machine-web-api/}

pushd .. > /dev/null
rsync -vprogzlt --chown=root:www-data --delete-during --rsh="ssh -v -i $DEPLOY_CREDENTIALS" $BUILD_OUTPUT/package root@$DEPLOY_DESTINATION:$DEPLOY_PATH || exit 1
popd > /dev/null

ssh root@$DEPLOY_DESTINATION "systemctl restart machine-web-api"

