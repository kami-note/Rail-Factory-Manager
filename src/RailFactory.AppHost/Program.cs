var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "rail-factory-dev-postgres", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("rail-factory-fork-postgres-data-v2")
    .WithPgAdmin();

var tenantCatalogDb = postgres.AddDatabase("tenantcatalog");
var tenantDevIamDb = postgres.AddDatabase("tenant-dev-iamdb", "iamdb");
var tenantDevSupplyChainDb = postgres.AddDatabase("tenant-dev-supplychaindb", "supplychaindb");
var tenantDevInventoryDb = postgres.AddDatabase("tenant-dev-inventorydb", "inventorydb");
var tenantDevProductionDb = postgres.AddDatabase("tenant-dev-productiondb", "productiondb");

var redis = builder.AddRedis("redis")
    .WithDataVolume("rail-factory-fork-redis-data");

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

var tenantManagement = builder.AddProject<Projects.RailFactory_Tenancy_Api>("tenant-management")
    .WithReference(tenantCatalogDb)
    .WithReference(tenantDevIamDb)
    .WithReference(tenantDevSupplyChainDb)
    .WithReference(tenantDevInventoryDb)
    .WithReference(tenantDevProductionDb)
    .WithEnvironment("TenantRouting__DefaultTenantCode", "dev")
    .WithEnvironment("TenantRouting__CatalogCacheTtlSeconds", "60")
    .WaitFor(tenantCatalogDb);

var iam = builder.AddProject<Projects.RailFactory_Iam_Api>("identity-access-management")
    .WithReference(tenantManagement)
    .WithReference(tenantCatalogDb)
    .WithReference(tenantDevIamDb)
    .WithReference(redis)
    .WithEnvironment("TenantRouting__ServiceKey", "iamdb")
    .WithEnvironment("TenantRouting__DefaultTenantCode", "dev")
    .WaitFor(tenantCatalogDb)
    .WaitFor(tenantDevIamDb)
    .WaitFor(redis)
    .WaitFor(tenantManagement);

var supplyChain = builder.AddProject<Projects.RailFactory_SupplyChain_Api>("supply-chain")
    .WithReference(tenantManagement)
    .WithReference(tenantCatalogDb)
    .WithReference(tenantDevSupplyChainDb)
    .WithEnvironment("TenantRouting__ServiceKey", "supplychaindb")
    .WithEnvironment("TenantRouting__DefaultTenantCode", "dev")
    .WaitFor(tenantCatalogDb)
    .WaitFor(tenantDevSupplyChainDb)
    .WaitFor(tenantManagement);

var inventory = builder.AddProject<Projects.RailFactory_Inventory_Api>("inventory")
    .WithReference(tenantManagement)
    .WithReference(tenantCatalogDb)
    .WithReference(tenantDevInventoryDb)
    .WithEnvironment("TenantRouting__ServiceKey", "inventorydb")
    .WithEnvironment("TenantRouting__DefaultTenantCode", "dev")
    .WaitFor(tenantCatalogDb)
    .WaitFor(tenantDevInventoryDb)
    .WaitFor(tenantManagement);

var production = builder.AddProject<Projects.RailFactory_Production_Api>("production")
    .WithReference(tenantManagement)
    .WithReference(tenantCatalogDb)
    .WithReference(tenantDevProductionDb)
    .WithReference(rabbitmq)
    .WithEnvironment("TenantRouting__ServiceKey", "productiondb")
    .WithEnvironment("TenantRouting__DefaultTenantCode", "dev")
    .WaitFor(tenantCatalogDb)
    .WaitFor(tenantDevProductionDb)
    .WaitFor(rabbitmq)
    .WaitFor(tenantManagement);

var gateway = builder.AddProject<Projects.RailFactory_Gateway>("gateway")
    .WithReference(tenantManagement)
    .WithReference(iam)
    .WithReference(supplyChain)
    .WithReference(inventory)
    .WithReference(production)
    .WaitFor(tenantManagement)
    .WaitFor(iam)
    .WaitFor(supplyChain)
    .WaitFor(inventory)
    .WaitFor(production);

var frontend = builder.AddProject<Projects.RailFactory_Frontend>("frontend")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.AddExecutable(
        "frontend-ui",
        "npm",
        "../RailFactory.Frontend/App",
        "run",
        "dev",
        "--",
        "--host",
        "0.0.0.0",
        "--port",
        "5082")
    .WithReference(frontend)
    .WithEnvironment("VITE_BFF_ORIGIN", frontend.GetEndpoint("http"))
    .WithHttpEndpoint(port: 5082, targetPort: 5082, env: "PORT", isProxied: false)
    .WaitFor(frontend);

builder.Build().Run();
