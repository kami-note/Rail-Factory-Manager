using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RailFactory.SupplyChain.Api.Application.Receiving;

namespace RailFactory.SupplyChain.Api.Api.ExceptionHandling;

public sealed class GlobalDomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, title, extensions) = exception switch
        {
            AssociationValidationException ex => (StatusCodes.Status400BadRequest, ex.Code, "Operation failed", null as Dictionary<string, object?>),
            AssociationConflictException ex => (StatusCodes.Status412PreconditionFailed, "association.conflict", "Operation failed", null),
            AssociationReleaseBlockedException ex => (StatusCodes.Status409Conflict, "association.release_blocked", "Release blocked", new Dictionary<string, object?> { ["blockers"] = ex.Blockers }),
            RemoteServiceValidationException ex => (StatusCodes.Status400BadRequest, ex.Code, "Operation failed", null),
            RemoteServiceConflictException ex => (StatusCodes.Status409Conflict, ex.Code, "SKU já existe no inventário", null),
            ReceiptAlreadyExistsException ex => (StatusCodes.Status409Conflict, "receipt.already_exists", "Operation failed", null),
            ImportXmlReceiptBatchValidationException ex => (StatusCodes.Status400BadRequest, "batch.validation_failed", "Operation failed", new Dictionary<string, object?> { ["errors"] = ex.Errors }),
            InvalidOperationException ex => (StatusCodes.Status409Conflict, "invalid_operation", "Operation failed", null),
            _ => (0, null, null, null)
        };

        if (statusCode == 0)
        {
            return false; // Not handled by this handler
        }

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = exception.Message,
            Status = statusCode,
            Instance = httpContext.Request.Path
        };

        if (code is not null)
        {
            problemDetails.Extensions["code"] = code;
        }

        if (extensions is not null)
        {
            foreach (var ext in extensions)
            {
                problemDetails.Extensions[ext.Key] = ext.Value;
            }
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
