#!/bin/bash

echo "Going down..."
docker-compose -f docker-compose.yml -f docker-compose.app.yml down

echo "Going up..."
docker-compose -f docker-compose.yml -f docker-compose.app.yml up --remove-orphans
