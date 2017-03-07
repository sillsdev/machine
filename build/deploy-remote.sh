#!/bin/bash

source deploy-local.sh

rsync -vprogzlt --chown=root:www-data --delete-during --rsh="ssh -v -i $DEPLOY_CREDENTIALS" $DEPLOY_PATH root@$DEPLOY_DESTINATION:$DEPLOY_PATH || exit 1

ssh root@$DEPLOY_DESTINATION "service machine-web-api restart"

