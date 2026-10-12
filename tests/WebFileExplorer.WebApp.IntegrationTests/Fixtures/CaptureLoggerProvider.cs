using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WebFileExplorer.WebApp.IntegrationTests.Fixtures;

internal sealed class CaptureLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(string Category, LogLevel Level, string Text)> Entries { get; } = new ();

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class CaptureLogger(CaptureLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            owner.Entries.Enqueue((
                category,
                logLevel,
                formatter(state, exception)));
        }
    }
}
