#!/bin/bash

DEPLOY_PATH="/var/www/virtual/languageforge.org/machine/"
USER="www-data"
ASPNETCORE_ENVIRONMENT="Development"

pushd $DEPLOY_PATH > /dev/null

if [[ -z $1 || $1 = "unix" ]]; then
    SOCKET_FILE="/tmp/machine-web-api.sock"
    URL="http://unix:$SOCKET_FILE"
    sudo -u $USER rm -f $SOCKET_FILE
elif [ "$1"=="tcp" ]; then
    URL="http://localhost:5000"
fi

sudo -u $USER "ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT" ./SIL.Machine.WebApi.Server --server.urls=$URL

popd > /dev/null