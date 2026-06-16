#!/bin/bash
# =============================================================================
# RailFactory-Fork: Clean All Dev Data Script
# Wipes all operational database tables for all tenants while keeping
# migrations history and IAM users/roles intact so sessions don't break.
# =============================================================================
set -e

DB_CONTAINER="postgres-vzpnfbzw"
PGPASSWORD="rail-factory-dev-postgres"

tenants=("dev" "acme")

echo "----------------------------------------------------------"
echo "🧹 Wiping operational tables for all databases..."
echo "----------------------------------------------------------"

for tenant in "${tenants[@]}"; do
    echo "Wiping databases for tenant: $tenant..."
    
    # 1. IAM Database (Keep users and roles, wipe audit logs)
    echo "  - tenant-$tenant-iamdb"
    docker exec -e PGPASSWORD=$PGPASSWORD $DB_CONTAINER psql -U postgres -d "tenant-$tenant-iamdb" -c "
    DO \$\$ DECLARE
        r RECORD;
    BEGIN
        FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename NOT IN ('__EFMigrationsHistory', 'iam_local_users', 'iam_tenant_roles', 'iam_user_roles')) LOOP
            EXECUTE 'TRUNCATE TABLE ' || quote_ident(r.tablename) || ' CASCADE;';
        END LOOP;
    END \$\$;" > /dev/null
    
    # 2. Operational Databases (Wipe all tables except migrations history)
    services=("supplychaindb" "inventorydb" "productiondb" "hrdb" "fleetdb" "logisticsdb")
    for svc in "${services[@]}"; do
        db_name="tenant-$tenant-$svc"
        echo "  - $db_name"
        docker exec -e PGPASSWORD=$PGPASSWORD $DB_CONTAINER psql -U postgres -d "$db_name" -c "
        DO \$\$ DECLARE
            r RECORD;
        BEGIN
            FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory') LOOP
                EXECUTE 'TRUNCATE TABLE ' || quote_ident(r.tablename) || ' CASCADE;';
            END LOOP;
        END \$\$;" > /dev/null
    done
done

echo "----------------------------------------------------------"
echo "✅ Operational database tables cleared successfully!"
echo "----------------------------------------------------------"
