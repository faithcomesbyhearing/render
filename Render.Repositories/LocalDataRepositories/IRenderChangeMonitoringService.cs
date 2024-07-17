namespace Render.Repositories.LocalDataRepositories
{
    public interface IRenderChangeMonitoringService : IDisposable
    {
        Task MonitorDocumentByIdAsync<T>(Guid id, Action<object, Guid> action);

        void RemoveDocumentById<T>(Guid id);

        void CancelService();

        void MonitorDocumentByField<T>(string fieldName, string fieldValue, Action action);

        void StopMonitoringDocumentByField<T>(string fieldName, string fieldValue);
    } 
}