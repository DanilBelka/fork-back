using System.ComponentModel.DataAnnotations;

namespace fork_back.Models
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Url]
        public string Url { get; set; } = string.Empty;

        public List<Epic>? Epics { get; set; }
    }
}
