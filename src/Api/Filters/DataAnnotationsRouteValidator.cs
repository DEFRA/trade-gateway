using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Filters;

public static class DataAnnotationsRouteValidator
{
    public static IDictionary<string, string[]>? Validate<T>(object? boundModel, RouteValueDictionary routeValues) where T : class, new()
    {
        object instance = boundModel ?? new T();

        if (boundModel is null)
        {
            var type = typeof(T);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var fromRoute = prop.GetCustomAttribute<FromRouteAttribute>();
                var routeName = fromRoute?.Name ?? prop.Name;

                if (routeValues.TryGetValue(routeName, out var raw))
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
                        // ignore conversion errors; validation will catch invalid values
                    }
                }
            }
        }

        var validationResults = new List<ValidationResult>();
        var contextForValidation = new ValidationContext(instance, null, null);

        var isValid = Validator.TryValidateObject(instance, contextForValidation, validationResults, validateAllProperties: true);

        if (isValid)
            return null;

        var dict = validationResults
            .SelectMany(r => (r.MemberNames?.Any() ?? false ? r.MemberNames : new[] { string.Empty })
                .Select(m => new { Member = m ?? string.Empty, Msg = r.ErrorMessage ?? string.Empty }))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Msg).ToArray());

        return dict;
    }
}
