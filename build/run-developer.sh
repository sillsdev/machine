#!/bin/bash

DEPLOY_PATH="/var/www/virtual/languageforge.org/machine/"
SOCKET_FILE="/tmp/machine-web-api.sock"
USER="www-data"
ASPNETCORE_ENVIRONMENT="Development"

pushd $DEPLOY_PATH > /dev/null

sudo -u $USER rm -f $SOCKET_FILE
sudo -u $USER "ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT" ./SIL.Machine.WebApi --server.urls=http://unix:$SOCKET_FILE

popd > /dev/null