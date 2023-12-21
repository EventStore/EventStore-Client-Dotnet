New-Item -ItemType Directory -Force -Path certs

docker-compose -f docker-compose.certs.yml up --remove-orphans

Import-Certificate -FilePath ".\certs\ca\ca.crt" -CertStoreLocation Cert:\CurrentUser\Root
