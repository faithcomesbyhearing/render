using NLog;
using Render.Interfaces;

namespace Render.Utilities
{
    public class RenderLogger : RenderLoggerBase
    {
        private static readonly string _loggerName = typeof(RenderLogger).ToString();

        public static void LogInfo(string message, IDictionary<string, string> properties = null)
            => Log(GetLogger(_loggerName), LogLevel.Info, message, properties: properties);

        public static void LogError(Exception exception, IDictionary<string, string> properties = null)
            => Log(GetLogger(_loggerName), LogLevel.Error, null, exception, properties);
    }

    internal class TypedRenderLogger : RenderLoggerBase, IRenderLogger
    {
        private Logger _logger;

        public TypedRenderLogger(Type loggerType)
        {
            _logger = GetLogger(loggerType.ToString());
        }
        
        void IRenderLogger.LogError(Exception exception, IDictionary<string, string> properties = null)
            => Log(_logger, LogLevel.Error, null, exception, properties);
        
        void IRenderLogger.LogInfo(string message, IDictionary<string, string> properties = null)
            => Log(_logger, LogLevel.Info, message, properties: properties);
        
        void IRenderLogger.LogDebug(string message, IDictionary<string, string> properties = null)
            => Log(_logger, LogLevel.Debug, message, properties: properties);
        
        void IRenderLogger.LogTrace(string message, IDictionary<string, string> properties = null)
            => Log(_logger, LogLevel.Trace, message, properties: properties);
    }
}
