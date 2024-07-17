using NLog;

namespace Render.Utilities
{
    public class RenderLoggerBase
    {
        protected static Logger GetLogger(string name)
        {
            return LogManager.GetLogger(name);
        }

        protected static void Log(Logger logger, LogLevel level, string message, Exception exception = null, IDictionary<string, string> properties = null)
        {
            var e = LogEventInfo.Create(level, logger.Name, exception, null, message);
            AddProperties(e, properties);

            logger.Log(e);
        }

        private static void AddProperties(LogEventInfo e, IDictionary<string, string> properties)
        {
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    e.Properties[item.Key] = item.Value;
                }
            }
        }
    }
}
