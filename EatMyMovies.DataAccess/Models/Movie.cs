using System.ComponentModel.DataAnnotations;

namespace EatMyMovies.DataAccess.Models
{
    public class Movie
    {
		[Key]
		public Guid MovieId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int? TmdbId { get; set; }

        public ICollection<ListRanking> ListRankings { get; set; } = [];

        public ICollection<MovieGenre> MovieGenres { get; set; } = [];
    }
}
