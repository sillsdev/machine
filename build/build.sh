#!/bin/bash

PROJECT=SIL.Machine.WebApi.Server
CONFIGURATION=${CONFIGURATION:-Release}
DEPLOY_RUNTIME=${DEPLOY_RUNTIME:-ubuntu.16.04-x64}
BUILD_OUTPUT=artifacts
THOT_NEW_MODEL_FILE="$BUILD_OUTPUT/package/thot-new-model.zip"
PACKAGE_FILE="$BUILD_OUTPUT/machine-web-api.tgz"

pushd .. > /dev/null

sudo rm -rf $BUILD_OUTPUT
dotnet restore || exit 1
dotnet publish -c $CONFIGURATION --runtime $DEPLOY_RUNTIME -o ../../$BUILD_OUTPUT/package src/$PROJECT/$PROJECT.csproj || exit 1
(cd src/$PROJECT/data/thot-new-model; zip -q -r ../../../../$THOT_NEW_MODEL_FILE . -x .gitattributes;) || exit 1
tar -cvzf $PACKAGE_FILE -C $BUILD_OUTPUT/package . > /dev/null || exit 1

popd > /dev/null