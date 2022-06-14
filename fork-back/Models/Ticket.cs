namespace fork_back.Models
{
    public enum TicketState
    {
        Triage,
        Open,
        InProgress,
        Resolved,
        Verified,
    }

    public class Ticket
    {
        public int Id { get; set; }

        public int EpicId { get; set; }
        public Epic? Epic { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketState State { get; set; } = TicketState.Triage;

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime? Opened { get; set; }

        public DateTime? Resolved { get; set; }

        public DateTime? Verified { get; set; }

        public List<Account>? Accounts { get; set; }
    }
}
