#!/bin/bash

# Basic script to add and apply EF Core migrations
# Assumes running from the solution root directory

# Configuration
INFRASTRUCTURE_PROJ="src/DDDProject.Infrastructure/DDDProject.Infrastructure.csproj"
API_PROJ="src/DDDProject.API/DDDProject.API.csproj"
MIGRATION_NAME="InitialCreate" # Default migration name, can be passed as argument

# Check if a migration name was provided as an argument
if [ ! -z "$1" ]; then
    MIGRATION_NAME=$1
fi

echo "Using Migration Name: $MIGRATION_NAME"

# Ensure EF Core tools are installed (optional check)
if ! command -v dotnet-ef &> /dev/null
then
    echo "dotnet-ef command could not be found. Please install EF Core tools globally:"
    echo "dotnet tool install --global dotnet-ef"
    exit 1
fi

# Add Migration
echo "Adding migration..."
dotnet ef migrations add $MIGRATION_NAME --project $INFRASTRUCTURE_PROJ --startup-project $API_PROJ

if [ $? -ne 0 ]; then
    echo "Failed to add migration."
    exit 1
fi

# Apply Migration
echo "Applying migration to the database..."
dotnet ef database update --project $INFRASTRUCTURE_PROJ --startup-project $API_PROJ

if [ $? -ne 0 ]; then
    echo "Failed to apply migration."
    exit 1
fi

echo "Migration completed successfully."
exit 0 