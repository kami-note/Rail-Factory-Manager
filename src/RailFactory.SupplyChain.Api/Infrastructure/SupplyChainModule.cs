using Microsoft.EntityFrameworkCore;
using RailFactory.SupplyChain.Api.Application;
using RailFactory.SupplyChain.Api.Application.Integration;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Application.Receiving;
using RailFactory.SupplyChain.Api.Application.Suppliers;
using RailFactory.SupplyChain.Api.Infrastructure.Integration;
using RailFactory.SupplyChain.Api.Infrastructure.Persistence;

namespace RailFactory.SupplyChain.Api.Infrastructure;

public static class SupplyChainModule
{
    public static IServiceCollection AddSupplyChainModule(this IServiceCollection services, IConfiguration configuration)
    {
        var supplyChainConnectionString = ResolveSupplyChainConnectionString(configuration)
            ?? throw new InvalidOperationException(
                "SupplyChain database connection string is required. Configure ConnectionStrings:supplychaindb or ConnectionStrings:tenant-dev-supplychaindb.");

        services.AddDbContext<SupplyChainDbContext>(options => options.UseNpgsql(supplyChainConnectionString));
        services.AddHostedService<SupplyChainSchemaInitializer>();
        services.AddHostedService<InventoryPendingBalanceDispatcher>();

        services.AddHttpClient("inventory-integration", client =>
        {
            client.BaseAddress = new Uri("http://inventory");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<ISupplyChainRepository, PostgresSupplyChainRepository>();
        services.AddScoped<ISupplyOutbox, SupplyOutboxStore>();
        services.AddScoped<ISupplyOutboxDiagnostics, PostgresSupplyOutboxDiagnostics>();
        services.AddScoped<ISupplyChainTransactionRunner, EfSupplyChainTransactionRunner>();
        services.AddScoped<INfeProvider, BasicXmlNfeProvider>();

        services.AddScoped<GetSupplyChainInfo>();
        services.AddScoped<MaterialReceiptWriter>();
        services.AddScoped<XmlReceiptBatchParser>();
        services.AddScoped<CreateSupplier>();
        services.AddScoped<CreateManualReceipt>();
        services.AddScoped<ImportXmlReceipt>();
        services.AddScoped<ImportXmlReceiptBatch>();
        services.AddScoped<ListReceipts>();
        services.AddScoped<ListSupplyOutboxDeadLetters>();

        return services;
    }

    private static string? ResolveSupplyChainConnectionString(IConfiguration configuration)
    {
        var primary = configuration.GetConnectionString("supplychaindb");
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return configuration.GetConnectionString("tenant-dev-supplychaindb");
    }
}
