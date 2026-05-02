const string DefaultTenantCode = "dev";
const string TenantCatalogCacheTtlSeconds = "60";

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("env");

var parameters = AddParameters(builder);
var infra = AddInfrastructure(builder, parameters);
var services = AddDomainServices(builder, infra, parameters);
var frontend = AddEdgeServices(builder, services, parameters.Edge);

if (builder.ExecutionContext.IsRunMode)
{
    AddLocalFrontendUi(builder, frontend);
}

builder.Build().Run();

static AppHostParameters AddParameters(IDistributedApplicationBuilder builder)
{
    return new AppHostParameters(
        PostgresPassword: builder.AddParameter("postgres-password", "rail-factory-dev-postgres", secret: true),
        OAuth: new AppHostOAuthParameters(
            GoogleClientId: builder.AddParameter("google-client-id", secret: true),
            GoogleClientSecret: builder.AddParameter("google-client-secret", secret: true)),
        Edge: new AppHostEdgeParameters(
            FrontendPublicOrigin: builder.AddParameter("frontend-public-origin", secret: true)));
}

static AppHostInfrastructure AddInfrastructure(
    IDistributedApplicationBuilder builder,
    AppHostParameters parameters)
{
    var postgres = builder.AddPostgres("postgres", password: parameters.PostgresPassword)
        .WithDataVolume("rail-factory-fork-postgres-data-v2")
        .WithPgAdmin();

    var redis = builder.AddRedis("redis")
        .WithDataVolume("rail-factory-fork-redis-data");

    return new AppHostInfrastructure(
        TenantCatalogDb: postgres.AddDatabase("tenantcatalog"),
        TenantDevIamDb: postgres.AddDatabase("tenant-dev-iamdb", "iamdb"),
        TenantDevSupplyChainDb: postgres.AddDatabase("tenant-dev-supplychaindb", "supplychaindb"),
        TenantDevInventoryDb: postgres.AddDatabase("tenant-dev-inventorydb", "inventorydb"),
        TenantDevProductionDb: postgres.AddDatabase("tenant-dev-productiondb", "productiondb"),
        Redis: redis,
        RabbitMq: builder.AddRabbitMQ("rabbitmq"));
}

static AppHostDomainServices AddDomainServices(
    IDistributedApplicationBuilder builder,
    AppHostInfrastructure infra,
    AppHostParameters parameters)
{
    var tenantManagement = builder.AddProject<Projects.RailFactory_Tenancy_Api>("tenant-management")
        .WithReference(infra.TenantCatalogDb)
        .WithReference(infra.TenantDevIamDb)
        .WithReference(infra.TenantDevSupplyChainDb)
        .WithReference(infra.TenantDevInventoryDb)
        .WithReference(infra.TenantDevProductionDb)
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("TenantRouting__CatalogCacheTtlSeconds", TenantCatalogCacheTtlSeconds)
        .WaitFor(infra.TenantCatalogDb);

    var iam = builder.AddProject<Projects.RailFactory_Iam_Api>("identity-access-management")
        .WithReference(tenantManagement)
        .WithReference(infra.TenantCatalogDb)
        .WithReference(infra.TenantDevIamDb)
        .WithReference(infra.Redis)
        .WithEnvironment("TenantRouting__ServiceKey", "iamdb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("Authentication__Google__ClientId", parameters.OAuth.GoogleClientId)
        .WithEnvironment("Authentication__Google__ClientSecret", parameters.OAuth.GoogleClientSecret)
        .WithEnvironment("Authentication__Google__PublicOrigin", parameters.Edge.FrontendPublicOrigin)
        .WaitFor(infra.TenantCatalogDb)
        .WaitFor(infra.TenantDevIamDb)
        .WaitFor(infra.Redis)
        .WaitFor(tenantManagement);

    var supplyChain = builder.AddProject<Projects.RailFactory_SupplyChain_Api>("supply-chain")
        .WithReference(tenantManagement)
        .WithReference(infra.TenantCatalogDb)
        .WithReference(infra.TenantDevSupplyChainDb)
        .WithEnvironment("TenantRouting__ServiceKey", "supplychaindb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WaitFor(infra.TenantCatalogDb)
        .WaitFor(infra.TenantDevSupplyChainDb)
        .WaitFor(tenantManagement);

    var inventory = builder.AddProject<Projects.RailFactory_Inventory_Api>("inventory")
        .WithReference(tenantManagement)
        .WithReference(infra.TenantCatalogDb)
        .WithReference(infra.TenantDevInventoryDb)
        .WithEnvironment("TenantRouting__ServiceKey", "inventorydb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WaitFor(infra.TenantCatalogDb)
        .WaitFor(infra.TenantDevInventoryDb)
        .WaitFor(tenantManagement);

    var production = builder.AddProject<Projects.RailFactory_Production_Api>("production")
        .WithReference(tenantManagement)
        .WithReference(infra.TenantCatalogDb)
        .WithReference(infra.TenantDevProductionDb)
        .WithReference(infra.RabbitMq)
        .WithEnvironment("TenantRouting__ServiceKey", "productiondb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WaitFor(infra.TenantCatalogDb)
        .WaitFor(infra.TenantDevProductionDb)
        .WaitFor(infra.RabbitMq)
        .WaitFor(tenantManagement);

    return new AppHostDomainServices(tenantManagement, iam, supplyChain, inventory, production);
}

static IResourceBuilder<ProjectResource> AddEdgeServices(
    IDistributedApplicationBuilder builder,
    AppHostDomainServices services,
    AppHostEdgeParameters edge)
{
    var gateway = builder.AddProject<Projects.RailFactory_Gateway>("gateway")
        .WithReference(services.TenantManagement)
        .WithReference(services.Iam)
        .WithReference(services.SupplyChain)
        .WithReference(services.Inventory)
        .WithReference(services.Production)
        .WaitFor(services.TenantManagement)
        .WaitFor(services.Iam)
        .WaitFor(services.SupplyChain)
        .WaitFor(services.Inventory)
        .WaitFor(services.Production);

    return builder.AddProject<Projects.RailFactory_Frontend>("frontend")
        .WithReference(gateway)
        .WithEnvironment("Frontend__PublicOrigin", edge.FrontendPublicOrigin)
        .WithExternalHttpEndpoints()
        .WaitFor(gateway);
}

static void AddLocalFrontendUi(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> frontend)
{
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
        .WithEnvironment("VITE_DEV_BFF_TARGET", frontend.GetEndpoint("http"))
        .WithHttpEndpoint(port: 5082, targetPort: 5082, env: "PORT", isProxied: false)
        .WaitFor(frontend);
}

file sealed record AppHostParameters(
    IResourceBuilder<ParameterResource> PostgresPassword,
    AppHostOAuthParameters OAuth,
    AppHostEdgeParameters Edge);

file sealed record AppHostOAuthParameters(
    IResourceBuilder<ParameterResource> GoogleClientId,
    IResourceBuilder<ParameterResource> GoogleClientSecret);

file sealed record AppHostEdgeParameters(
    IResourceBuilder<ParameterResource> FrontendPublicOrigin);

file sealed record AppHostInfrastructure(
    IResourceBuilder<PostgresDatabaseResource> TenantCatalogDb,
    IResourceBuilder<PostgresDatabaseResource> TenantDevIamDb,
    IResourceBuilder<PostgresDatabaseResource> TenantDevSupplyChainDb,
    IResourceBuilder<PostgresDatabaseResource> TenantDevInventoryDb,
    IResourceBuilder<PostgresDatabaseResource> TenantDevProductionDb,
    IResourceBuilder<RedisResource> Redis,
    IResourceBuilder<RabbitMQServerResource> RabbitMq);

file sealed record AppHostDomainServices(
    IResourceBuilder<ProjectResource> TenantManagement,
    IResourceBuilder<ProjectResource> Iam,
    IResourceBuilder<ProjectResource> SupplyChain,
    IResourceBuilder<ProjectResource> Inventory,
    IResourceBuilder<ProjectResource> Production);
