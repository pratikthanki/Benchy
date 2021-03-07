#!/bin/bash
set -e

printf "Building kafka docker image"
docker build -f Dockerfile.kafka -t kafka .

printf "Building zookeeper docker image"
docker build -f Dockerfile.zookeeper -t zookeeper .

printf "docker composing up"
docker-compose up -d

