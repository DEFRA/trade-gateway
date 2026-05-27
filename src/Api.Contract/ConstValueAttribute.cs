namespace Trade.Gateway.Api.Contract;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConstValueAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}
