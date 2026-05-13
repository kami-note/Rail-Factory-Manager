#!/bin/bash

# =============================================================================
# RailFactory-Fork: Dev Database Reset Script
# Simulates 'php artisan migrate:fresh' by dropping and recreating dev DBs.
# =============================================================================

# Default credentials for local development
DB_USER=${POSTGRES_USER:-"postgres"}
DB_HOST=${POSTGRES_HOST:-"localhost"}
DB_PORT=${POSTGRES_PORT:-5432}

# Databases to reset
DATABASES=("tenant-dev-iamdb" "tenant-dev-supplychaindb" "tenant-dev-inventorydb" "tenant-dev-productiondb")

echo "----------------------------------------------------------"
echo "☢️  RAILFACTORY DEV DATABASE RESET ☢️"
echo "----------------------------------------------------------"

for db in "${DATABASES[@]}"; do
    echo "Resetting database: $db..."
    
    # Terminate active connections to allow dropping the DB
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$db' AND pid <> pg_backend_pid();" > /dev/null 2>&1
    
    # Drop and Recreate
    dropdb -h $DB_HOST -p $DB_PORT -U $DB_USER --if-exists $db
    createdb -h $DB_HOST -p $DB_PORT -U $DB_USER $db
    
    if [ $? -eq 0 ]; then
        echo "✅ $db reset successfully."
    else
        echo "❌ Failed to reset $db."
    fi
done

echo "----------------------------------------------------------"
echo "Done! Please restart the application to apply migrations."
echo "----------------------------------------------------------"
