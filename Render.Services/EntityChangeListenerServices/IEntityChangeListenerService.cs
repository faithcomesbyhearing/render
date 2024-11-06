namespace Render.Services.EntityChangeListenerServices;

public interface IEntityChangeListenerService : IDisposable
{
    void InitializeListener(Func<Guid, Task> actionOnDocumentChanged);
    void ResetListeners();
    void RemoveListeners();
}