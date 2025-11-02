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