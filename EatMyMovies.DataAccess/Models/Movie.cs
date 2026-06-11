using System.ComponentModel.DataAnnotations;

namespace EatMyMovies.DataAccess.Models
{
    public class Movie
    {
		[Key]
		public Guid MovieId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int? TmdbId { get; set; }
    }
}
