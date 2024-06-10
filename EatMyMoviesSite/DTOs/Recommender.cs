namespace EatMyMoviesSite.DTOs
{
	public class Recommender
	{
		public string CurrentGenre { get; set; }
		public MovieDetail RecommendedMovie { get; set; }
		public List<string> Genres { get; set; }
	}

}
