using fork_back.DataContext;
using fork_back.Models;
using fork_back.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;

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

            var claims = new List<Claim> 
            {
                new Claim(nameof(Account.Id), account.Id.ToString()),
                new Claim(ClaimTypes.Role, account.Role.ToString()),
                new Claim(ClaimTypes.Upn, account.Login),
                new Claim(ClaimTypes.Name, account.FirstName),
                new Claim(ClaimTypes.Surname, account.LastName), 
            };

            var jwt = new JwtSecurityToken(
                    issuer: Seсurity.JwtIssuer,
                    audience: Seсurity.JwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(Seсurity.JwtAccessExperation),
                    signingCredentials: new SigningCredentials(Seсurity.JwtSecurityKey, SecurityAlgorithms.HmacSha256));

            var res = new LoginResponce()
            {
                Login = account.Login,
                Role = account.Role,
                AccessTocken = new JwtSecurityTokenHandler().WriteToken(jwt),
                AccessValidTo = jwt.ValidTo,
            };

            return res;
        }
    }
}
