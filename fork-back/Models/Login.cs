using System.ComponentModel.DataAnnotations;

namespace fork_back.Models
{
    public class LoginReference
    {
        [EmailAddress]
        [MaxLength(256)]
        public string Login { get; set; } = string.Empty;
    }

    public class LoginSaltResponce
    {
        [MaxLength(80)]
        public string HashType { get; set; } = string.Empty;

        [MaxLength(80)]
        public string Salt { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [EmailAddress]
        [MaxLength(256)]
        public string Login { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Hash { get; set; } = string.Empty;
    }

    public class LoginResponce
    {
        [EmailAddress]
        [MaxLength(256)]
        public string Login { get; set; } = string.Empty;

        public AccountRole Role { get; set; } = AccountRole.Developer;

        public string AccessTocken { get; set; } = string.Empty;

        public DateTime AccessValidTo { get; set; } = DateTime.UtcNow;
    }
}
