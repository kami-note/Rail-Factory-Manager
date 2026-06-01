using RailFactory.BuildingBlocks.Auth;
using RailFactory.HumanResources.Api.Api.Requests;
using RailFactory.HumanResources.Api.Application.Hours;
using RailFactory.HumanResources.Api.Application.People;
using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Application.Skills;
using RailFactory.HumanResources.Api.Application.Shifts;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Api;

public static class HrEndpoints
{
    private const string ApiGroup = "/api/hr";

    public static WebApplication MapHrEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}/info"));

        var group = app.MapGroup(ApiGroup).WithTags("HumanResources");

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

        // Skills Matrix (RF-32)
        secure.MapGet("/people/{id:guid}/skills", HandleListSkills)
            .RequirePermission(SystemPermissions.Hr.Read);

        secure.MapPost("/people/{id:guid}/skills", HandleAddSkill)
            .RequirePermission(SystemPermissions.Hr.Write);

        secure.MapDelete("/people/{id:guid}/skills/{skillId:guid}", HandleRemoveSkill)
            .RequirePermission(SystemPermissions.Hr.Write);

        // Work Shifts (RD-HR-03)
        secure.MapGet("/people/{id:guid}/shifts", HandleListShifts)
            .RequirePermission(SystemPermissions.Hr.Read);

        secure.MapPost("/people/{id:guid}/shifts", HandleCreateShift)
            .RequirePermission(SystemPermissions.Hr.Write);

        secure.MapDelete("/people/{id:guid}/shifts/{shiftId:guid}", HandleDeleteShift)
            .RequirePermission(SystemPermissions.Hr.Write);

        // CSV Exports (RF-37)
        secure.MapGet("/people/export", HandleExportPeopleCsv)
            .RequirePermission(SystemPermissions.Hr.Read);

        // Payroll Export (RD-HR-02)
        secure.MapGet("/payroll/export", HandleExportPayrollCsv)
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

    // ── Skills ────────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListSkills(
        Guid id, ListPersonSkills useCase, CancellationToken ct)
    {
        var skills = await useCase.ExecuteAsync(id, ct);
        return Results.Ok(skills.Select(MapSkillResponse));
    }

    private static async Task<IResult> HandleAddSkill(
        Guid id, AddPersonSkillRequest req, AddPersonSkill useCase, CancellationToken ct)
    {
        try
        {
            var skill = await useCase.ExecuteAsync(
                new AddPersonSkillInput(id, req.SkillName, req.ProficiencyLevel, req.CertifiedAt, req.Notes), ct);
            return Results.Created($"{ApiGroup}/people/{id}/skills/{skill.Id}", MapSkillResponse(skill));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleRemoveSkill(
        Guid id, Guid skillId, RemovePersonSkill useCase, CancellationToken ct)
    {
        var removed = await useCase.ExecuteAsync(skillId, ct);
        return removed ? Results.NoContent() : Results.NotFound();
    }

    // ── Shifts ────────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListShifts(
        Guid id, DateOnly? from, DateOnly? to, ListWorkShifts useCase, CancellationToken ct)
    {
        var shifts = await useCase.ExecuteAsync(id, from, to, ct);
        return Results.Ok(shifts.Select(MapShiftResponse));
    }

    private static async Task<IResult> HandleCreateShift(
        Guid id, CreateWorkShiftRequest req, CreateWorkShift useCase, CancellationToken ct)
    {
        try
        {
            var shift = await useCase.ExecuteAsync(
                new CreateWorkShiftInput(id, req.ShiftDate, req.StartTime, req.EndTime, req.Notes), ct);
            return Results.Created($"{ApiGroup}/people/{id}/shifts/{shift.Id}", MapShiftResponse(shift));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleDeleteShift(
        Guid id, Guid shiftId, DeleteWorkShift useCase, CancellationToken ct)
    {
        var removed = await useCase.ExecuteAsync(shiftId, ct);
        return removed ? Results.NoContent() : Results.NotFound();
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

    // ── CSV Exports (RF-37 / RD-HR-02) ───────────────────────────────────────

    private static async Task<IResult> HandleExportPeopleCsv(
        ListPersons useCase, CancellationToken ct)
    {
        var people = await useCase.ExecuteAsync(null, null, ct);
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,Name,DocumentNumber,Email,Type,Status,CreatedAt");
        foreach (var p in people)
            csv.AppendLine($"{p.Id},{Csv(p.Name)},{p.DocumentNumber},{Csv(p.Email ?? "")},{p.Type},{p.Status},{p.CreatedAt:O}");
        return Results.Text(csv.ToString(), "text/csv", System.Text.Encoding.UTF8);
    }

    private static async Task<IResult> HandleExportPayrollCsv(
        IPersonRepository personRepo, IHourLogRepository hourLogRepo,
        int? year, int? month, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;

        var logs = await hourLogRepo.ListAllForPeriodAsync(y, m, ct);
        var personIds = logs.Select(l => l.PersonId).Distinct().ToList();
        var people = (await personRepo.ListAsync(null, null, ct))
            .Where(p => personIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("PersonId,Name,DocumentNumber,Date,HoursWorked,Description");
        foreach (var log in logs)
        {
            people.TryGetValue(log.PersonId, out var person);
            csv.AppendLine($"{log.PersonId},{Csv(person?.Name ?? "")},{Csv(person?.DocumentNumber ?? "")},{log.Date},{log.HoursWorked},{Csv(log.Description ?? "")}");
        }
        return Results.Text(csv.ToString(), "text/csv", System.Text.Encoding.UTF8);
    }

    private static string Csv(string value)
        => value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static object MapSkillResponse(PersonSkill s) => new
    {
        s.Id, s.PersonId, s.SkillName, s.ProficiencyLevel, s.CertifiedAt, s.Notes, s.CreatedAt
    };

    private static object MapShiftResponse(WorkShift s) => new
    {
        s.Id, s.PersonId, s.ShiftDate, s.StartTime, s.EndTime, s.Notes, s.CreatedAt
    };
}
