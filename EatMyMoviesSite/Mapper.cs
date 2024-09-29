using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Services;
using Newtonsoft.Json;
using System.Globalization;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace EatMyMoviesSite
{
	public static class Mapper
	{
		public static ListMovie BuildListMovie(Movie tmdbMovie, decimal? imdbRating, int ranking)
		{
			return new ListMovie
			{
				Title = tmdbMovie.Title,
				PosterPath = $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}",
				Genres = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name)),
				ImdbRating = imdbRating != null ? imdbRating : null,
				Ranking = ranking,
				Synopsis = tmdbMovie.Overview,
				Runtime = tmdbMovie.Runtime,
				ReleaseDate = tmdbMovie.ReleaseDate.Value.ToString("yyyy", CultureInfo.InvariantCulture),
				Language = LanguageHelper.GetLanguageName(tmdbMovie.OriginalLanguage)
            };
		}

		public static MovieDetail MapToMovieDetail(Movie tmdbMovie, Video trailer, decimal? imdbRating)
		{
			return new MovieDetail
			{
				Title = tmdbMovie.Title,
				PosterPath = tmdbMovie.PosterPath,
				BackdropPath = tmdbMovie.BackdropPath,
				Overview = tmdbMovie.Overview,
				ReleaseDate = tmdbMovie.ReleaseDate.Value.ToString("yyyy", CultureInfo.InvariantCulture),
				TrailerPath = trailer != null ? $"https://www.youtube.com/embed/{trailer.Key}" : null,
				Tagline = tmdbMovie.Tagline,
				Genres = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name)),
				ImdbRating = imdbRating != null ? imdbRating : null,
				Runtime = tmdbMovie.Runtime,
				TmdbId = tmdbMovie.Id,
                Language = LanguageHelper.GetLanguageName(tmdbMovie.OriginalLanguage)
            };
		}

        public static ListMovie MapToMovieSummary(Movie tmdbMovie, decimal? imdbRating)
        {
            return new ListMovie
            {
                Title = tmdbMovie.Title,
                PosterPath = $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}",
                Genres = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name)),
                ImdbRating = imdbRating,
                Synopsis = tmdbMovie.Overview,
				Runtime = tmdbMovie.Runtime,
				ReleaseDate = tmdbMovie.ReleaseDate.Value.ToString("yyyy", CultureInfo.InvariantCulture),
                Language = LanguageHelper.GetLanguageName(tmdbMovie.OriginalLanguage)
            };
        }
    }
}
