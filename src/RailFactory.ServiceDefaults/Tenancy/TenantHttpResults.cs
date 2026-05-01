using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

public static class TenantHttpResults
{
    public static IResult CodeRequired()
    {
        return Results.Problem(
            title: "Invalid request",
            detail: "Tenant code is required.",
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = TenantConstants.CodeRequiredErrorCode
            });
    }
}
