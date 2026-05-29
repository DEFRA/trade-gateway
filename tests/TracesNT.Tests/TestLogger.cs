using Microsoft.Extensions.Logging;

namespace TracesNT.Tests;

internal sealed class TestLogger(LogLevel minimumLevel = LogLevel.Trace) : ILogger
{
    internal List<LogEntry> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= minimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    internal sealed record LogEntry(LogLevel Level, string Message);

    private sealed class NullScope : IDisposable
    {
        internal static NullScope Instance { get; } = new();

        public void Dispose() { }
    }
}
