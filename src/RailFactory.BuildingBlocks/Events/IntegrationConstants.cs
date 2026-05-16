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
    }
}
