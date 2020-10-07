ARG DOCKER_TAG=ci
FROM docker.pkg.github.com/eventstore/eventstore/eventstore:${DOCKER_TAG}

WORKDIR /etc/eventstore/certs

COPY certs .

USER root

RUN chown -R eventstore:eventstore /etc/eventstore/certs

USER eventstore

ENV EVENTSTORE_MEM_DB=true
ENV EVENTSTORE_CERTIFICATE_FILE=/etc/eventstore/certs/node/node.crt
ENV EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE=/etc/eventstore/certs/node/node.key
ENV EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH=/etc/eventstore/certs/ca

WORKDIR /opt/eventstore

