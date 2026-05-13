-- =============================================================================
-- RailFactory-Fork: Dev Data Cleanup Script
-- Use this script in pgAdmin to wipe operational data from 'dev' databases.
-- =============================================================================

-- 1. IAM Database (tenant-dev-iamdb)
-- CONNECT TO tenant-dev-iamdb;
-- TRUNCATE TABLE iam_local_users CASCADE;

-- 2. Supply Chain Database (tenant-dev-supplychaindb)
-- CONNECT TO tenant-dev-supplychaindb;
-- TRUNCATE TABLE material_receipt_items CASCADE;
-- TRUNCATE TABLE material_receipts CASCADE;
-- TRUNCATE TABLE suppliers CASCADE;
-- TRUNCATE TABLE supply_audit_entries CASCADE;
-- TRUNCATE TABLE supply_outbox_messages CASCADE;

-- 3. Inventory Database (tenant-dev-inventorydb)
-- CONNECT TO tenant-dev-inventorydb;
-- TRUNCATE TABLE inventory_ledger_entries CASCADE;
-- TRUNCATE TABLE inventory_balances CASCADE;
-- TRUNCATE TABLE materials CASCADE;
-- TRUNCATE TABLE inventory_processed_integration_messages CASCADE;
-- TRUNCATE TABLE stock_locations CASCADE;

-- 4. Production Database (tenant-dev-productiondb)
-- CONNECT TO tenant-dev-productiondb;
-- (Add tables here as P4/P5 progresses)

-- NOTE: CASCADE ensures that dependent records are removed. 
-- In PostgreSQL, you must run these commands against each specific database.
