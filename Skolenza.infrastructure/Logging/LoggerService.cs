using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ILogger;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.Logging
{
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;

        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger; // Injecting the logger
        }

        public void LogInfo(string message) => _logger.LogInformation(message);
        public void LogWarn(string message) => _logger.LogWarning(message);
        public void LogError(string message) => _logger.LogError(message);
        public void LogDebug(string message) => _logger.LogDebug(message);
    }
}
