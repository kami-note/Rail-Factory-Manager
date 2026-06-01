using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Fleet.Api.Api.Requests;
using RailFactory.Fleet.Api.Application.Drivers;
using RailFactory.Fleet.Api.Application.Fueling;
using RailFactory.Fleet.Api.Application.Maintenance;
using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Application.Routing;
using RailFactory.Fleet.Api.Application.Telemetry;
using RailFactory.Fleet.Api.Application.Vehicles;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Api;

public static class FleetEndpoints
{
    private const string ApiGroup = "/api/fleet";

    public static WebApplication MapFleetEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}/info"));

        var group = app.MapGroup(ApiGroup).WithTags("Fleet");

        group.MapGet("/info", (IHostEnvironment env) =>
            Results.Ok(new { Service = "fleet", Environment = env.EnvironmentName })
        ).AllowAnonymous();

        var secure = group.MapGroup("/").RequireAuthorization();

        // Vehicles
        secure.MapGet("/vehicles", HandleListVehicles)
            .RequirePermission(SystemPermissions.Fleet.Read);

        secure.MapGet("/vehicles/{id:guid}", HandleGetVehicle)
            .RequirePermission(SystemPermissions.Fleet.Read);

        secure.MapPost("/vehicles", HandleCreateVehicle)
            .RequirePermission(SystemPermissions.Fleet.Write);

        secure.MapPut("/vehicles/{id:guid}/deactivate", HandleDeactivateVehicle)
            .RequirePermission(SystemPermissions.Fleet.Write);

        secure.MapPut("/vehicles/{id:guid}/activate", HandleActivateVehicle)
            .RequirePermission(SystemPermissions.Fleet.Write);

        // Driver Assignments
        secure.MapPost("/vehicles/{id:guid}/driver-assignments", HandleAssignDriver)
            .RequirePermission(SystemPermissions.Fleet.Write);

        secure.MapGet("/vehicles/{id:guid}/driver-assignments", HandleListDriverAssignments)
            .RequirePermission(SystemPermissions.Fleet.Read);

        // Maintenance Plans
        secure.MapPost("/vehicles/{id:guid}/maintenance-plans", HandleScheduleMaintenance)
            .RequirePermission(SystemPermissions.Fleet.Write);

        secure.MapGet("/vehicles/{id:guid}/maintenance-plans", HandleListMaintenancePlans)
            .RequirePermission(SystemPermissions.Fleet.Read);

        secure.MapPut("/vehicles/{id:guid}/maintenance-plans/{planId:guid}/complete", HandleCompleteMaintenance)
            .RequirePermission(SystemPermissions.Fleet.Write);

        secure.MapPut("/vehicles/{id:guid}/maintenance-plans/{planId:guid}/cancel", HandleCancelMaintenance)
            .RequirePermission(SystemPermissions.Fleet.Write);

        // Fueling Records
        secure.MapPost("/vehicles/{id:guid}/fueling-records", HandleRecordFueling)
            .RequirePermission(SystemPermissions.Fleet.Write);

        secure.MapGet("/vehicles/{id:guid}/fueling-records", HandleListFuelingRecords)
            .RequirePermission(SystemPermissions.Fleet.Read);

        // Telemetry Events (RF-30)
        secure.MapPost("/vehicles/{id:guid}/telemetry-events", HandleRecordTelemetry)
            .RequirePermission(SystemPermissions.Fleet.Write);

        secure.MapGet("/vehicles/{id:guid}/telemetry-events", HandleListTelemetry)
            .RequirePermission(SystemPermissions.Fleet.Read);

        // Route Optimization (RF-29)
        secure.MapPost("/route-optimization", HandleRouteOptimization)
            .RequirePermission(SystemPermissions.Fleet.Read);

        return app;
    }

    // ── Vehicles ──────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListVehicles(
        string? status, ListVehicles useCase, CancellationToken ct)
    {
        VehicleStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<VehicleStatus>(status, true, out var s))
            parsedStatus = s;

        var result = await useCase.ExecuteAsync(parsedStatus, ct);
        return Results.Ok(result.Select(MapVehicleResponse));
    }

    private static async Task<IResult> HandleGetVehicle(
        Guid id, IVehicleRepository repo, CancellationToken ct)
    {
        var vehicle = await repo.GetByIdAsync(id, ct);
        return vehicle is null ? Results.NotFound() : Results.Ok(MapVehicleResponse(vehicle));
    }

    private static async Task<IResult> HandleCreateVehicle(
        CreateVehicleRequest req, CreateVehicle useCase, CancellationToken ct)
    {
        try
        {
            var vehicle = await useCase.ExecuteAsync(
                new CreateVehicleInput(req.Plate, req.Chassis, req.Renavam, req.Type,
                    req.MaxWeightKg, req.MaxVolumeCbm, req.LicenseExpiry), ct);
            return Results.Created($"{ApiGroup}/vehicles/{vehicle.Id}", MapVehicleResponse(vehicle));
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleDeactivateVehicle(
        Guid id, DeactivateVehicle useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleActivateVehicle(
        Guid id, ActivateVehicle useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    // ── Driver Assignments ────────────────────────────────────────────────────

    private static async Task<IResult> HandleAssignDriver(
        Guid id, AssignDriverRequest req, AssignDriver useCase, CancellationToken ct)
    {
        try
        {
            var assignment = await useCase.ExecuteAsync(
                new AssignDriverInput(id, req.DriverPersonId, req.StartDate, req.EndDate, req.Notes), ct);
            return Results.Created($"{ApiGroup}/vehicles/{id}/driver-assignments/{assignment.Id}",
                MapAssignmentResponse(assignment));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleListDriverAssignments(
        Guid id, ListDriverAssignments useCase, CancellationToken ct)
    {
        try
        {
            var result = await useCase.ExecuteAsync(id, ct);
            return Results.Ok(result.Select(MapAssignmentResponse));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static object MapVehicleResponse(Vehicle v) => new
    {
        v.Id, v.Plate, v.Chassis, v.Renavam,
        v.MaxWeightKg, v.MaxVolumeCbm,
        v.LicenseExpiry,
        Type   = v.Type.ToDisplayType(),
        Status = v.Status.ToDisplayStatus(),
        v.CreatedAt, v.UpdatedAt
    };

    private static object MapAssignmentResponse(DriverAssignment a) => new
    {
        a.Id, a.VehicleId, a.DriverPersonId,
        a.StartDate, a.EndDate, a.Notes, a.AssignedAt
    };

    // ── Maintenance Plans ─────────────────────────────────────────────────────

    private static async Task<IResult> HandleScheduleMaintenance(
        Guid id, ScheduleMaintenanceRequest req, ScheduleMaintenance useCase, CancellationToken ct)
    {
        if (!Enum.TryParse<MaintenanceType>(req.Type, true, out var maintenanceType))
            return Results.BadRequest(new { Error = $"Invalid maintenance type '{req.Type}'. Valid: {string.Join(", ", Enum.GetNames<MaintenanceType>())}" });
        try
        {
            var plan = await useCase.ExecuteAsync(
                new ScheduleMaintenanceInput(id, maintenanceType, req.Description, req.ScheduledDate, req.Notes), ct);
            return Results.Created($"{ApiGroup}/vehicles/{id}/maintenance-plans/{plan.Id}", MapMaintenanceResponse(plan));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleListMaintenancePlans(
        Guid id, ListMaintenancePlans useCase, CancellationToken ct)
    {
        var plans = await useCase.ExecuteAsync(id, ct);
        return Results.Ok(plans.Select(MapMaintenanceResponse));
    }

    private static async Task<IResult> HandleCompleteMaintenance(
        Guid id, Guid planId, CompleteMaintenanceRequest req, CompleteMaintenance useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, planId, req.CompletedDate, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleCancelMaintenance(
        Guid id, Guid planId, CancelMaintenance useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, planId, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    // ── Fueling Records ───────────────────────────────────────────────────────

    private static async Task<IResult> HandleRecordFueling(
        Guid id, RecordFuelingRequest req, RecordFueling useCase, CancellationToken ct)
    {
        try
        {
            var record = await useCase.ExecuteAsync(
                new RecordFuelingInput(id, req.Date, req.LitersSupplied, req.PricePerLiter,
                    req.Odometer, req.Supplier, req.Notes), ct);
            return Results.Created($"{ApiGroup}/vehicles/{id}/fueling-records/{record.Id}", MapFuelingResponse(record));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleListFuelingRecords(
        Guid id, ListFuelingRecords useCase, CancellationToken ct)
    {
        var records = await useCase.ExecuteAsync(id, ct);
        return Results.Ok(records.Select(MapFuelingResponse));
    }

    // ── Telemetry ─────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleRecordTelemetry(
        Guid id, RecordTelemetryEventRequest req, RecordTelemetryEvent useCase, CancellationToken ct)
    {
        try
        {
            var ev = await useCase.ExecuteAsync(new RecordTelemetryInput(
                id, req.DriverPersonId, req.EventType, req.Description,
                req.OccurredAt, req.LatitudeDeg, req.LongitudeDeg), ct);
            return Results.Created($"{ApiGroup}/vehicles/{id}/telemetry-events/{ev.Id}", MapTelemetryResponse(ev));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleListTelemetry(
        Guid id, DateTimeOffset? from, DateTimeOffset? to, ListTelemetryEvents useCase, CancellationToken ct)
    {
        var events = await useCase.ExecuteAsync(id, from, to, ct);
        return Results.Ok(events.Select(MapTelemetryResponse));
    }

    private static object MapTelemetryResponse(VehicleTelemetryEvent e) => new
    {
        e.Id, e.VehicleId, e.DriverPersonId, e.EventType,
        e.Description, e.OccurredAt, e.LatitudeDeg, e.LongitudeDeg, e.CreatedAt
    };

    private static object MapMaintenanceResponse(VehicleMaintenancePlan p) => new
    {
        p.Id, p.VehicleId,
        Type = p.Type.ToString(),
        p.Description, p.ScheduledDate, p.CompletedDate,
        Status = p.Status.ToString(),
        p.Notes, p.CreatedAt
    };

    // ── Route Optimization (RF-29) ────────────────────────────────────────────

    private static IResult HandleRouteOptimization([FromBody] RouteOptimizationRequest req)
    {
        if (req.Stops is null || req.Stops.Count == 0)
            return Results.BadRequest(new { Error = "At least one stop is required." });

        var stops = req.Stops.Select(s => new RouteStop(s.Id, s.Label, s.Lat, s.Lon)).ToList();
        var result = RouteOptimizer.Optimize(stops);
        return Results.Ok(result);
    }

    private static object MapFuelingResponse(FuelingRecord r) => new
    {
        r.Id, r.VehicleId, r.Date,
        r.LitersSupplied, r.PricePerLiter,
        TotalBrl = r.LitersSupplied * r.PricePerLiter,
        r.Odometer, r.Supplier, r.Notes, r.RecordedAt
    };
}
