using fork_back.Utility;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace fork_back.Models
{
    public enum AccountRole
    {
        Administrator,
        Manager,
        Developer
    }

    public class Account
    {
        public int Id { get; set; }

        [EmailAddress]
        [MaxLength(256)]
        public string Login { get; set; } = string.Empty;

        public AccountRole Role { get; set; } = AccountRole.Developer;

        [MaxLength(256)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string LastName { get; set; } = string.Empty;

        public AccountSeсurity? Seсurity { get; set; }

        public List<Ticket>? Tickets { get; set; }
    }

    [Owned]
    public class AccountSeсurity
    {
        [MaxLength(256)]
        public string Hash { get; set; } = string.Empty;

        [MaxLength(80)]
        public string HashType { get; set; } = string.Empty;

        [MaxLength(80)]
        public string Salt { get; set; } = string.Empty;

        internal static AccountSeсurity Build(string password)
        {
            var salt = Seсurity.GenerateSalt();
            Debug.Assert(salt.Length == 80);

            var res = new AccountSeсurity()
            {
                Hash = Seсurity.GetSHA256Hash($"{password}{salt}"),
                HashType = "sha256",
                Salt = salt
            };

            return res;
        }
    }

    public class AccountReference
    {
        public int Id { get; set; }
    }
}
