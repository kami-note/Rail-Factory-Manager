using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

/// <summary>
/// Seeds initial local users and system roles for local development and testing.
/// </summary>
public static class IamLocalUsersDataSeeder
{
    /// <summary>
    /// Seeds default system roles.
    /// </summary>
    public static async Task SeedTenantRolesAsync(
        IamAuthDbContext dbContext,
        string tenantCode,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var hasRoles = await dbContext.Roles.AnyAsync(cancellationToken);
        if (hasRoles) return;

        logger.LogInformation("Seeding default roles for tenant '{TenantCode}'...", tenantCode);

        dbContext.Roles.AddRange(
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Administrador do Sistema",
                Description = "Acesso total a todos os módulos e gestão de usuários.",
                Permissions = SystemPermissions.All().ToList(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Operador de Logística",
                Description = "Gerencia expedição, despachos, transportadoras, recebimentos e estoque.",
                Permissions =
                [
                    SystemPermissions.Inventory.Read, SystemPermissions.Inventory.Write,
                    SystemPermissions.SupplyChain.Read, SystemPermissions.SupplyChain.Write,
                    SystemPermissions.Logistics.Read, SystemPermissions.Logistics.Write,
                    SystemPermissions.Hr.Read,
                    SystemPermissions.Fleet.Read, SystemPermissions.Fleet.Write,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Consulta (Apenas Leitura)",
                Description = "Acesso de visualização para todos os módulos.",
                Permissions =
                [
                    SystemPermissions.Inventory.Read, SystemPermissions.SupplyChain.Read,
                    SystemPermissions.Production.Read, SystemPermissions.Iam.Read,
                    SystemPermissions.Hr.Read, SystemPermissions.Fleet.Read, SystemPermissions.Logistics.Read,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Supervisor de RH e Frota",
                Description = "Gerencia cadastro de pessoas, apontamentos de horas, veículos e alocações de motoristas.",
                Permissions =
                [
                    SystemPermissions.Hr.Read, SystemPermissions.Hr.Write,
                    SystemPermissions.Fleet.Read, SystemPermissions.Fleet.Write,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Operador de Produção",
                Description = "Gerencia ordens de produção, BOMs, centros de trabalho e consulta estoque.",
                Permissions =
                [
                    SystemPermissions.Production.Read, SystemPermissions.Production.Write,
                    SystemPermissions.Inventory.Read, SystemPermissions.Hr.Read,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Responsável Fiscal",
                Description = "Gerencia documentos fiscais NF-e de saída: emissão, reemissão e monitor fiscal.",
                Permissions =
                [
                    SystemPermissions.Logistics.Read, SystemPermissions.Logistics.Fiscal,
                    SystemPermissions.Inventory.Read, SystemPermissions.SupplyChain.Read,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds default users and user-role mappings if environment is Development.
    /// </summary>
    public static async Task SeedTenantUsersAsync(
        IamAuthDbContext dbContext,
        string tenantCode,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        var hasUsers = await dbContext.LocalUsers.AnyAsync(cancellationToken);
        if (hasUsers) return;

        logger.LogInformation("Seeding default users for tenant '{TenantCode}'...", tenantCode);

        // 1. Create Users
        var users = new List<IamLocalUserRecord>
        {
            new() { ExternalProvider = "google", ExternalSubject = "111111111111111111111", Email = "yurinote666@gmail.com", DisplayName = "Yuri Note", FirstLoginAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new() { ExternalProvider = "google", ExternalSubject = "104339721309686158509", Email = "yurinote666@gmail.com", DisplayName = "Yuzi Otaku", FirstLoginAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new() { ExternalProvider = "google", ExternalSubject = "222222222222222222222", Email = "admin@railfactory.com.br", DisplayName = "Admin RailFactory", FirstLoginAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new() { ExternalProvider = "google", ExternalSubject = "333333333333333333333", Email = "logistica@railfactory.com.br", DisplayName = "Expedição RailFactory", FirstLoginAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new() { ExternalProvider = "google", ExternalSubject = "444444444444444444444", Email = "producao@railfactory.com.br", DisplayName = "Chão de Fábrica", FirstLoginAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new() { ExternalProvider = "google", ExternalSubject = "555555555555555555555", Email = "rh@railfactory.com.br", DisplayName = "Recursos Humanos", FirstLoginAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        };

        dbContext.LocalUsers.AddRange(users);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 2. Map Roles
        var adminRole = await dbContext.Roles.FirstAsync(r => r.Name == "Administrador do Sistema" && r.TenantCode == tenantCode, cancellationToken);
        var logisticaRole = await dbContext.Roles.FirstAsync(r => r.Name == "Operador de Logística" && r.TenantCode == tenantCode, cancellationToken);
        var producaoRole = await dbContext.Roles.FirstAsync(r => r.Name == "Operador de Produção" && r.TenantCode == tenantCode, cancellationToken);
        var rhRole = await dbContext.Roles.FirstAsync(r => r.Name == "Supervisor de RH e Frota" && r.TenantCode == tenantCode, cancellationToken);

        dbContext.UserRoles.AddRange(
            new IamTenantUserRoleRecord { TenantCode = tenantCode, ExternalProvider = "google", ExternalSubject = "111111111111111111111", RoleId = adminRole.Id, AssignedAt = DateTimeOffset.UtcNow },
            new IamTenantUserRoleRecord { TenantCode = tenantCode, ExternalProvider = "google", ExternalSubject = "104339721309686158509", RoleId = adminRole.Id, AssignedAt = DateTimeOffset.UtcNow },
            new IamTenantUserRoleRecord { TenantCode = tenantCode, ExternalProvider = "google", ExternalSubject = "222222222222222222222", RoleId = adminRole.Id, AssignedAt = DateTimeOffset.UtcNow },
            new IamTenantUserRoleRecord { TenantCode = tenantCode, ExternalProvider = "google", ExternalSubject = "333333333333333333333", RoleId = logisticaRole.Id, AssignedAt = DateTimeOffset.UtcNow },
            new IamTenantUserRoleRecord { TenantCode = tenantCode, ExternalProvider = "google", ExternalSubject = "444444444444444444444", RoleId = producaoRole.Id, AssignedAt = DateTimeOffset.UtcNow },
            new IamTenantUserRoleRecord { TenantCode = tenantCode, ExternalProvider = "google", ExternalSubject = "555555555555555555555", RoleId = rhRole.Id, AssignedAt = DateTimeOffset.UtcNow }
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
