# SWEN3Project
## By
Sebastian & Felix

## Overview
.NET/C# document Document Management System for SWEN3 (FH Technikum Wien)

## Building
Build using the docker-compose file: docker compose up -d

## DB Setup (Initial Migration)
1. Start Docker Desktop
2. Run where docker-compose.yml is located: docker compose up -d
3. Navigate in Docker Desktop: Containers > swen3project > paperless-postgres > Files > run > secrets > postgres_password
4. Save password within to a local txt file
5. In C# Project: Change passwordPath in Configuration.cs to absolute path to local txt file
6. In Package Manager Console: Add-Migration InitialMigration
7. In C# Project: Change passwordPath in Configuration.cs back to relative path
8. Run where docker-compose.yml is located: docker compose down -v
9. Run where docker-compose.yml is located: docker compose up -d
10. C# Project will now apply the migration when starting up
