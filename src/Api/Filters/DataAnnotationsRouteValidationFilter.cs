using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Filters;

public sealed class DataAnnotationsRouteValidationFilter<T> : IEndpointFilter where T : class, new()
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var instance = new T();
        var type = typeof(T);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // determine route name from [FromRoute(Name = "...")] or property name
            var fromRoute = prop.GetCustomAttribute<FromRouteAttribute>();
            var routeName = fromRoute?.Name ?? prop.Name;

            if (context.HttpContext.Request.RouteValues.TryGetValue(routeName, out var raw))
            {
                try
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    object? converted = null;

                    if (raw is string s)
                    {
                        if (targetType == typeof(string))
                            converted = s;
                        else
                            converted = Convert.ChangeType(s, targetType);
                    }
                    else
                    {
                        converted = Convert.ChangeType(raw, targetType);
                    }

                    prop.SetValue(instance, converted);
                }
                catch
                {
                    // ignore conversion errors here; validation will catch missing/invalid values
                }
            }
        }

        var validationResults = new List<ValidationResult>();
        var contextForValidation = new ValidationContext(instance, null, null);

        var isValid = Validator.TryValidateObject(instance, contextForValidation, validationResults, validateAllProperties: true);

        if (!isValid)
        {
            var dict = validationResults
                .SelectMany(r => (r.MemberNames?.Any() ?? false ? r.MemberNames : new[] { string.Empty })
                    .Select(m => new { Member = m ?? string.Empty, Msg = r.ErrorMessage ?? string.Empty }))
                .GroupBy(x => x.Member)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Msg).ToArray());

            return new ValueTask<object?>(Results.ValidationProblem(dict));
        }

        return next(context);
    }
}
