#!/bin/bash

DEPLOY_PATH="/var/www/virtual/languageforge.org/machine/"
EXECUTE_USER="www-data"
ASPNETCORE_ENVIRONMENT="Development"

pushd $DEPLOY_PATH > /dev/null

if [ $2 = $USER ]; then
    EXECUTE_USER=""
elif [ ! -z $2 ]; then
    EXECUTE_USER=$2
fi

if [[ -z $1 || $1 = "unix" ]]; then
    SOCKET_FILE="/tmp/machine-web-api.sock"
    URL="http://unix:$SOCKET_FILE"
    if [ -z $EXECUTE_USER ]; then
        rm -f $SOCKET_FILE
    else
        sudo -u $EXECUTE_USER rm -f $SOCKET_FILE
    fi
elif [ $1 = "tcp" ]; then
    URL="http://localhost:5000"
fi

if [ -z $EXECUTE_USER ]; then
    ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT ./SIL.Machine.WebApi.Server --server.urls=$URL
else
    sudo -u $EXECUTE_USER ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT ./SIL.Machine.WebApi.Server --server.urls=$URL
fi

popd > /dev/null