using System.Reflection;
using Refit;

namespace Trade.Gateway.Api.Client;

public class UtcDateTimeUrlParameterFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        if (value is DateTime dt)
        {
            return dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        return value?.ToString();
    }
}