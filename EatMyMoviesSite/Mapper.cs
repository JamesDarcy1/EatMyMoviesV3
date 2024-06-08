using EatMyMoviesSite.DTOs;
using Newtonsoft.Json;
using System.Globalization;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace EatMyMoviesSite
{
	public static class Mapper
	{
		public static ListMovie BuildListMovie(Movie tmdbMovie, decimal imdbRating, int ranking)
		{
			return new ListMovie
			{
				Title = tmdbMovie.Title,
				PosterPath = $"https://image.tmdb.org/t/p/w154{tmdbMovie.PosterPath}",
				Genres = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name)),
				ImdbRating = imdbRating,
				Ranking = ranking,
				Synopsis = tmdbMovie.Overview
			};
		}

		public static MovieDetail MapToMovieDetail(Movie tmdbMovie, Video trailer, decimal imdbRating)
		{
			return new MovieDetail
			{
				Title = tmdbMovie.Title,
				PosterPath = tmdbMovie.PosterPath,
				BackdropPath = tmdbMovie.BackdropPath,
				Overview = tmdbMovie.Overview,
				ReleaseDate = tmdbMovie.ReleaseDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				TrailerPath = $"https://www.youtube.com/embed/{trailer.Key}",
				Tagline = tmdbMovie.Tagline,
				Genres = tmdbMovie.Genres.Select(g => g.Name),
				ImdbRating = imdbRating
			};
		}

        public static ListMovie MapToMovieSummary(Movie tmdbMovie, decimal imdbRating)
        {
            return new ListMovie
            {
                Title = tmdbMovie.Title,
                PosterPath = $"https://image.tmdb.org/t/p/w154{tmdbMovie.PosterPath}",
                Genres = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name)),
                ImdbRating = imdbRating,
                Synopsis = tmdbMovie.Overview
            };
        }
    }
}
