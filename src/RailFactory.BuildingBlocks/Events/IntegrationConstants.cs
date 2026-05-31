namespace RailFactory.BuildingBlocks.Events;

/// <summary>
/// Defines system-wide integration event names and internal API paths.
/// </summary>
public static class IntegrationConstants
{
    public static class Events
    {
        public const string ReceiptItemRegistered = "supply.receipt_item_registered";
        public const string ReceiptItemConferred = "supply.receipt_item_conferred";
        public const string SupplierMaterialMappingCreated = "supply.supplier_material_mapping_created";
    }

    public static class ApiPaths
    {
        public const string InternalPendingBalances = "/api/inventory/internal/pending-balances";
        public const string InternalConfirmedBalances = "/api/inventory/internal/confirmed-balances";
        public const string InternalReserveBalances = "/api/inventory/internal/reserve-balances";
        public const string InternalSupplierMaterialMapping = "/api/inventory/internal/supplier-material-mapping";
    }

    public static class ProductionEvents
    {
        public const string ProductionOrderReleased = "production_order_released";

        /// <summary>
        /// Published per BOM item when a production order is released.
        /// Replaces the HTTP fan-out; Inventory consumes this to create a stock reservation.
        /// </summary>
        public const string StockReservationRequested = "production.stock_reservation_requested";
    }

    public static class LogisticsEvents
    {
        /// <summary>
        /// Published per shipment item when a dispatch is marked as shipped.
        /// Inventory consumes this to debit available stock.
        /// </summary>
        public const string ShipmentDispatched = "logistics.shipment_dispatched";

        /// <summary>
        /// Published once per dispatch status change. Processed by LogisticsWebhookDispatcher
        /// to POST the status update to the carrier's webhook URL.
        /// </summary>
        public const string WebhookNotification = "logistics.webhook_notification";
    }

    /// <summary>
    /// RabbitMQ exchange names used by publishers and the topology declarator.
    /// </summary>
    public static class Exchanges
    {
        public const string SupplyChain = "railfactory.supply-chain";
        public const string Production = "railfactory.production";
        public const string Logistics = "railfactory.logistics";
        public const string DeadLetter = "railfactory.dlx";
    }

    /// <summary>
    /// RabbitMQ queue names consumed by the Inventory integration consumer.
    /// </summary>
    public static class Queues
    {
        public const string InventorySupplyIntegration = "inventory.supply.integration";
        public const string InventoryProductionIntegration = "inventory.production.integration";
        public const string InventoryLogisticsIntegration = "inventory.logistics.integration";
        public const string DeadLetters = "railfactory.dead-letters";
    }
}
