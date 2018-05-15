#!/bin/bash

DEPLOY_PATH=${DEPLOY_PATH:-/opt/machine-web-api/} 
EXECUTE_USER=${EXECUTE_USER:-www-data}
ASPNETCORE_ENVIRONMENT="Development"
URL=${URL:-http://localhost:5001}

pushd $DEPLOY_PATH > /dev/null

if [ -z $EXECUTE_USER ]; then
    ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT ./SIL.Machine.WebApi.Server --urls $URL
else
    sudo -u $EXECUTE_USER ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT ./SIL.Machine.WebApi.Server --urls $URL
fi

popd > /dev/null