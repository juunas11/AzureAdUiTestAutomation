using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UiTestAutomation.Tests.Entities;
using Xunit;

namespace UiTestAutomation.Tests
{
    public class FetchDataTests
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly string _username;
        private readonly string _password;
        private readonly string _authority;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string[] _scopes;
        private readonly RemoteWebDriver _driver;

        public FetchDataTests()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<FetchDataTests>()
                .Build();
            _username = config["username"];
            _password = config["password"];
            _authority = config["authority"];
            _clientId = config["clientId"];
            _clientSecret = config["clientSecret"];
            _scopes = config.GetSection("scopes").Get<string[]>();
            _driver = new ChromeDriver();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        [Fact]
        public async Task GivenTablePageIsLoadedThenFiveRowsAreReturned()
        {
            try
            {
                _driver.Navigate().GoToUrl("https://localhost:44338");
                await AcquireAndSetTokensAsync();
                _driver.Navigate().Refresh();

                var fetchDataLink = _driver.FindElementByCssSelector("nav .nav-item:last-child > a");
                fetchDataLink.Click();

                var tableRows = _driver.FindElementsByCssSelector(".container table > tbody > tr");
                Assert.Equal(5, tableRows.Count);
            }
            finally
            {
                _driver.Quit();
            }
        }

        private async Task AcquireAndSetTokensAsync()
        {
            var tokenResponse = await AcquireTokensAsync();
            var idToken = new JwtSecurityToken(tokenResponse.IdToken);
            var localAccountId = idToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value
                    ?? idToken.Claims.First(c => c.Type == "sid").Value;
            var realm = idToken.Claims.First(c => c.Type == "tid").Value;
            // Normally MSAL.js would form this from client_info.uid and utid
            var homeAccountId = $"{localAccountId}.{realm}";
            var preferredUsername = idToken.Claims.First(c => c.Type == "preferred_username").Value;
            var name = idToken.Claims.First(c => c.Type == "name").Value;

            var accountKey = $"{homeAccountId}-login.windows.net-{realm}";
            MsalAccountEntity accountEntity = BuildAccountEntity(homeAccountId, realm, localAccountId, preferredUsername, name);

            var idTokenKey = $"{homeAccountId}-login.windows.net-idtoken-{_clientId}-{realm}-";
            MsalIdTokenEntity idTokenEntity = BuildIdTokenEntity(homeAccountId, _clientId, tokenResponse.IdToken, realm);

            var accessTokenKey = $"{homeAccountId}-login.windows.net-accesstoken-{_clientId}-{realm}-{string.Join(" ", _scopes)}";
            MsalAccessTokenEntity accessTokenEntity = BuildAccessTokenEntity(homeAccountId, tokenResponse.AccessToken, tokenResponse.ExpiresIn, tokenResponse.ExtExpiresIn, _clientId, realm, _scopes);

            SetLocalStorageItem(_driver, accountKey, accountEntity);
            SetLocalStorageItem(_driver, idTokenKey, idTokenEntity);
            SetLocalStorageItem(_driver, accessTokenKey, accessTokenEntity);
        }

        private static MsalAccessTokenEntity BuildAccessTokenEntity(
            string homeAccountId,
            string accessToken,
            int expiresIn,
            int extExpiresIn,
            string clientId,
            string realm,
            string[] scopes)
        {
            return new MsalAccessTokenEntity
            {
                HomeAccountId = homeAccountId,
                CredentialType = "AccessToken",
                Secret = accessToken,
                CachedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ExpiresOn = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn).ToString(),
                ExtendedExpiresOn = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + extExpiresIn).ToString(),
                Environment = "login.windows.net",
                ClientId = clientId,
                Realm = realm,
                Target = string.Join(" ", scopes.Select(s => s.ToLower()))
                // Scopes _must_ be lowercase or the token won't be found
            };
        }

        private static MsalAccountEntity BuildAccountEntity(
            string homeAccountId,
            string realm,
            string localAccountId,
            string username,
            string name)
        {
            return new MsalAccountEntity
            {
                AuthorityType = "MSSTS",
                ClientInfo = "", //EncodeClientInfo(localAccountId, realm)
                HomeAccountId = homeAccountId,
                Environment = "login.windows.net",
                Realm = realm,
                LocalAccountId = localAccountId,
                Username = username,
                Name = name
            };
        }

        private static MsalIdTokenEntity BuildIdTokenEntity(
            string homeAccountId,
            string clientId,
            string idToken,
            string realm)
        {
            return new MsalIdTokenEntity
            {
                CredentialType = "IdToken",
                HomeAccountId = homeAccountId,
                Environment = "login.windows.net",
                ClientId = clientId,
                Secret = idToken,
                Realm = realm
            };
        }

        private async Task<AadTokenResponse> AcquireTokensAsync()
        {
            string tokenUrl = _authority + "/oauth2/v2.0/token";
            var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = _clientId,
                    ["client_secret"] = _clientSecret,
                    ["scope"] = string.Join(" ", new[] { "openid", "profile" }.Concat(_scopes)),
                    ["username"] = _username,
                    ["password"] = _password
                })
            };

            HttpResponseMessage res = await HttpClient.SendAsync(req);

            string json = await res.Content.ReadAsStringAsync();
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Failed to acquire token");
            }

            return JsonConvert.DeserializeObject<AadTokenResponse>(json);
        }

        private static void SetLocalStorageItem(
            IJavaScriptExecutor driver,
            string key,
            object value)
        {
            var json = JsonConvert.SerializeObject(value, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });

            driver.ExecuteScript($"window.localStorage.setItem('{key}', '{json}')");
        }

        // This isn't required for tests, but can be included for completeness
        private static string EncodeClientInfo(string localAccountId, string realm)
        {
            string json = $"{{\"uid\":\"{localAccountId}\",\"utid\":\"{realm}\"}}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var urlSafeBase64 = base64.Replace("=", "").Replace("/", "_").Replace("+", "-");
            return urlSafeBase64;
        }
    }
}
