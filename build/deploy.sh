#!/bin/bash

PROJECT=SIL.Machine.WebApi
BUILD_OUTPUT=artifacts
PACKAGE_FILE=$BUILD_OUTPUT/machine-web-api.tgz
DEPLOY_PATH=/var/www/languageforge.org_cat/machine/

pushd .. > /dev/null

sudo rm -rf $BUILD_OUTPUT
dotnet restore || exit 1
dotnet publish -c Release -o $BUILD_OUTPUT/package src/$PROJECT/project.json || exit 1
tar -cvzf $PACKAGE_FILE -C $BUILD_OUTPUT/package . > /dev/null || exit 1

sudo rm -rf $DEPLOY_PATH
mkdir $DEPLOY_PATH || exit 1
tar -xzf $PACKAGE_FILE -C $DEPLOY_PATH > /dev/null || exit 1
sudo chown -R root:www-data $DEPLOY_PATH || exit 1
sudo chmod -R 755 $DEPLOY_PATH || exit 1

rsync -vprogzlt --chown=root:www-data --delete-during --rsh="ssh -v -i $DEPLOY_CREDENTIALS" $DEPLOY_PATH $DEPLOY_DESTINATION || exit 1

popd > /dev/null
