namespace RailFactory.HumanResources.Api.Domain;

public static class HrStatusExtensions
{
    public static object ToDisplayStatus(this PersonStatus status) => status switch
    {
        PersonStatus.Active   => new { Key = "active",   Label = "Ativo",   Color = "success" },
        PersonStatus.Inactive => new { Key = "inactive", Label = "Inativo", Color = "default" },
        _                     => new { Key = "unknown",  Label = "?",       Color = "default" }
    };

    public static object ToDisplayType(this PersonType type) => type switch
    {
        PersonType.Employee   => new { Key = "employee",   Label = "Colaborador",  Color = "primary" },
        PersonType.Driver     => new { Key = "driver",     Label = "Motorista",    Color = "info" },
        PersonType.Contractor => new { Key = "contractor", Label = "Terceirizado", Color = "warning" },
        _                     => new { Key = "unknown",    Label = "?",            Color = "default" }
    };
}
