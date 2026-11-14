using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MemberIngestion.TestSupport;

public sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

/// <summary>Captures every log message, formatted, with its exception. Use it to check what reaches logs.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> entries = new();

    public IReadOnlyList<LogEntry> Entries => [.. entries];

    /// <summary>All messages and exceptions as one string.</summary>
    public string Text => string.Join(Environment.NewLine, entries.Select(e => $"{e.Message} {e.Exception}"));

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception), exception));
    }
}
