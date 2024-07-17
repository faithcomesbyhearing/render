namespace Render.Interfaces
{
    public interface IRenderLogger
    {
        void LogError(Exception exception, IDictionary<string, string> properties = null);

        void LogInfo(string message, IDictionary<string, string> properties = null);
        
        void LogDebug(string message, IDictionary<string, string> properties = null);
        
        void LogTrace(string message, IDictionary<string, string> properties = null);
    }
}