using System.ComponentModel.DataAnnotations;

namespace EatMyMovies.DataAccess.Models
{
	public class ListRanking
    {
        [Key]
        public Guid ListRankingId { get; set; }

        public int Ranking { get; set; }

        public Movie Movie { get; set; }

        public List List { get; set; }
    }
}
