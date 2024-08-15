using System.Collections.Generic;

namespace EatMyMoviesSite.DTOs
{
	public class ListMovie
	{
		public string Title { get; set; }

		public string SmallPosterPath { get; set; }

		public string LargePosterPath { get; set; }

		public int Ranking { get; set; }

		public string Synopsis { get; set; }

		public string Genres { get; set; }

		public decimal? ImdbRating {  get; set; }

		public int? Runtime { get; set; }

        public string ReleaseDate { get; set; }
    }
}
