using Render.Models.Users;

namespace Render.Services.SyncService;

public class LastSynchronizationState
{
    public Guid ProjectId { get; set; }

    public IUser LoggedInUser { get; set; }

    public string SyncGatewayPassword { get; set; }

    public Action OnSyncStarting { get; set; }

    public bool NeedToResetLocalSync { get; set; }
    
    public bool LastInternetAccess { get; set; }
}