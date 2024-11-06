namespace Render.Repositories.Kernel
{
    public interface IDocumentChangeListener : IDisposable
    {
        void MonitorDocumentByField<T>(string fieldName, string fieldValue, Func<Guid, Task> onDatabaseChanged);
        void StopMonitoringDocumentByField<T>(string fieldName, string fieldValue);
    } 
}