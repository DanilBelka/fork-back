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

        public DateTime? DateCreated { get; set; }

        public DateTime? DateOpened { get; set; }

        public DateTime? DateResolved { get; set; }

        public DateTime? DateVerified { get; set; }

        public List<Account>? Accounts { get; set; }
    }

    public class TicketReference
    {
        public int Id { get; set; }
    }

    public class TicketStateData
    {
        public TicketState State { get; set; } = TicketState.Triage;
    }
}
