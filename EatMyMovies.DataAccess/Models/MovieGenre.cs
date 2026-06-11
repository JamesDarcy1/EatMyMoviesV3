namespace EatMyMovies.DataAccess.Models
{
    public class MovieGenre
    {
        public Guid GenreId { get; set; }

        public Genre Genre { get; set; } = null!;

        public Guid MovieId { get; set; }

        public Movie Movie  { get; set; } = null!;
    }
}
