#!/bin/bash

PROJECT=SIL.Machine.WebApi.Server
CONFIGURATION=${CONFIGURATION:-Release}
DEPLOY_RUNTIME=${DEPLOY_RUNTIME:-ubuntu.16.04-x64}
BUILD_OUTPUT=artifacts
THOT_NEW_MODEL_FILE=$BUILD_OUTPUT/package/thot-new-model.zip
PACKAGE_FILE=$BUILD_OUTPUT/machine-web-api.tgz
DEPLOY_PATH=${DEPLOY_PATH:-/var/www/languageforge.org_cat/machine/}
ENGINES_PATH="/var/lib/languageforge/machine/engines/"
DATA_PATH="/var/lib/languageforge/machine/data/"

pushd .. > /dev/null

sudo rm -rf $BUILD_OUTPUT
dotnet restore || exit 1
dotnet publish -c $CONFIGURATION --runtime $DEPLOY_RUNTIME -o ../../$BUILD_OUTPUT/package src/$PROJECT/$PROJECT.csproj || exit 1
(cd src/$PROJECT/data/thot-new-model; zip -q -r ../../../../$THOT_NEW_MODEL_FILE . -x .gitattributes;) || exit 1
tar -cvzf $PACKAGE_FILE -C $BUILD_OUTPUT/package . > /dev/null || exit 1

sudo rm -rf $DEPLOY_PATH
sudo mkdir -p $DEPLOY_PATH || exit 1
sudo tar -xzf $PACKAGE_FILE -C $DEPLOY_PATH > /dev/null || exit 1
sudo chown -R root:www-data $DEPLOY_PATH || exit 1
sudo chmod -R 755 $DEPLOY_PATH || exit 1

if [ ! -d $ENGINES_PATH ]; then
    sudo mkdir -p $ENGINES_PATH || exit 1
    sudo chown -R www-data:www-data $ENGINES_PATH || exit 1
    sudo chmod -R 755 $ENGINES_PATH || exit 1
fi

if [ ! -d $DATA_PATH ]; then
    sudo mkdir -p $DATA_PATH || exit 1
    sudo chown -R www-data:www-data $DATA_PATH || exit 1
    sudo chmod -R 755 $DATA_PATH || exit 1
fi

popd > /dev/null