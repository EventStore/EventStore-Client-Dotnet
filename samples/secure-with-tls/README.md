# Secure EventStoreDB with TLS

- [Secure EventStoreDB with TLS](#secure-eventstoredb-with-tls)
  - [Overview](#overview)
    - [Certificates](#certificates)
  - [Description](#description)
  - [Running Sample](#running-sample)
    - [1. Generate self-signed certificates](#1-generate-self-signed-certificates)
    - [2. Run samples with Docker](#2-run-samples-with-docker)
    - [3. Run samples locally (without Docker)](#3-run-samples-locally-without-docker)
      - [3.1 Install certificate - Linux (Ubuntu, Debian)](#31-install-certificate---linux-ubuntu-debian)
      - [3.2 Install certificate - Windows](#32-install-certificate---windows)
      - [3.3. Run EventStoreDB node](#33-run-eventstoredb-node)
      - [3.3 Run client application](#33-run-client-application)

## Overview

The sample shows how to run the .NET gRPC client secured by TLS certificates.

Read more in the docs:
- [Security](https://developers.eventstore.com/server/v20/server/security/)
- [Running EventStoreDB with `docker-compose`](https://developers.eventstore.com/server/v20/server/installation/docker.html#use-docker-compose)
- [Event Store Certificate Generation CLI](https://github.com/EventStore/es-gencert-cli)

It is essential for production use to configure EventStoreDB security features to prevent unauthorised access to your data.
EventStoreDB supports gRPC with TLS and SSL. Each protocol has its security configuration, but you can only use one set of certificates for TLS and HTTPS.

### Certificates

The protocol security configuration depends a lot on the deployment topology and platform. We have created an interactive [configuration tool](https://github.com/EventStore/es-gencert-cli), which also has instructions on generating and installing the certificates and configure EventStoreDB nodes to use them. 

You need to generate CA (certificate authority)

`./es-gencert-cli create-ca -out ./es-ca`

And certificate for each node in your cluster.

`./es-gencert-cli-cli create-node -ca-certificate ./es-ca/ca.crt -ca-key ./es-ca/ca.key -out ./node -ip-addresses 127.0.0.1,172.20.240.1 -dns-names localhost,eventstoredb`

The client application should have public CA certificate installed (**_Note:_** private keys should not be shared to clients).

While generating the certificate, you need to remember to pass:
- IP addresses to `-ip-addresses`: e.g. `127.0.0.1,172.20.240.1` or 
- DNS names to `-dns-names`: e.g. `localhost,eventstoredb`
that will match the URLs that you will be accessing EventStoreDB nodes.
  
[Certificate Generation CLI](https://github.com/EventStore/es-gencert-cli) is also available as the Docker image. You can define [docker-compose configuration](./docker-compose.generate-certs.yml) as e.g.:

```yaml
version: "3.5"

services:
    cert-gen:
        image: eventstore/es-gencert-cli:1.0.2
        entrypoint: bash
        command: >
            -c "es-gencert-cli create-ca -out /tmp/certs/ca &&
                es-gencert-cli create-node -ca-certificate /tmp/certs/ca/ca.crt -ca-key /tmp/certs/ca/ca.key -out \
                /tmp/certs/node -ip-addresses 127.0.0.1 -dns-names localhost,eventstoredb"
        user: "1000:1000"
        volumes:
            - "./certs:/tmp/certs"
```

And run `docker-compose up` to generate certificates.

See instruction how to install certificates [below](#3-run-run-samples-locally-without-docker).

You can find helpers scripts that are also installing created CA on local machine:
- Linux (Debian based) - [create-certs.sh](./create-certs.sh),
- Windows - [create-certs.ps1](./create-certs.ps1)

## Description

The sample shows how to connect with the gRPC client and append new event. You can run it locally or through docker configuration.

Suggested order of reading:
- whole code is located in [Program.cs](./Program.cs) file
- [Dockerfile](./Dockerfile) for building sample image
- [Docker compose config](./docker-compose.yml) that has configuration for both sample client app and EventStoreDB node.

## Running Sample

### 1. Generate self-signed certificates
Use following command to generate certificates:
- Linux/MacOS
  ```console
  ./create-certs.sh
  ```
- Windows 
  ```powershell
  .\create-certs.ps1
  ```
_Note: to regenerate certificates you need to remove the [./certs](./certs) folder._

### 2. Run samples with Docker
Use the following command to run samples with Docker:
```consoler
docker-compose up
```
It will run both server and client with preconfigured TLS connection setup.

### 3. Run samples locally (without Docker)
To run samples locally, you need to install the generated CA certificate. 

#### 3.1 Install certificate - Linux (Ubuntu, Debian)
- Copy [./certs/ca/ca.crt](./certs/ca/ca.crt) to dir `/usr/local/share/ca-certificates/`
- Use command: 
  ```console
  sudo cp foo.crt /usr/local/share/ca-certificates/foo.crt
  ```
- Update the CA store: 
  ```console
  sudo update-ca-certificates.
  ```
#### 3.2 Install certificate - Windows
Windows certificate will be automatically installed when [.\create-certs.ps1](.\create-certs.ps1) was run.

#### 3.3. Run EventStoreDB node

Use the following command to run EventStoreDB 

```console
docker-compose up eventstoredb
```

#### 3.3 Run client application
Run application from console:

```console
dotnet run .\secure-with-tls.csproj
```
or from your favourite IDE.
