using System.Collections.Generic;

namespace EatMyMoviesSite.DTOs
{
	public class ListMovie
	{
		public string Title { get; set; }

		public string PosterPath { get; set; }

		public int Ranking { get; set; }

		public IEnumerable<string> Genres { get; set; } = new List<string>();

		public Decimal ImdbRating {  get; set; }
	}
}
