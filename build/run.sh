#!/bin/bash

DEPLOY_PATH="/var/www/languageforge.org_cat/machine/"
SOCKET_FILE="/tmp/machine-web-api.sock"
USER="www-data"

pushd $DEPLOY_PATH > /dev/null

sudo -u $USER rm -f $SOCKET_FILE
sudo -u $USER ./SIL.Machine.WebApi --server.urls=http://unix:$SOCKET_FILE

popd > /dev/null