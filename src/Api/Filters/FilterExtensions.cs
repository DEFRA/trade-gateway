namespace Api.Filters;

public static class FilterExtensions
{
    public static RouteHandlerBuilder Validates<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        builder.AddEndpointFilter((context, next) => new ValidationFilter<T>().InvokeAsync(context, next));

        return builder;
    }

    public static RouteHandlerBuilder ValidatesRoute<T>(this RouteHandlerBuilder builder)
        where T : class, new()
    {
        builder.AddEndpointFilter((context, next) => new DataAnnotationsRouteValidationFilter<T>().InvokeAsync(context, next));

        return builder;
    }
}
