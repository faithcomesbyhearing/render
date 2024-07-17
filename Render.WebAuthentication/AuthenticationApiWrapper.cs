using System.Text;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace Render.WebAuthentication
{
    public class AuthenticationApiWrapper : IAuthenticationApiWrapper
    {
        private const string SyncPath = "SyncAuthentication/";
        private const string AuthPath = "UserAuthentication/";
        private const string ProjectDownloadPath = "ProjectDownloadAuthentication/";
		private readonly int _maxAuthenticationAttempts;

		private readonly HttpClient _httpClient;

        public AuthenticationApiWrapper(
            HttpClient httpClient, 
            string connectionString, 
            int? maxAuthenticationAttempts)
        {
            _maxAuthenticationAttempts = maxAuthenticationAttempts ?? 1;
            _httpClient = httpClient;

            if (string.IsNullOrEmpty(connectionString) is false)
            {
                _httpClient.BaseAddress = new Uri($"{connectionString}RenderAuthentication/");
            }

            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

		public async Task<SyncGatewayUser> AuthenticateRenderUserForSyncAsync(string username, string password, Guid projectId)
        {
            var renderUser = new RenderUserToAuthenticate(username, password, projectId);
            var result = await _httpClient.PostAsync(
                requestUri: SyncPath,
                content: new StringContent(
                    content: JsonConvert.SerializeObject(renderUser),
                    encoding: Encoding.UTF8,
                    mediaType: "application/json"));

            if (result == null || !result.IsSuccessStatusCode)
            {
                return new SyncGatewayUser(Guid.Empty, "");
            }

            var json = await result.Content.ReadAsStringAsync();
            var jsonSerializer = new JsonSerializer();
            var userResult = jsonSerializer.Deserialize<SyncGatewayUser>(new JsonTextReader(new StringReader(json)));
            return userResult;
        }

        public async Task<AuthenticationResult> AuthenticateUserAsync(string username, string password)
        {
			var attempts = 0;
			AuthenticationResult result = null;

			// Authenticate multiple times in case of unstable internet connection to prevent 'OfflineError' result.
			while (result is null || (result.OfflineError && attempts < _maxAuthenticationAttempts))
			{
				result = await AuthenticateUserOneAttemptAsync(username, password);
				attempts++;
			}

			return result;
		}

		private async Task<AuthenticationResult> AuthenticateUserOneAttemptAsync(string username, string password)
		{
			var negativeAuthResult = new AuthenticationResult(false, "");
            try
            {
                var user = new UserToAuthenticate(username, password);
                var result = await _httpClient.PostAsync(
                    requestUri: AuthPath,
                    content: new StringContent(
                        content: JsonConvert.SerializeObject(user),
                        encoding: Encoding.UTF8,
                        mediaType: "application/json"));

                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    var jsonSerializer = new JsonSerializer();
                    var jsonReader = new JsonTextReader(new StringReader(json));
                    var authResult = jsonSerializer.Deserialize<AuthenticationResult>(jsonReader);

                    return authResult;
                }
            }
            catch (Exception)
            {
                negativeAuthResult.OfflineError = true;
            }

            return negativeAuthResult;
        }

        public async Task<RenderProjectDownloadObject> AuthenticateForProjectDownloadViaIdAsync(string projectId)
        {
            var result = await _httpClient.PostAsync(
                requestUri: ProjectDownloadPath + $"?projectId={projectId}", 
                content: null);

            if (result == null || !result.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await result.Content.ReadAsStringAsync();
            var jsonSerializer = new JsonSerializer();
            var jsonReader = new JsonTextReader(new StringReader(json));
            var projectResult = jsonSerializer.Deserialize<RenderProjectDownloadObject>(jsonReader);

            return projectResult;
        }

        public class AuthenticationResult
        {
            public bool SignInResult { get; }
            public bool OfflineError { get; set; }
            public string SyncGatewayPassword { get; }

            public AuthenticationResult(bool signInResult, string syncGatewayPassword)
            {
                SignInResult = signInResult;
                SyncGatewayPassword = syncGatewayPassword;
            }
        }

        private class UserToAuthenticate
        {
            public string username { get; set; }
            public string password { get; set; }

            public UserToAuthenticate(string _username, string _password)
            {
                username = _username;
                password = _password;
            }
        }

        public class RenderUserToAuthenticate
        {
            public string username { get; set; }
            public string password { get; set; }
            public Guid projectId { get; set; }

            public RenderUserToAuthenticate(string _username, string _password, Guid _projectId)
            {
                username = _username;
                password = _password;
                projectId = _projectId;
            }
        }

        public class SyncGatewayUser
        {
            public Guid UserId;
            public string SyncGatewayPassword;

            public SyncGatewayUser(Guid userId, string syncGatewayPassword)
            {
                UserId = userId;
                SyncGatewayPassword = syncGatewayPassword;
            }
        }

        public class RenderProjectDownloadObject
        {
            public string Username { get; }
            public string Password { get; }
            public List<Guid> GlobalUserIds { get; }

            public RenderProjectDownloadObject(string username, string password, List<Guid> projectUserIds)
            {
                Username = username;
                Password = password;
                GlobalUserIds = projectUserIds;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}