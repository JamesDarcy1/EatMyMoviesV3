namespace EatMyMoviesSite.DTOs
{
	public class MovieRanking
	{
		public Guid MovieId { get; set; }
		public Guid ListId { get; set; }
		public string ListName { get; set; }
		public int Ranking { get; set; }
		public Guid ListRankingId { get; set; }
	}
}
