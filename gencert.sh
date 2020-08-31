#!/usr/bin/env bash

mkdir -p certs

docker pull docker.pkg.github.com/eventstore/es-gencert-cli/es-gencert-cli:1.0.1

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) docker.pkg.github.com/eventstore/es-gencert-cli/es-gencert-cli:1.0.1 create-ca -out /tmp/ca

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) docker.pkg.github.com/eventstore/es-gencert-cli/es-gencert-cli:1.0.1 create-node -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/node -ip-addresses 127.0.0.1

chmod 0755 -R ./certs
