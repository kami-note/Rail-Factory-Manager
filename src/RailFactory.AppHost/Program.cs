using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

const string DefaultTenantCode = "dev";
const string TenantCatalogCacheTtlSeconds = "60";

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("env");

ValidateSecrets(builder.Configuration);

var parameters = AddParameters(builder);
var infra = AddInfrastructure(builder, parameters);
var services = AddDomainServices(builder, infra, parameters);
var frontend = AddEdgeServices(builder, services, infra, parameters);

if (builder.ExecutionContext.IsRunMode)
{
    AddLocalFrontendUi(builder, frontend);
}

builder.Build().Run();

static AppHostParameters AddParameters(IDistributedApplicationBuilder builder)
{
    return new AppHostParameters(
        PostgresPassword: builder.AddParameter("postgres-password", "rail-factory-dev-postgres", secret: true),
        InternalApiKey: builder.AddParameter("internal-api-key", secret: true),
        InternalTokenSigningKey: builder.AddParameter("internal-token-signing-key", secret: true),
        TenancyKek: builder.AddParameter("tenancy-kek", secret: true),
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
        .WithDataVolume("rail-factory-fork-postgres-data-v4")
        .WithPgAdmin();

    var redis = builder.AddRedis("redis")
        .WithDataVolume("rail-factory-fork-redis-data");

    var minio = builder.AddContainer("minio", "minio/minio")
        .WithImageTag("latest")
        .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
        .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
        .WithArgs("server", "/data", "--console-address", ":9001")
        .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
        .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
        .WithAnnotation(new ContainerMountAnnotation("rail-factory-fork-minio-data", "/data", ContainerMountType.Volume, false));

    return new AppHostInfrastructure(
        Postgres: postgres,
        TenantCatalogDb: postgres.AddDatabase("tenantcatalog"),
        Redis: redis,
        RabbitMq: builder.AddRabbitMQ("rabbitmq"),
        Minio: minio);
}

static AppHostDomainServices AddDomainServices(
    IDistributedApplicationBuilder builder,
    AppHostInfrastructure infra,
    AppHostParameters parameters)
{
    var tenantManagement = builder.AddProject<Projects.RailFactory_Tenancy_Api>("tenant-management")
        .WithEnvironment("TENANCY__KEK", parameters.TenancyKek)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WithEnvironment("InternalApiKey", parameters.InternalApiKey)
        .WithReference(infra.Postgres)        // server connection string — used to create tenant databases at runtime
        .WithReference(infra.TenantCatalogDb)
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("TenantRouting__CatalogCacheTtlSeconds", TenantCatalogCacheTtlSeconds)
        .WaitFor(infra.TenantCatalogDb);

    var iam = builder.AddProject<Projects.RailFactory_Iam_Api>("identity-access-management")
        .WithReference(tenantManagement)
        .WithReference(infra.Redis)
        .WithEnvironment("TenantRouting__ServiceKey", "iamdb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("Authentication__Google__ClientId", parameters.OAuth.GoogleClientId)
        .WithEnvironment("Authentication__Google__ClientSecret", parameters.OAuth.GoogleClientSecret)
        .WithEnvironment("Authentication__Google__PublicOrigin", parameters.Edge.FrontendPublicOrigin)
        .WithEnvironment("Authentication__Google__CallbackPath", "/api/iam/auth/google/callback")
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WaitFor(infra.Redis)
        .WaitFor(tenantManagement);

    var inventory = builder.AddProject<Projects.RailFactory_Inventory_Api>("inventory")
        .WithReference(tenantManagement)
        .WithReference(infra.RabbitMq)
        .WithEnvironment("TenantRouting__ServiceKey", "inventorydb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("InternalApiKey", parameters.InternalApiKey)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WaitFor(infra.RabbitMq)
        .WaitFor(tenantManagement);

    var supplyChain = builder.AddProject<Projects.RailFactory_SupplyChain_Api>("supply-chain")
        .WithReference(tenantManagement)
        .WithReference(inventory)
        .WithReference(infra.RabbitMq)
        .WithEnvironment("TenantRouting__ServiceKey", "supplychaindb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("InternalApiKey", parameters.InternalApiKey)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WaitFor(infra.RabbitMq)
        .WaitFor(tenantManagement)
        .WaitFor(inventory);

    var production = builder.AddProject<Projects.RailFactory_Production_Api>("production")
        .WithReference(tenantManagement)
        .WithReference(infra.RabbitMq)
        .WithEnvironment("TenantRouting__ServiceKey", "productiondb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("InternalApiKey", parameters.InternalApiKey)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WaitFor(infra.RabbitMq)
        .WaitFor(tenantManagement);

    var humanResources = builder.AddProject<Projects.RailFactory_HumanResources_Api>("human-resources")
        .WithReference(tenantManagement)
        .WithEnvironment("TenantRouting__ServiceKey", "hrdb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("InternalApiKey", parameters.InternalApiKey)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WaitFor(tenantManagement);

    var fleet = builder.AddProject<Projects.RailFactory_Fleet_Api>("fleet")
        .WithReference(tenantManagement)
        .WithEnvironment("TenantRouting__ServiceKey", "fleetdb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("InternalApiKey", parameters.InternalApiKey)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WaitFor(tenantManagement);

    var logistics = builder.AddProject<Projects.RailFactory_Logistics_Api>("logistics")
        .WithReference(tenantManagement)
        .WithReference(infra.RabbitMq)
        .WithEnvironment("TenantRouting__ServiceKey", "logisticsdb")
        .WithEnvironment("TenantRouting__DefaultTenantCode", DefaultTenantCode)
        .WithEnvironment("InternalApiKey", parameters.InternalApiKey)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WaitFor(infra.RabbitMq)
        .WaitFor(tenantManagement);

    return new AppHostDomainServices(tenantManagement, iam, supplyChain, inventory, production, humanResources, fleet, logistics);
}

static IResourceBuilder<ProjectResource> AddEdgeServices(
    IDistributedApplicationBuilder builder,
    AppHostDomainServices services,
    AppHostInfrastructure infra,
    AppHostParameters parameters)
{
    var gateway = builder.AddProject<Projects.RailFactory_Gateway>("gateway")
        .WithReference(services.TenantManagement)
        .WithReference(services.Iam)
        .WithReference(services.SupplyChain)
        .WithReference(services.Inventory)
        .WithReference(services.Production)
        .WithReference(services.HumanResources)
        .WithReference(services.Fleet)
        .WithReference(services.Logistics)
        .WaitFor(services.TenantManagement)
        .WaitFor(services.Iam)
        .WaitFor(services.SupplyChain)
        .WaitFor(services.Inventory)
        .WaitFor(services.Production)
        .WaitFor(services.HumanResources)
        .WaitFor(services.Fleet)
        .WaitFor(services.Logistics);

    return builder.AddProject<Projects.RailFactory_Frontend>("frontend")
        .WithReference(gateway)
        .WithEnvironment("Frontend__PublicOrigin", parameters.Edge.FrontendPublicOrigin)
        .WithEnvironment("InternalToken__SigningKey", parameters.InternalTokenSigningKey)
        .WithEnvironment("Minio__Endpoint", "http://localhost:9000")
        .WithEnvironment("Minio__AccessKey", "minioadmin")
        .WithEnvironment("Minio__SecretKey", "minioadmin")
        .WithExternalHttpEndpoints()
        .WaitFor(gateway)
        .WaitFor(infra.Minio);
}

static void AddLocalFrontendUi(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> frontend)
{
    builder.AddExecutable(
            "frontend-ui",
            "sh",
            "../RailFactory.Frontend/App",
            "-c",
            "npm run dev -- --host 0.0.0.0 --port 5082")
        .WithReference(frontend)
        .WithEnvironment("VITE_DEV_BFF_TARGET", frontend.GetEndpoint("http"))
        .WithHttpEndpoint(port: 5082, targetPort: 5082, env: "PORT", isProxied: false)
        .WaitFor(frontend);
}

static void ValidateSecrets(IConfiguration configuration)
{
    var signingKey = configuration["Parameters:internal-token-signing-key"] ?? string.Empty;
    if (System.Text.Encoding.UTF8.GetByteCount(signingKey) < 32)
    {
        throw new InvalidOperationException(
            $"Parameters:internal-token-signing-key is too short ({System.Text.Encoding.UTF8.GetByteCount(signingKey)} bytes). " +
            "HS256 requires at least 32 bytes (256 bits). Fix with:\n" +
            "  dotnet user-secrets set \"Parameters:internal-token-signing-key\" " +
            $"\"$(python3 -c 'import secrets; print(secrets.token_hex(32))')\"" +
            "\nRun this command from the RailFactory.AppHost directory.");
    }

    var kek = configuration["Parameters:tenancy-kek"] ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(kek))
    {
        try
        {
            var kekBytes = Convert.FromBase64String(kek);
            if (kekBytes.Length < 32)
                throw new InvalidOperationException(
                    $"Parameters:tenancy-kek must decode to at least 32 bytes (256 bits). Current: {kekBytes.Length} bytes.");
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Parameters:tenancy-kek must be a valid Base64 string. Generate with:\n" +
                "  dotnet user-secrets set \"Parameters:tenancy-kek\" " +
                $"\"$(python3 -c 'import secrets,base64; print(base64.b64encode(secrets.token_bytes(32)).decode())')\"" +
                "\nRun this command from the RailFactory.AppHost directory.");
        }
    }
}

file sealed record AppHostParameters(
    IResourceBuilder<ParameterResource> PostgresPassword,
    IResourceBuilder<ParameterResource> InternalApiKey,
    IResourceBuilder<ParameterResource> InternalTokenSigningKey,
    IResourceBuilder<ParameterResource> TenancyKek,
    AppHostOAuthParameters OAuth,
    AppHostEdgeParameters Edge);

file sealed record AppHostOAuthParameters(
    IResourceBuilder<ParameterResource> GoogleClientId,
    IResourceBuilder<ParameterResource> GoogleClientSecret);

file sealed record AppHostEdgeParameters(
    IResourceBuilder<ParameterResource> FrontendPublicOrigin);

file sealed record AppHostInfrastructure(
    IResourceBuilder<PostgresServerResource> Postgres,
    IResourceBuilder<PostgresDatabaseResource> TenantCatalogDb,
    IResourceBuilder<RedisResource> Redis,
    IResourceBuilder<RabbitMQServerResource> RabbitMq,
    IResourceBuilder<ContainerResource> Minio);

file sealed record AppHostDomainServices(
    IResourceBuilder<ProjectResource> TenantManagement,
    IResourceBuilder<ProjectResource> Iam,
    IResourceBuilder<ProjectResource> SupplyChain,
    IResourceBuilder<ProjectResource> Inventory,
    IResourceBuilder<ProjectResource> Production,
    IResourceBuilder<ProjectResource> HumanResources,
    IResourceBuilder<ProjectResource> Fleet,
    IResourceBuilder<ProjectResource> Logistics);
