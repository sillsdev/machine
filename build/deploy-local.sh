#!/bin/bash

source build.sh

DEPLOY_PATH=${DEPLOY_PATH:-/opt/machine-web-api/}
DATA_PATH="/var/opt/machine-web-api"

pushd .. > /dev/null

sudo rm -rf $DEPLOY_PATH
sudo mkdir -p $DEPLOY_PATH || exit 1
sudo tar -xpzf $PACKAGE_FILE -C $DEPLOY_PATH > /dev/null || exit 1
sudo chown -R root:www-data $DEPLOY_PATH || exit 1

if [ ! -d $DATA_PATH ]; then
    sudo mkdir -p $DATA_PATH || exit 1
    sudo chown -R www-data:www-data $DATA_PATH || exit 1
    sudo chmod -R 755 $DATA_PATH || exit 1
fi

popd > /dev/null

sudo systemctl restart machine-web-api