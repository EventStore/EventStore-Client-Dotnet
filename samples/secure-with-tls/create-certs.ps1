docker-compose -f docker-compose.generate-certs.yml up

Import-Certificate -FilePath ".\certs\ca\ca.crt" -CertStoreLocation Cert:\CurrentUser\Root