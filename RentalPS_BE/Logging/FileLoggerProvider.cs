using System.Text;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Logging;

/// <summary>
/// ILogger provider sederhana yang menulis log ke satu file per hari.
/// Tidak membutuhkan package/NuGet tambahan.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly string _filePrefix;
    private readonly object _sync = new();
    private StreamWriter? _writer;
    private string? _currentFilePath;
    private bool _disposed;

    public FileLoggerProvider(string logDirectory, string filePrefix = "rentalps")
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentException("Log directory cannot be empty.", nameof(logDirectory));

        if (string.IsNullOrWhiteSpace(filePrefix))
            throw new ArgumentException("File prefix cannot be empty.", nameof(filePrefix));

        _logDirectory = logDirectory;
        _filePrefix = filePrefix;

        // Folder dan file hari ini dibuat otomatis ketika aplikasi mulai.
        Directory.CreateDirectory(_logDirectory);
        lock (_sync)
        {
            EnsureWriter(DateTimeOffset.Now);
            _writer!.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [Information] [RentalPS] File logging initialized.");
            _writer.Flush();
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileLoggerProvider));

        return new FileLogger(this, categoryName);
    }

    internal bool IsEnabled(LogLevel logLevel) =>
        !_disposed && logLevel != LogLevel.None && logLevel >= LogLevel.Information;

    internal void Write(
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        string message,
        Exception? exception)
    {
        if (!IsEnabled(logLevel))
            return;

        var now = DateTimeOffset.Now;
        var singleLineMessage = (message ?? string.Empty)
            .Replace("\r\n", " | ")
            .Replace('\r', ' ')
            .Replace('\n', ' ');

        lock (_sync)
        {
            if (_disposed)
                return;

            EnsureWriter(now);

            var eventText = eventId.Id == 0
                ? string.Empty
                : $" [EventId: {eventId.Id} - {eventId.Name}]";

            _writer!.WriteLine(
                $"{now:yyyy-MM-dd HH:mm:ss.fff zzz} [{GetLevelName(logLevel)}] [{categoryName}]{eventText} {singleLineMessage}");

            if (exception is not null)
            {
                _writer.WriteLine(exception);
            }

            _writer.Flush();
        }
    }

    private void EnsureWriter(DateTimeOffset now)
    {
        var fileName = $"{_filePrefix}-{now:yyyyMMdd}.log";
        var targetPath = Path.Combine(_logDirectory, fileName);

        if (string.Equals(_currentFilePath, targetPath, StringComparison.OrdinalIgnoreCase)
            && _writer is not null)
        {
            return;
        }

        _writer?.Dispose();
        Directory.CreateDirectory(_logDirectory);

        var stream = new FileStream(
            targetPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete);

        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
        _currentFilePath = targetPath;
    }

    private static string GetLevelName(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Information",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Critical",
        _ => "None"
    };

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _categoryName;

        public FileLogger(FileLoggerProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter is null)
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception is null)
                return;

            _provider.Write(_categoryName, logLevel, eventId, message, exception);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
