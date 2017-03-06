#!/bin/bash

PROJECT=SIL.Machine.WebApi
BUILD_OUTPUT=artifacts
THOT_NEW_MODEL_FILE=$BUILD_OUTPUT/package/thot-new-model.tgz
PACKAGE_FILE=$BUILD_OUTPUT/machine-web-api.tgz
DEPLOY_PATH=/var/www/languageforge.org_cat/machine/

pushd .. > /dev/null

sudo rm -rf $BUILD_OUTPUT
dotnet restore || exit 1
dotnet publish -c Release --runtime $DEPLOY_RUNTIME -o $BUILD_OUTPUT/package src/$PROJECT/project.json || exit 1
tar -cvzf $THOT_NEW_MODEL_FILE -C src/$PROJECT/data/thot-new-model --exclude=".gitattributes" . > /dev/null || exit 1
tar -cvzf $PACKAGE_FILE -C $BUILD_OUTPUT/package . > /dev/null || exit 1

sudo rm -rf $DEPLOY_PATH
mkdir $DEPLOY_PATH || exit 1
tar -xzf $PACKAGE_FILE -C $DEPLOY_PATH > /dev/null || exit 1
sudo chown -R root:www-data $DEPLOY_PATH || exit 1
sudo chmod -R 755 $DEPLOY_PATH || exit 1

rsync -vprogzlt --chown=root:www-data --delete-during --rsh="ssh -v -i $DEPLOY_CREDENTIALS" $DEPLOY_PATH root@$DEPLOY_DESTINATION:$DEPLOY_PATH || exit 1

ssh root@$DEPLOY_DESTINATION "service machine-web-api restart"

popd > /dev/null
