namespace fork_back.Models
{
    public class Epic
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<Ticket>? Tickets { get; set; }
    }
}
