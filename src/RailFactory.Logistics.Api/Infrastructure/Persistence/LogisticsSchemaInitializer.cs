using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.Logistics.Api.Domain;
using System.Reflection;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class LogisticsSchemaInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<LogisticsSchemaInitializer> logger) : BackgroundService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);
    private readonly HashSet<string> _migratedTenants = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Logistics schema initializer started.");
        await MigrateNewTenantsAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await MigrateNewTenantsAsync(stoppingToken);
    }

    private async Task MigrateNewTenantsAsync(CancellationToken cancellationToken)
    {
        List<TenantResolutionResult> pending;
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
            var all = await client.ListAllAsync(cancellationToken);
            pending = all.Where(t => t.IsActive && !_migratedTenants.Contains(t.Code)).ToList();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not fetch tenant list from catalog. Will retry in 15s.");
            return;
        }

        if (pending.Count == 0) return;

        logger.LogInformation("Migrating Logistics databases for {Count} new tenant(s)...", pending.Count);
        await Task.WhenAll(pending.Select(t => MigrateTenantAsync(t, cancellationToken)));
    }

    private async Task MigrateTenantAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        await MigrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var tenantScope = serviceProvider.CreateScope();
            var scopedContextAccessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            scopedContextAccessor.Current = new TenantContext(
                tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
            await SeedLogisticsDataAsync(dbContext, tenant.Code, cancellationToken);
            await TenantServiceReadiness.MarkReadyAsync(dbContext.Database.GetDbConnection(), cancellationToken);

            _migratedTenants.Add(tenant.Code);
            logger.LogInformation("Logistics database for tenant '{TenantCode}' migrated.", tenant.Code);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to migrate Logistics database for tenant '{TenantCode}'. Will retry.", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    private async Task SeedLogisticsDataAsync(
        LogisticsDbContext dbContext,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        // 1. Create Carriers
        // TransRápido Sorocaba Ltda (CNPJ: 12345678000100)
        var carrier1 = await dbContext.Carriers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.DocumentNumber == "12345678000100", cancellationToken);
        if (carrier1 == null)
        {
            carrier1 = Carrier.Create("TransRápido Sorocaba Ltda", "12345678000100", "contato@transrapido.com.br", 0.45m, 12.50m, "https://api.transrapido.com.br/webhook");
            dbContext.Carriers.Add(carrier1);
        }

        // Logística Brasil S.A. (CNPJ: 98765432000111)
        var carrier2 = await dbContext.Carriers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.DocumentNumber == "98765432000111", cancellationToken);
        if (carrier2 == null)
        {
            carrier2 = Carrier.Create("Logística Brasil S.A.", "98765432000111", "contato@logisticabrasil.com.br", 0.35m, 10.00m);
            dbContext.Carriers.Add(carrier2);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 2. Create Shipment Order EXP-20260610-001 (Shipped)
        var order = await dbContext.ShipmentOrders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.OrderNumber == "EXP-20260610-001", cancellationToken);
        if (order == null)
        {
            order = ShipmentOrder.Create(
                productionOrderRef: null,
                notes: "Ordem de expedição de semente.",
                deliveryLatitudeDeg: -23.5489m,
                deliveryLongitudeDeg: -46.6388m,
                deliveryCity: "Sorocaba",
                recipientCnpj: "12345678000195", // ACME CNPJ
                recipientName: "Metalúrgica Jundiaí",
                recipientEmail: "recebimento@metalurgicajundiai.com.br",
                recipientStreet: "Rodovia Dom Gabriel Paulino Bueno Couto",
                recipientNumber: "Km 65",
                recipientDistrict: "Industrial",
                recipientCity: "Jundiaí",
                recipientState: "SP",
                recipientZipCode: "13212000",
                natureOfOperation: "Venda de mercadoria",
                recipientIe: "123456789",
                modalidadeFrete: 0
            );

            // Use reflection to force deterministic order number
            var orderNumberProp = typeof(ShipmentOrder).GetProperty("OrderNumber");
            if (orderNumberProp != null)
            {
                orderNumberProp.SetValue(order, "EXP-20260610-001");
            }

            // Add item: PRO-TR-100 (5 UN), weight: 100KG per UN (500KG total), volume: 0.1CBM per UN (0.5CBM total)
            order.AddItem("PRO-TR-100", 5m, "UN", 500m, 0.5m, ncmCode: "73021010", cfopCode: "5102", unitValue: 150m, taxBaseIcms: 750m, icmsRate: 18m);

            // Transition lifecycle
            order.StartPicking();
            order.StartPacking();
            order.MarkReadyToShip();
            order.MarkShipped();

            dbContext.ShipmentOrders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 3. Create Dispatch RF-TRJ1001 (Delivered)
            var dispatch = await dbContext.Dispatches.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.TrackingCode == "RF-TRJ1001", cancellationToken);
            if (dispatch == null)
            {
                // We use BRA2S19 vehicle (Plate: BRA2S19) and Marcos Oliveira (Driver)
                var vehicleId = Guid.Parse("99999999-9999-9999-9999-999999999999"); // Fake/deterministic vehicle Guid
                var driverId = Guid.Parse("33333333-3333-3333-3333-333333333333"); // Marcos Oliveira

                // Freight calculation: max(500 * 0.45, 0.5 * 12.50) = 225 BRL
                dispatch = Dispatch.Create(
                    order.Id,
                    carrier1.Id,
                    vehicleId,
                    driverId,
                    225.00m,
                    vehiclePlate: "BRA2S19",
                    vehicleRntrc: "12345678",
                    driverCpf: "45678912300",
                    driverName: "Marcos Oliveira"
                );

                // Use reflection to force deterministic tracking code
                var trackingCodeProp = typeof(Dispatch).GetProperty("TrackingCode");
                if (trackingCodeProp != null)
                {
                    trackingCodeProp.SetValue(dispatch, "RF-TRJ1001");
                }

                // Transition to Delivered
                dispatch.Ship();
                dispatch.Deliver();

                // Set sample fiscal document info
                dispatch.UpdateFiscalStatus("FIS-EXT-001", "Authorized", "35260612345678000100550010000010011000010012", pdfUrl: "https://fiscal.railfactory.local/pdf/35260612345678000100550010000010011000010012", xmlUrl: "https://fiscal.railfactory.local/xml/35260612345678000100550010000010011000010012");

                dbContext.Dispatches.Add(dispatch);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
