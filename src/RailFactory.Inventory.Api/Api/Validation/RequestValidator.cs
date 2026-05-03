using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RailFactory.Inventory.Api.Api.Validation;

public static class RequestValidator
{
    public static IResult? Validate(object dto)
    {
        var results = new List<ValidationResult>();
        ValidateRecursive(dto, results, "request");
        if (results.Count == 0)
        {
            return null;
        }

        var errors = results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, memberName) => new { memberName, result.ErrorMessage })
            .GroupBy(x => string.IsNullOrWhiteSpace(x.memberName) ? "request" : x.memberName)
            .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage ?? "Invalid value.").ToArray());

        return Results.ValidationProblem(errors);
    }

    private static void ValidateRecursive(object instance, List<ValidationResult> results, string path)
    {
        var context = new ValidationContext(instance);
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);

        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType == typeof(string))
            {
                continue;
            }

            var value = property.GetValue(instance);
            if (value is null)
            {
                continue;
            }

            if (value is System.Collections.IEnumerable enumerable && value.GetType() != typeof(byte[]))
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    if (item is null || item is string)
                    {
                        index++;
                        continue;
                    }

                    ValidateRecursive(item, results, $"{path}.{property.Name}[{index}]");
                    index++;
                }

                continue;
            }

            if (property.PropertyType.IsClass || (property.PropertyType.IsValueType && !property.PropertyType.IsPrimitive && !property.PropertyType.IsEnum))
            {
                ValidateRecursive(value, results, $"{path}.{property.Name}");
            }
        }
    }
}
