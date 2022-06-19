using fork_back.DataContext;
using fork_back.Models;
using fork_back.Utility;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace fork_back.Controllers
{
    public class LoginController : BaseController
    {
        DatabaseContext DataContext { get; init; }
        ILogger<LoginController> Logger { get; init; }

        public LoginController(DatabaseContext dbContext, ILogger<LoginController> logger)
        {
            DataContext = dbContext;
            Logger = logger;
        }


        [HttpPut("Salt")]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<LoginSaltResponce>> GetAccountSaltAsync(LoginReference loginReference)
        {
            var account = await DataContext.Accounts.FirstOrDefaultAsync(a => a.Login == loginReference.Login);
            if (account == default)
            {
                return NotFound();
            }

            var res = new LoginSaltResponce()
            {
                HashType = account.Seсurity?.HashType ?? string.Empty,
                Salt = account.Seсurity?.Salt ?? string.Empty
            };

            return res;
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<LoginResponce>> LoginAsync(LoginRequest loginRequest)
        {
            var account = await DataContext.Accounts.FirstOrDefaultAsync(a => a.Login == loginRequest.Login);
            if (account == default)
            {
                return NotFound();
            }

            var isLogingValid = account.Seсurity?.Hash == loginRequest.Hash;
            if (!isLogingValid)
            {
                return Unauthorized();
            }

            var jwt = Security.BuildAccessToken(account);

            var res = new LoginResponce()
            {
                Login = account.Login,
                Role = account.Role,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                AccessValidTo = jwt.ValidTo,
            };

            return res;
        }

        [HttpPost("IdToken")]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<LoginResponce>> LoginByIdTokenAsync(IdTokenLoginRequest loginRequest)
        {
            if (loginRequest.Provider != IdTokenProvider.Google)
            {
                ModelState.AddModelError(nameof(IdTokenLoginRequest.Provider), "Unknow IdToken provider");
                return ValidationProblem();
            }

            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configPath = Path.Combine(assemblyFolder ?? Directory.GetCurrentDirectory(), "google_client_secret.json");

            var clientSecrets = (await GoogleClientSecrets.FromFileAsync(configPath)).Secrets;

            var idTokenValidationSettings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new string[] { clientSecrets!.ClientId }
            };

            var idTokenPayload = await GoogleJsonWebSignature.ValidateAsync(loginRequest.IdToken, idTokenValidationSettings);
            if (string.IsNullOrEmpty(idTokenPayload.Email))
            {
                ModelState.AddModelError(nameof(IdTokenLoginRequest.IdToken), "IdToken does not contain account email");
                return ValidationProblem();
            }

            var account = await DataContext.Accounts.FirstOrDefaultAsync(a => a.Login == idTokenPayload.Email);
            if (account == default)
            {
                return NotFound();
            }

            var jwt = Security.BuildAccessToken(account);

            var res = new LoginResponce()
            {
                Login = account.Login,
                Role = account.Role,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                AccessValidTo = jwt.ValidTo,
            };

            return res;
        }
    }
}
