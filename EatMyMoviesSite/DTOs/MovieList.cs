namespace EatMyMoviesSite.DTOs
{
	public class MovieList
	{
		public string Name { get; set; }
		public List<ListMovie> Movies { get; set; }
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
	}
}
