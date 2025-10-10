# SWEN3Project
## By
Sebastian & Felix

## Overview
.NET/C# document Document Management System for SWEN3 (FH Technikum Wien)

## DB Setup (Docker Desktop)
1. Pull the postgres-17.5 image
2. Run it in a container
3. Use psql in Docker Desktop to create a DB called 'swen3project'
4. Add it in Visual Studio's Server Explorer
5. Update Configuration.cs with location of your config file (_FILENAME)
6. Perform Entity Framework Migration to set up DB

## Entity Framework Migrations (in Package Manager Console)
1. Install-Package Microsoft.EntityFrameworkCore.Tools
2. Add-Migration MigrationName
3. Update-Database

## HTTP Request Testing
1. code --install-extension humao.rest-client
