using fork_back.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace fork_back.Utility
{
    static class Security
    {
        static readonly Random random = new Random();
        const string jwtKey = "pKDxnlRhVMMpNWjMFYHWBgqk5rRxFMpJZ2mdEJwY";

        internal static string JwtIssuer => "ForkSecurity";
        internal static string JwtAudience => "Fork-Back";
        internal static TimeSpan JwtAccessExperation => TimeSpan.FromMinutes(60);
        internal static SecurityKey JwtSecurityKey => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        internal static string GenerateSalt()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 80;

            return new string(Enumerable.Range(0, length)
                                        .Select(_ => chars[random.Next(chars.Length)])
                                        .ToArray());
        }

        internal static string GetSHA256Hash(string input)
        {
            var res = string.Empty;

            using (var sha256 = SHA256.Create())
            {
                res = GetHash(sha256, input);
            }

            return res;
        }

        internal static JwtSecurityToken BuildAccessToken(Account account)
        {
            var claims = new List<Claim>
            {
                new Claim(nameof(Account.Id), account.Id.ToString()),
                new Claim(ClaimTypes.Role, account.Role.ToString()),
                new Claim(ClaimTypes.Upn, account.Login),
                new Claim(ClaimTypes.Name, account.FirstName),
                new Claim(ClaimTypes.Surname, account.LastName),
            };

            var jwt = new JwtSecurityToken(
                    issuer: JwtIssuer,
                    audience: JwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(JwtAccessExperation),
                    signingCredentials: new SigningCredentials(JwtSecurityKey, SecurityAlgorithms.HmacSha256));

            return jwt;
        }


        // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.hashalgorithm.computehash?view=net-6.0
        static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
