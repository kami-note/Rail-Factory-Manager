using RailFactory.BuildingBlocks.Auth;
using RailFactory.HumanResources.Api.Api.Requests;
using RailFactory.HumanResources.Api.Application.Hours;
using RailFactory.HumanResources.Api.Application.People;
using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Api;

public static class HrEndpoints
{
    private const string ApiGroup = "/api/hr";

    public static WebApplication MapHrEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}/info"));

        var group = app.MapGroup(ApiGroup);

        group.MapGet("/info", (IHostEnvironment env) =>
            Results.Ok(new { Service = "human-resources", Environment = env.EnvironmentName })
        ).AllowAnonymous();

        var secure = group.MapGroup("/").RequireAuthorization();

        // People
        secure.MapGet("/people", HandleListPersons)
            .RequirePermission(SystemPermissions.Hr.Read);

        secure.MapGet("/people/{id:guid}", HandleGetPerson)
            .RequirePermission(SystemPermissions.Hr.Read);

        secure.MapPost("/people", HandleCreatePerson)
            .RequirePermission(SystemPermissions.Hr.Write);

        secure.MapPut("/people/{id:guid}/deactivate", HandleDeactivatePerson)
            .RequirePermission(SystemPermissions.Hr.Write);

        secure.MapPut("/people/{id:guid}/activate", HandleActivatePerson)
            .RequirePermission(SystemPermissions.Hr.Write);

        // Hour Logs
        secure.MapPost("/people/{id:guid}/hour-logs", HandleLogHours)
            .RequirePermission(SystemPermissions.Hr.Write);

        secure.MapGet("/people/{id:guid}/hour-logs", HandleListHourLogs)
            .RequirePermission(SystemPermissions.Hr.Read);

        return app;
    }

    // ── People ────────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListPersons(
        string? type, string? status, ListPersons useCase, CancellationToken ct)
    {
        PersonType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<PersonType>(type, true, out var t))
            parsedType = t;

        PersonStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PersonStatus>(status, true, out var s))
            parsedStatus = s;

        var result = await useCase.ExecuteAsync(parsedType, parsedStatus, ct);
        return Results.Ok(result.Select(MapPersonResponse));
    }

    private static async Task<IResult> HandleGetPerson(
        Guid id, IPersonRepository repo, CancellationToken ct)
    {
        var person = await repo.GetByIdAsync(id, ct);
        return person is null ? Results.NotFound() : Results.Ok(MapPersonResponse(person));
    }

    private static async Task<IResult> HandleCreatePerson(
        CreatePersonRequest req, CreatePerson useCase, CancellationToken ct)
    {
        try
        {
            var person = await useCase.ExecuteAsync(
                new CreatePersonInput(req.Name, req.DocumentNumber, req.Type, req.Email), ct);
            return Results.Created($"{ApiGroup}/people/{person.Id}", MapPersonResponse(person));
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleDeactivatePerson(
        Guid id, DeactivatePerson useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleActivatePerson(
        Guid id, ActivatePerson useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    // ── Hour Logs ─────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleLogHours(
        Guid id, LogHoursRequest req, LogHours useCase, CancellationToken ct)
    {
        try
        {
            var log = await useCase.ExecuteAsync(new LogHoursInput(id, req.Date, req.HoursWorked, req.Description), ct);
            return Results.Created($"{ApiGroup}/people/{id}/hour-logs/{log.Id}", MapHourLogResponse(log));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
        catch (ArgumentOutOfRangeException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleListHourLogs(
        Guid id, DateOnly? from, DateOnly? to, ListHourLogs useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, from, to, ct);
        return Results.Ok(result.Select(MapHourLogResponse));
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static object MapPersonResponse(Person p) => new
    {
        p.Id, p.Name, p.DocumentNumber, p.Email,
        Type   = p.Type.ToDisplayType(),
        Status = p.Status.ToDisplayStatus(),
        p.CreatedAt, p.UpdatedAt
    };

    private static object MapHourLogResponse(HourLog l) => new
    {
        l.Id, l.PersonId, l.Date, l.HoursWorked, l.Description, l.RecordedAt
    };
}
