# SWEN3Project
## By
Sebastian & Felix

## Overview
.NET/C# document Document Management System for SWEN3 (FH Technikum Wien)

## Building
Build using the docker-compose file: docker compose up -d --build

## DB Setup (Initial Migration)
1. Start Docker Desktop
2. Run where docker-compose.yml is located: docker compose up -d --build
3. Navigate in Docker Desktop: Containers > swen3project > paperless-postgres > Files > run > secrets > postgres_password
4. Save password locally
5. In C# Project: Hard-code Configuration.cs to include password
6. In Package Manager Console: Add-Migration MigrationName
7. In C# Project: Change Configuration.cs back to initial setup
8. Run where docker-compose.yml is located: docker compose down
9. Run where docker-compose.yml is located: docker compose up -d --build
10. C# Project will now apply the migration when starting up

## RabbitMQ Setup
RabbitMQ is configured via:

- a local `.env` file (not tracked)
- a local RabbitMQ config file `config/myrabbitmq.conf` (not tracked)

### 1. Create `.env`

In the repository root (next to `docker-compose.yml`), create a `.env` file.
You can use `.env.example` as a starting point.

Example:

RABBITMQ_USER=paperless
RABBITMQ_PASSWORD=paperless
RABBITMQ_HOST=paperless-rabbitmq
RABBITMQ_PORT=5672
RABBITMQ_QUEUE=paperless.ocr

### 2. Create config file

1. Create config directory in repository root
2. Create myrabbitmq.conf in config directory
3. Add two lines to the conf file: (replace with credentials of your choice)
    - default_user = 'myusername'
    - default_pass = 'mypassword'


## Integration tests

curl script is under tests/integration
to run, edit the script in VS code and change to LF (bottom right)
