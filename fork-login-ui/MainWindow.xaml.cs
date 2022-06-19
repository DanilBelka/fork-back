using fork_back.Models;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace fork_login_ui
{
    public partial class MainWindow : Window
    {
        Uri ForkBackBaseUri => new Uri("https://localhost:7234/");

        string ClientName => "Fork";

        string[] ClientScopes => new string[] { Oauth2Service.Scope.UserinfoEmail };

        ClientSecrets? ClientSecrets { get; set; }
       
        UserCredential? UserCredential { get; set; }

        HttpClient HttpClient { get; } = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnLoginAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                UserCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                        ClientSecrets,
                                        ClientScopes,
                                        ClientName,
                                        CancellationToken.None);

                if (UserCredential != default)
                {
                    if (UserCredential.Token.IsExpired(UserCredential.Flow.Clock))
                    {
                        await UserCredential.RefreshTokenAsync(CancellationToken.None);
                    }

                    var accessToken = UserCredential.Token.AccessToken;
                    var idToken = UserCredential.Token.IdToken;

                    var jsonOptions = new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // login to fork-back
                    var loginInfo = default(LoginResponce);
                    {
                        var request = new IdTokenLoginRequest()
                        { 
                            IdToken = idToken, 
                            AccessToken = accessToken 
                        };

                        var httpContent = new StringContent(JsonSerializer.Serialize(request, jsonOptions), Encoding.UTF8, MediaTypeNames.Application.Json);

                        using (var responce = await HttpClient.PostAsync(new Uri(ForkBackBaseUri, "api/Login/IdToken"), httpContent))
                        {
                            responce.EnsureSuccessStatusCode();

                            var stringStream = await responce.Content.ReadAsStringAsync();
                            loginInfo = JsonSerializer.Deserialize<LoginResponce>(stringStream, jsonOptions);
                        }
                    }

                    // me endpoint
                    var account = default(Account);
                    {
                        using (var meRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(ForkBackBaseUri, "api/Account/Me")))
                        {
                            meRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, loginInfo!.AccessToken);

                            using (var responce = await HttpClient.SendAsync(meRequest))
                            {
                                responce.EnsureSuccessStatusCode();

                                var stringStream = await responce.Content.ReadAsStringAsync();
                                account = JsonSerializer.Deserialize<Account>(stringStream, jsonOptions);
                            }
                        }
                    }

                    if (account != default)
                    {
                        var accountText = new StringBuilder();
                        accountText.Append($"Account: {account.Login}\n");
                        accountText.Append($"Name: {account.FirstName}{account.LastName}\n");
                        accountText.Append($"Role: {account.Role}\n");
                        accountText.Append($"Access Token: {loginInfo.AccessToken}\n");
                        accountText.Append($"Access Expires: {loginInfo.AccessValidTo.ToLocalTime()}\n");

                        accountTextInfo.Text = accountText.ToString();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var keyName = assembly.GetManifestResourceNames()
                                  .Single(s => s.EndsWith("google_client_secret.json"));

            using (var stream = assembly.GetManifestResourceStream(keyName))
            {
                ClientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            }
        }
    }
}
