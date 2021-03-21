#!/bin/bash
set -e

trap cleanup ERR TERM INT

echo "Killing existing rabbitmq docker container.."
docker stop rabbitmq || true && docker rm rabbitmq || true

echo "Running rabbitmq docker container.."
docker run -d --rm -it --hostname my-rabbit --name rabbitmq -p 15672:15672 -p 5672:5672 rabbitmq:3-management

echo "Building Benchy docker image.."
# docker build . -t benchy

echo "Running benchy in producer mode.."
# TODO: docker run here

echo "Running benchy in consumer mode.."
# TODO: docker run here

echo "Done!"
