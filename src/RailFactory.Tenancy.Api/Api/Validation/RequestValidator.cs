using System.ComponentModel.DataAnnotations;

namespace RailFactory.Tenancy.Api.Api.Validation;

public static class RequestValidator
{
    public static IResult? Validate(object dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(dto, context, results, validateAllProperties: true))
        {
            return null;
        }

        var errors = results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty),
                (result, memberName) => new { memberName, result.ErrorMessage })
            .GroupBy(x => string.IsNullOrWhiteSpace(x.memberName) ? "request" : x.memberName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.ErrorMessage ?? "Invalid value.").ToArray());

        return Results.ValidationProblem(errors);
    }
}
