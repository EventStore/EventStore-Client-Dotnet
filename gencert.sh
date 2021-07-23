#!/usr/bin/env bash

mkdir -p certs

chmod 0755 ./certs

docker pull eventstore/es-gencert-cli:1.0.1

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) eventstore/es-gencert-cli:1.0.1 create-ca -out /tmp/ca

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) eventstore/es-gencert-cli:1.0.1 create-node -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/node -ip-addresses 127.0.0.1 -dns-names localhost

chmod 0755 -R ./certs
