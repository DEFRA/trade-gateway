using Api.Mapping;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;

namespace Api.Tests.Mapping;

public class DraftNotificationTests
{
    [Fact]
    public void LogIgnored_WritesAValidMessage()
    {
        var messages = new List<string>();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new Capture(messages)));

        DraftNotification.LogIgnored(loggerFactory, "CHED", "CHEDA.GB.2024.1020304");

        messages
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be("CHED CHEDA.GB.2024.1020304 has not been processed due to current status of 47");
    }

    private sealed class Capture(List<string> messages) : ILogger, ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => this;

        public void Dispose() { }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => messages.Add(formatter(state, exception));
    }
}
