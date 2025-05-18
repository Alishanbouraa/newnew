// OfflinePOS.DataAccess/Logging/LoggerAdapter.cs
using Microsoft.Extensions.Logging;
using System;

namespace OfflinePOS.DataAccess.Logging
{
    /// <summary>
    /// Adapter that allows any ILogger to be used where a specific ILogger<T> is required
    /// </summary>
    /// <typeparam name="T">Type to associate with the logger</typeparam>
    public class LoggerAdapter<T> : ILogger<T>
    {
        private readonly ILogger _innerLogger;

        public LoggerAdapter(ILogger innerLogger)
        {
            _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _innerLogger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _innerLogger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}