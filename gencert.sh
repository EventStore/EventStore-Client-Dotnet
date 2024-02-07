#!/usr/bin/env bash

unameOutput="$(uname -sr)"
case "${unameOutput}" in
    Linux*Microsoft*) machine=WSL;;
    Linux*)           machine=Linux;;
    Darwin*)          machine=MacOS;;
    *)                machine="${unameOutput}"
esac

echo ">> Generating certificate..."
mkdir -p certs

chmod 0755 ./certs

docker pull eventstore/es-gencert-cli:1.0.2

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) eventstore/es-gencert-cli:1.0.2 create-ca -out /tmp/ca

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) eventstore/es-gencert-cli:1.0.2 create-node -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/node -ip-addresses 127.0.0.1 -dns-names localhost

chmod -R 0755 ./certs

echo ">> Copying certificate..."
cp certs/ca/ca.crt /usr/local/share/ca-certificates/eventstore_ca.crt

if [ "${machine}" == "MacOS" ]; then
  echo ">> Installing certificate on ${machine}..."
  sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain /usr/local/share/ca-certificates/eventstore_ca.crt   
elif [ "$machine" == "Linux" ] || [ "$machine" == "WSL" ]; then
  echo ">> Installing certificate on ${machine}..."
  sudo update-ca-certificates    
else
  echo ">> Unknown platform. Please install the certificate manually."   
fi