using System.ComponentModel.DataAnnotations;

namespace EatMyMovies.DataAccess.Models
{
    public class Genre
    {
        [Key]
		public Guid GenreId { get; set; }

        public string Name  { get; set; } = string.Empty;

        public ICollection<MovieGenre> MovieGenres { get; set; } = [];
    }
}
