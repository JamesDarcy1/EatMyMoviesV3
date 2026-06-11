using System.ComponentModel.DataAnnotations;

namespace EatMyMovies.DataAccess.Models
{
    public class List
    {
		[Key]
		public Guid ListId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public ICollection<ListRanking> ListRankings { get; set; } = [];
    }
}
