using Microsoft.Extensions.Configuration;
using Render.TempFromVessel.Kernel;

namespace Render
{
    public class AppSettings : IAppSettings
    {
        public string Environment { get; }
        public string ApiEndpoint { get; }
        public string CouchbaseReplicationUri { get; }
        public string CouchbasePeerUsername { get; }
        public string CouchbasePeerPassword { get; }
        public int CouchbaseStartingPort { get; }
        public int CouchbaseMaxSyncAttempts { get; }
        public int? MaxAuthenticationAttempts { get; }
        public string WebSocketProtocol { get; }
        public string ReplicationPort { get; }
        public string AppCenterAppName { get; }
        public string AppCenterAPIToken { get; }
        public string AppCenterUwpKey { get; }
        public string AppCenterAndroidKey { get; }
        public string AppCenterIOSKey { get; }
        public string RenderXamarinAppDirName { get; }

        public AppSettings()
        {
            var configNoEnv = GetConfigurationRoot(false);
            Environment = configNoEnv.GetValue<string>("Environment");
            ApiEndpoint = configNoEnv.GetValue<string>("ApiEndpoint");

            var config = GetConfigurationRoot(true);
            AppCenterAppName = config.GetValue<string>("AppCenterAppName");
            AppCenterAPIToken = config.GetValue<string>("AppCenterAPIToken");
            WebSocketProtocol = config.GetValue<string>("WebSocketProtocol");
            ReplicationPort = config.GetValue<string>("ReplicationPort");
            MaxAuthenticationAttempts = config.GetValue<int?>("MaxAuthenticationAttempts");

            var couchbaseConfigurationSection = config.GetSection("CouchbaseConfiguration");
            CouchbaseReplicationUri = couchbaseConfigurationSection.GetValue<string>("ReplicationUri");
            CouchbasePeerUsername = couchbaseConfigurationSection.GetValue<string>("PeerUsername");
            CouchbasePeerPassword = couchbaseConfigurationSection.GetValue<string>("PeerPassword");
            CouchbaseStartingPort = couchbaseConfigurationSection.GetValue<int>("StartingPort");
            CouchbaseMaxSyncAttempts = couchbaseConfigurationSection.GetValue<int>("MaxSyncAttempts");
            RenderXamarinAppDirName = couchbaseConfigurationSection.GetValue<string>("RenderXamarinAppDirName");

            var appCenterKeysSection = config.GetSection("AppCenterKeys");
            AppCenterUwpKey = appCenterKeysSection.GetValue<string>("Uwp");
            AppCenterAndroidKey = appCenterKeysSection.GetValue<string>("Android");
            AppCenterIOSKey = appCenterKeysSection.GetValue<string>("iOS");
        }

        private IConfigurationRoot GetConfigurationRoot(bool supportingEnvironmentVariables)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("AppSettings.json")
                .AddJsonFile("AppSettings.development.json", optional: true); // configuration values override

            if (supportingEnvironmentVariables)
            {
                configurationBuilder.AddEnvironmentVariables();
            }

            return configurationBuilder.Build();
        }
    }
}