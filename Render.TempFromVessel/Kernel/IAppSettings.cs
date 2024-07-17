namespace Render.TempFromVessel.Kernel
{
    public interface IAppSettings
    {
        string Environment { get; }
        string ApiEndpoint { get; }

        string CouchbaseReplicationUri { get; }
        string CouchbasePeerUsername { get; }
        string CouchbasePeerPassword { get; }
        int CouchbaseStartingPort { get; }
        int CouchbaseMaxSyncAttempts { get; }

        int? MaxAuthenticationAttempts { get; }
        string WebSocketProtocol { get; }
        string ReplicationPort { get; }
        string AppCenterAppName { get; }
        string AppCenterAPIToken { get; }
        string AppCenterUwpKey { get; }
        string AppCenterAndroidKey { get; }
        string AppCenterIOSKey { get; }
    }
}