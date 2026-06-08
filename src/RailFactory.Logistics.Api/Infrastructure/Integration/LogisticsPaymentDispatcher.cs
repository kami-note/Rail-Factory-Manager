using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Integrations;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Infrastructure.Adapters.Payment;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Integration;

public sealed class LogisticsPaymentDispatcher(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<LogisticsPaymentDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LogisticsPaymentDispatcher encountered an unexpected error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchAllTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
        var activeTenants = await catalogClient.ListAllAsync(cancellationToken);

        foreach (var tenant in activeTenants.Where(t => t.IsActive))
        {
            try
            {
                await DispatchTenantBatchAsync(tenant, cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("was not found in configuration"))
            {
                logger.LogDebug("Database for tenant {TenantCode} is not provisioned yet. Skipping payment dispatch.", tenant.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LogisticsPaymentDispatcher failed for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        var db = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
        var integrationClient = scope.ServiceProvider.GetRequiredService<ITenantIntegrationClient>();

        if (!await TenantServiceReadiness.IsReadyAsync(db.Database.GetDbConnection(), cancellationToken))
            return;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var pending = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "logistics_outbox"
                WHERE "EventType" = {0}
                  AND "DispatchedAt" IS NULL
                  AND "DeadLetteredAt" IS NULL
                ORDER BY "OccurredAt" ASC
                LIMIT 20
                FOR UPDATE SKIP LOCKED
                """, IntegrationConstants.LogisticsEvents.PaymentChargeRequested)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        using var config = await integrationClient.GetConfigAsync(tenant.Code, "payment", cancellationToken);

        if (config is null)
        {
            logger.LogDebug("No payment integration configured for tenant {TenantCode}. Skipping batch.", tenant.Code);
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        var adapter = PaymentGatewayAdapterBuilder.Build(config, httpClientFactory);
        config.Credentials.TryGetString("billing_type", out var billingType);
        var effectiveBillingType = string.IsNullOrWhiteSpace(billingType) ? "BOLETO" : billingType.ToUpperInvariant();

        foreach (var message in pending)
        {
            try
            {
                await ProcessMessageAsync(db, adapter, effectiveBillingType, message, tenant.Code, cancellationToken);
                message.MarkDispatched();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Payment charge request failed for message {MessageId} (tenant {TenantCode}).",
                    message.Id, tenant.Code);
                if (message.AttemptCount >= 4)
                    message.MarkDeadLetter(ex.Message);
                else
                    message.MarkTransientFailure(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        LogisticsDbContext db,
        IPaymentGatewayAdapter adapter,
        string billingType,
        Domain.LogisticsOutboxMessage message,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(message.Payload);
        var dispatchId = doc.RootElement.GetProperty("dispatchId").GetGuid();
        var shipmentOrderId = doc.RootElement.GetProperty("shipmentOrderId").GetGuid();

        var dispatch = await db.Dispatches.FirstOrDefaultAsync(d => d.Id == dispatchId, cancellationToken);
        if (dispatch is null)
        {
            logger.LogWarning("Payment: dispatch {DispatchId} not found.", dispatchId);
            return;
        }

        if (!string.IsNullOrEmpty(dispatch.PaymentExternalId))
        {
            logger.LogInformation(
                "Dispatch {DispatchId} already has PaymentExternalId={Id}. Skipping.",
                dispatchId, dispatch.PaymentExternalId);
            return;
        }

        var order = await db.ShipmentOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == shipmentOrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Payment: shipment order {OrderId} not found.", shipmentOrderId);
            return;
        }

        var totalValue = order.Items.Sum(i => i.UnitValue * i.Quantity);
        if (totalValue <= 0) totalValue = dispatch.FreightValueBrl;

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        var chargeRequest = new PaymentChargeRequest(
            TenantId: tenantCode,
            ExternalReference: dispatch.TrackingCode,
            CustomerName: order.RecipientName ?? "Destinatário",
            CustomerCpfCnpj: order.RecipientCnpj ?? "00000000000000",
            CustomerEmail: order.RecipientEmail ?? string.Empty,
            ValueBrl: totalValue > 0 ? totalValue : 1m,
            Description: $"Pedido {order.OrderNumber} — {order.NatureOfOperation}",
            DueDate: dueDate,
            BillingType: billingType);

        var result = await adapter.CreateChargeAsync(chargeRequest, cancellationToken);

        if (result.Status == "error")
            throw new InvalidOperationException(result.ErrorMessage ?? "Payment provider returned an error.");

        dispatch.UpdatePaymentStatus(result.ExternalId, result.Status, result.BoletoUrl, result.PixUrl, result.ErrorMessage);

        logger.LogInformation(
            "Payment charge created for dispatch {DispatchId}: externalId={Id} status={Status}",
            dispatchId, result.ExternalId, result.Status);
    }
}
