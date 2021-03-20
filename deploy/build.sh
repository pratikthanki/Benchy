#!/bin/bash
set -e

trap cleanup ERR TERM INT

echo "Running redis docker container.."
docker run --name local-redis -d redis

echo "Building Benchy docker image.."
docker build . -t benchy

echo "Running benchy in producer mode"
# TODO: docker run here

echo "Running benchy in consumer mode"
# TODO: docker run here

echo "done!"
