unameOutput="$(uname -sr)"
case "${unameOutput}" in
    Linux*Microsoft*) machine=WSL;;
    Linux*)           machine=Linux;;
    Darwin*)          machine=MacOS;;
    *)                machine="${unameOutput}"
esac

echo ">> Generating certificate..."
mkdir -p certs
docker-compose -f docker-compose.certs.yml up --remove-orphans

echo ">> Copying certificate..."
cp certs/ca/ca.crt /usr/local/share/ca-certificates/eventstore_ca.crt
#rsync --progress certs/ca/ca.crt /usr/local/share/ca-certificates/eventstore_ca.crt

echo ">> Updating certificate permissions..."
#chmod 644 /usr/local/share/ca-certificates/eventstore_ca.crt

if [ "${machine}" == "MacOS" ]; then
  echo ">> Installing certificate on ${machine}..."
  sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain /usr/local/share/ca-certificates/eventstore_ca.crt   
elif [ "$(machine)" == "Linux" ]; then
  echo ">> Installing certificate on ${machine}..."
  sudo update-ca-certificates    
elif [ "$(machine)" == "WSL" ]; then
  echo ">> Installing certificate on ${machine}..."
  sudo update-ca-certificates    
else
  echo ">> Unknown platform. Please install the certificate manually."   
fi