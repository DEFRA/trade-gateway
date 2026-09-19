namespace Api.Tests
{
    public record TokenSubstitution
    {
        public required string Token { get; init; }

        public required string Substitution { get; init; }
    }
}
