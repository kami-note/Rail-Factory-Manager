namespace RailFactory.BuildingBlocks.Auth;

/// <summary>
/// Central registry of all atomic permissions available in the system.
/// </summary>
public static class SystemPermissions
{
    public static class Inventory
    {
        public const string Read = "inventory.read";
        public const string Write = "inventory.write";
        public const string Delete = "inventory.delete";
    }

    public static class SupplyChain
    {
        public const string Read = "supplychain.read";
        public const string Write = "supplychain.write";
        public const string Admin = "supplychain.admin";
    }

    public static class Production
    {
        public const string Read = "production.read";
        public const string Write = "production.write";
    }

    public static class Iam
    {
        public const string Read = "iam.read";
        public const string Write = "iam.write";
        public const string RolesManage = "iam.roles.manage";
    }

    public static class Hr
    {
        public const string Read = "hr.read";
        public const string Write = "hr.write";
    }

    public static class Fleet
    {
        public const string Read = "fleet.read";
        public const string Write = "fleet.write";
    }

    public static class Logistics
    {
        public const string Read = "logistics.read";
        public const string Write = "logistics.write";
    }

    public static class Tenancy
    {
        public const string Admin = "tenancy.admin";
    }

    /// <summary>
    /// Returns all available permission codes for validation or UI selection.
    /// </summary>
    public static IEnumerable<string> All()
    {
        yield return Inventory.Read;
        yield return Inventory.Write;
        yield return Inventory.Delete;
        yield return SupplyChain.Read;
        yield return SupplyChain.Write;
        yield return SupplyChain.Admin;
        yield return Production.Read;
        yield return Production.Write;
        yield return Iam.Read;
        yield return Iam.Write;
        yield return Iam.RolesManage;
        yield return Hr.Read;
        yield return Hr.Write;
        yield return Fleet.Read;
        yield return Fleet.Write;
        yield return Logistics.Read;
        yield return Logistics.Write;
        yield return Tenancy.Admin;
    }
}
