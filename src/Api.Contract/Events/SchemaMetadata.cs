namespace Trade.Gateway.Api.Contract.Events;

public struct SchemaMetadata
{
    public required string Version { get; set; }
    public required Uri Uri { get; set; }
}
