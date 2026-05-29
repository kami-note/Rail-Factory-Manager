namespace RailFactory.Fleet.Api.Domain;

public static class FleetStatusExtensions
{
    public static object ToDisplayStatus(this VehicleStatus status) => status switch
    {
        VehicleStatus.Active   => new { Key = "active",   Label = "Ativo",   Color = "success" },
        VehicleStatus.Inactive => new { Key = "inactive", Label = "Inativo", Color = "default" },
        _                      => new { Key = "unknown",  Label = "?",       Color = "default" }
    };

    public static object ToDisplayType(this VehicleType type) => type switch
    {
        VehicleType.Car        => new { Key = "car",        Label = "Carro",     Color = "primary" },
        VehicleType.Truck      => new { Key = "truck",      Label = "Caminhão",  Color = "info" },
        VehicleType.Van        => new { Key = "van",        Label = "Van",       Color = "secondary" },
        VehicleType.Motorcycle => new { Key = "motorcycle", Label = "Moto",      Color = "warning" },
        _                      => new { Key = "unknown",    Label = "?",         Color = "default" }
    };
}
