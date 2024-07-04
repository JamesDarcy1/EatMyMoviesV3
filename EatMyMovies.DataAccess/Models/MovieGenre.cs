namespace EatMyMovies.DataAccess.Models
{
    public class MovieGenre
    {
        public Guid MovieGenreId { get; set; }

        public Genre Genre { get; set; }

        public Movie Movie  { get; set; }
    }
}
