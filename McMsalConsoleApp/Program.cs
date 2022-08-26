using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;


namespace McMsalConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            authenticate().Wait();
        }
        
        static async Task authenticate() {
            // Below are the clientId (Application Id) of your app registration and the tenant information. 
            // You have to replace:
            // - the content of ClientID with the Application Id for your app registration
            // - The content of Tenant by the information about the accounts allowed to sign-in in your application:
            //   - For Work or School account in your org, use your tenant ID, or domain
            //   - for any Work or School accounts, use organizations
            //   - for any Work or School accounts, or Microsoft personal account, use consumers
            //   - for Microsoft Personal account, use consumers
            const string ClientId = "b91b3560-b580-45cc-8bdb-86c06279f4c4";
            //string ClientId = "3e3a9bda-6e1c-45a8-a945-1a1dd832aff6";

            // Note: Tenant is important for the quickstart.
            string Tenant = "consumers";
            string Instance = "https://login.microsoftonline.com/";
            string newLine = Environment.NewLine;

            //create auth application
            var builder = PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority($"{Instance}{Tenant}")
                    .WithDefaultRedirectUri();

            builder.WithWindowsBroker(true);  // Requires redirect URI "ms-appx-web://microsoft.aad.brokerplugin/{client_id}" in app registration
            IPublicClientApplication app = builder.Build();

            TokenCacheHelper.EnableSerialization(app.UserTokenCache);
            string[] scopes = new string[] { "user.read" };
            AuthenticationResult authResult = null;
            IAccount account = PublicClientApplication.OperatingSystemAccount;

            try
            {
                authResult = await app
                    .AcquireTokenSilent(scopes, account)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(account)
                        .WithParentActivityOrWindow(GetForegroundWindow()) // optional, used to center the browser on the window
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    Console.WriteLine( $"Error Acquiring Token:{newLine}{msalex}" );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine( $"Error Acquiring Token Silently:{newLine}{ex}" );
                return;
            }

            if (authResult != null)
            {
                string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

                Console.WriteLine( "AccessToken:" + newLine + authResult.AccessToken);
                Console.WriteLine( "=====" + newLine +
                    "IdToken:" + newLine + authResult.IdToken
                );

                Console.WriteLine(
                    "=====" + newLine + 
                    "Token Info:" + newLine +
                    $"Username: {authResult.Account.Username}" + newLine +
                    $"Token Expires: {authResult.ExpiresOn.ToLocalTime()}"
                );

                var content = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                Console.WriteLine(
                    "=====" + newLine +
                    $"Graph API (https://graph.microsoft.com/v1.0/me) results:\n:{content}"
                );
            }

        }
        public static async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

    }
}
