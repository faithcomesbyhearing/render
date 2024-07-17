using Newtonsoft.Json;

namespace Render.WebAuthentication
{
    public class AppCenterApiWrapper : IAppCenterApiWrapper
    {
        private const string DefaultVersionNumber = "1.0.0.0";
        private const string DefaultDistributionGroupName = "Render%20Global%20Distribution";
		private const string BetaTestingDistributionGroupName = "Beta-Test%20Group";

		private readonly HttpClient _httpClient;
		private string _downloadUrl = string.Empty;

		public string AppName { get; }

        public string DownloadUrl => _downloadUrl;

        public AppCenterApiWrapper(HttpClient httpClient, string appName, string token)
        {
            AppName = appName;

            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("X-API-Token", token);
        }

		public async Task<string> GetLatestVersionAsync(bool availableBetaTesting)
		{
			if (availableBetaTesting)
			{
				var (betaResult, betaVersion) = await TryGetLatestVersionAsync(true);
				if (betaResult)
				{
					return betaVersion;
				}
			}

			var (_, currentVersion) = await TryGetLatestVersionAsync(false);

            return currentVersion;
		}

		private async Task<(bool success, string version)> TryGetLatestVersionAsync(bool availableBetaTesting)
        {
			var distributionGroup = availableBetaTesting
				? BetaTestingDistributionGroupName
				: DefaultDistributionGroupName;
			var requestUrl = $"https://api.appcenter.ms/v0.1/apps/FCBH/{AppName}/distribution_groups/{distributionGroup}/releases";
            var result = await _httpClient.GetAsync(requestUrl);
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                var jsonSerializer = new JsonSerializer();
                var data = jsonSerializer.Deserialize<List<AppCenterRelease>>(new JsonTextReader(new StringReader(json)));
                var release = data.FirstOrDefault();
                if (release != null)
                {
					_downloadUrl =
	                    $"https://install.appcenter.ms/orgs/fcbh/apps/{AppName}/distribution_groups/{distributionGroup}";

					return (true, release.version);
                }
            }

            return (false, DefaultVersionNumber);
		}

        public class AppCenterRelease
        {
            public int id { get; set; }
            public string version { get; set; }
            public string origin { get; set; }
            public string short_version { get; set; }
            public bool enabled { get; set; }
            public string uploaded_at { get; set; }
            public bool is_external_build { get; set; }
        }
    }
}