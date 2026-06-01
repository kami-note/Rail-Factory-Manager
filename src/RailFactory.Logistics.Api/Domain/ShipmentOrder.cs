namespace RailFactory.Logistics.Api.Domain;

public enum ShipmentOrderStatus { Draft, Picking, Packing, ReadyToShip, Shipped, Cancelled }

public sealed class ShipmentItem
{
    public Guid Id { get; private set; }
    public Guid ShipmentOrderId { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal WeightKg { get; private set; }
    public decimal VolumeCbm { get; private set; }

    private ShipmentItem() { }

    public static ShipmentItem Create(Guid shipmentOrderId, string materialCode,
        decimal quantity, string unitOfMeasure, decimal weightKg, decimal volumeCbm)
    {
        return new ShipmentItem
        {
            Id = Guid.NewGuid(),
            ShipmentOrderId = shipmentOrderId,
            MaterialCode = materialCode.Trim().ToUpperInvariant(),
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure.Trim(),
            WeightKg = weightKg,
            VolumeCbm = volumeCbm
        };
    }
}

public sealed class ShipmentOrder
{
    private readonly List<ShipmentItem> _items = [];

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid? ProductionOrderRef { get; private set; }
    public string? Notes { get; private set; }
    public ShipmentOrderStatus Status { get; private set; }

    /// <summary>Optional delivery coordinates for heat-map visualisation (RF-35).</summary>
    public decimal? DeliveryLatitudeDeg { get; private set; }
    public decimal? DeliveryLongitudeDeg { get; private set; }
    public string? DeliveryCity { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<ShipmentItem> Items => _items.AsReadOnly();

    private ShipmentOrder() { }

    public static ShipmentOrder Create(Guid? productionOrderRef, string? notes,
        decimal? deliveryLatitudeDeg = null, decimal? deliveryLongitudeDeg = null, string? deliveryCity = null)
    {
        var now = DateTimeOffset.UtcNow;
        var orderNumber = $"EXP-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        return new ShipmentOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            ProductionOrderRef = productionOrderRef,
            Notes = notes?.Trim(),
            Status = ShipmentOrderStatus.Draft,
            DeliveryLatitudeDeg = deliveryLatitudeDeg,
            DeliveryLongitudeDeg = deliveryLongitudeDeg,
            DeliveryCity = deliveryCity?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public ShipmentItem AddItem(string materialCode, decimal quantity, string unitOfMeasure,
        decimal weightKg, decimal volumeCbm)
    {
        if (Status != ShipmentOrderStatus.Draft)
            throw new InvalidOperationException("Items can only be added to Draft orders.");
        var item = ShipmentItem.Create(Id, materialCode, quantity, unitOfMeasure, weightKg, volumeCbm);
        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
        return item;
    }

    public void StartPicking()
    {
        if (Status != ShipmentOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft orders can start picking.");
        Status = ShipmentOrderStatus.Picking;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StartPacking()
    {
        if (Status != ShipmentOrderStatus.Picking)
            throw new InvalidOperationException("Only Picking orders can start packing.");
        Status = ShipmentOrderStatus.Packing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReadyToShip()
    {
        if (Status != ShipmentOrderStatus.Packing)
            throw new InvalidOperationException("Only Packing orders can be marked ReadyToShip.");
        Status = ShipmentOrderStatus.ReadyToShip;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkShipped()
    {
        if (Status != ShipmentOrderStatus.ReadyToShip)
            throw new InvalidOperationException("Only ReadyToShip orders can be shipped.");
        Status = ShipmentOrderStatus.Shipped;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is ShipmentOrderStatus.Shipped or ShipmentOrderStatus.Cancelled)
            throw new InvalidOperationException("Shipped or cancelled orders cannot be cancelled.");
        Status = ShipmentOrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
