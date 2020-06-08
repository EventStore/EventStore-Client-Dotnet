rm -rf certs
mkdir certs

openssl genrsa -out certs/ca.key 2048

openssl req \
	-new \
	-sha256 \
	-key certs/ca.key \
	-subj "/OU=Development/O=Event Store Ltd" \
	-out certs/ca.csr

openssl req \
    -x509 \
    -key certs/ca.key \
    -in certs/ca.csr \
    -out certs/ca.crt \
    -days 10000 \
    -sha256

openssl genrsa -out certs/dev.key 2048

openssl req \
	-new \
	-sha256 \
	-key certs/dev.key \
	-subj "/OU=Development/O=Event Store Ltd" \
	-out certs/dev.csr

openssl x509 \
    -req \
    -in certs/dev.csr \
    -CA certs/ca.crt \
    -CAkey certs/ca.key \
    -CAcreateserial \
    -out certs/dev.crt \
    -days 10000 \
    -extfile <(printf "subjectAltName = IP:127.0.0.1,DNS:localhost") \
    -sha256
