using System.ComponentModel.DataAnnotations;

namespace fork_back.Models
{
    public class Account
    {
        public int Id { get; set; }

        [EmailAddress]
        public string Login { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public List<Ticket>? Tickets { get; set; }
    }
}
