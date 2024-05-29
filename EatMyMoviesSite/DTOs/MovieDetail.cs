using System.Collections.Generic;

namespace EatMyMoviesSite.DTOs
{
	public class MovieDetail
	{
		public string Title { get; set; }
		public string Tagline { get; set; }
		public string ReleaseDate { get; set; }
		public string PosterPath { get; set; }
		public string BackdropPath { get; set; }
		public string TrailerPath { get; set; }
		public string Overview { get; set; }
		public decimal ImdbRating { get; set; }
		public IEnumerable<string> Genres { get; set; }
	}
}
