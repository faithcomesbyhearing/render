using Render.Models.Users;
using Render.Repositories.Kernel;

namespace Render.Services.EntityChangeListenerServices;

public class UserChangeListenerService(IDocumentChangeListener documentChangeListener, List<Guid> userIds)
    : IEntityChangeListenerService
{
    private Func<Guid, Task> _actionOnDocumentChanged;
    private readonly List<Guid> _userIds = userIds ?? [];

    public void Dispose()
    {
        RemoveListeners();
        documentChangeListener?.Dispose();
    }

    public void InitializeListener(Func<Guid, Task> actionOnDocumentChanged)
    {
        _actionOnDocumentChanged = actionOnDocumentChanged;
        foreach (var userId in _userIds)
        {
            documentChangeListener.MonitorDocumentByField<User>(nameof(User.Id), userId.ToString(),
                _actionOnDocumentChanged);
        }
    }

    public void ResetListeners()
    {
        foreach (var userId in _userIds)
        {
            documentChangeListener?.StopMonitoringDocumentByField<User>(nameof(User.Id), userId.ToString());
            documentChangeListener?.MonitorDocumentByField<User>(nameof(User.Id), userId.ToString(),
                _actionOnDocumentChanged);
        }
    }

    public void RemoveListeners()
    {
        foreach (var userId in _userIds)
        {
            documentChangeListener?.StopMonitoringDocumentByField<User>(nameof(User.Id), userId.ToString());
        }
    }
}