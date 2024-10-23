Write-Host ">> Generating certificate..."

# Create directory if it doesn't exist
New-Item -ItemType Directory -Path .\certs -Force

# Set permissions for the directory
icacls .\certs /grant:r "$($env:UserName):(OI)(CI)F"

# Pull the Docker image
docker pull docker.eventstore.com/eventstore-utils/es-gencert-cli:latest

docker run --rm --volume .\certs:/tmp docker.eventstore.com/eventstore-utils/es-gencert-cli create-ca -out /tmp/ca

docker run --rm --volume .\certs:/tmp docker.eventstore.com/eventstore-utils/es-gencert-cli create-node -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/node -ip-addresses 127.0.0.1 -dns-names localhost

# Create admin user
docker run --rm --volume .\certs:/tmp docker.eventstore.com/eventstore-utils/es-gencert-cli create-user -username admin -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/user-admin

# Create an invalid user
docker run --rm --volume .\certs:/tmp docker.eventstore.com/eventstore-utils/es-gencert-cli create-user -username invalid -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/user-invalid

# Set permissions recursively for the directory
icacls .\certs /grant:r "$($env:UserName):(OI)(CI)F"
