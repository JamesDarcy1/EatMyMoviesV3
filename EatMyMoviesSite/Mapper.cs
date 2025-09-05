using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using EatMyMoviesSite.Services;
using Newtonsoft.Json;
using System.Globalization;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace EatMyMoviesSite
{
	public static class Mapper
	{
		public static ListMovie BuildListMovie(Movie tmdbMovie, decimal? imdbRating, int ranking)
		{
			return new ListMovie
			{
				Title = tmdbMovie.Title,
				PosterPath = $"https://image.tmdb.org/t/p/w185{tmdbMovie.PosterPath}",
				Genres = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name)),
				ImdbRating = imdbRating != null ? imdbRating : null,
				Ranking = ranking,
				Synopsis = tmdbMovie.Overview,
				Runtime = tmdbMovie.Runtime,
				ReleaseDate = tmdbMovie.ReleaseDate.Value.ToString("yyyy", CultureInfo.InvariantCulture),
				Language = LanguageHelper.GetLanguageName(tmdbMovie.OriginalLanguage),
                TmdbId = tmdbMovie.Id,
            };
		}

		public static MovieDetail MapToMovieDetail(Movie tmdbMovie, Video trailer, decimal? imdbRating, Person director, List<Person> actors)
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
                Language = LanguageHelper.GetLanguageName(tmdbMovie.OriginalLanguage),
                Director = director,
                Actors = actors.Take(6).ToList(),
                CanEdit = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "development", StringComparison.InvariantCultureIgnoreCase)
            };
		}

        public static MovieDetail MapToTvShowDetail(TvShow tmdbShow, Video trailer, decimal? imdbRating, Person director, List<Person> actors)
        {
            return new MovieDetail
            {
                Title = tmdbShow.Name,
                PosterPath = tmdbShow.PosterPath,
                BackdropPath = tmdbShow.BackdropPath,
                Overview = tmdbShow.Overview,
                ReleaseDate = tmdbShow.FirstAirDate.Value.ToString("yyyy", CultureInfo.InvariantCulture),
                TrailerPath = trailer != null ? $"https://www.youtube.com/embed/{trailer.Key}" : null,
                Tagline = tmdbShow.Tagline,
                Genres = string.Join(", ", tmdbShow.Genres.Select(g => g.Name)),
                ImdbRating = imdbRating != null ? imdbRating : null,
                TmdbId = tmdbShow.Id,
                Language = LanguageHelper.GetLanguageName(tmdbShow.OriginalLanguage),
                Director = director,
                Actors = actors.Take(6).ToList(),
                CanEdit = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "development", StringComparison.InvariantCultureIgnoreCase)
            };
        }

        public static ListMovie MapToMovieSummary(Movie tmdbMovie, decimal? imdbRating, string director)
        {
            return new ListMovie
            {
                Title = tmdbMovie.Title,
                PosterPath = $"https://image.tmdb.org/t/p/w342{tmdbMovie.PosterPath}",
                Genres = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name)),
                ImdbRating = imdbRating,
                Synopsis = tmdbMovie.Overview,
				Runtime = tmdbMovie.Runtime,
				ReleaseDate = tmdbMovie.ReleaseDate.Value.ToString("yyyy", CultureInfo.InvariantCulture),
                Language = LanguageHelper.GetLanguageName(tmdbMovie.OriginalLanguage),
                TmdbId= tmdbMovie.Id,
                Director = director,
            };
        }

        public static Dictionary<Feeling, List<string>> GetFeelingToGenreMapping()
        {
            return new Dictionary<Feeling, List<string>>
        {
            { Feeling.FeelGood, new List<string> { "Comedy", "Family", "Romance", "Animation" } },
            { Feeling.Emotional, new List<string> { "Drama", "Romance", "Music", "History" } },
            //{ Feeling.Exciting, new List<string> { "Action", "Adventure", "Thriller", "War", "Science Fiction" } },
            //{ Feeling.ThoughtProvoking, new List<string> { "Drama", "Mystery", "Science Fiction", "Documentary" } },
            { Feeling.Funny, new List<string> { "Comedy", "Family", "Animation" } },
            { Feeling.Romantic, new List<string> { "Romance", "Drama" } },
            { Feeling.Adventurous, new List<string> { "Adventure", "Fantasy", "Action", "Animation" } },
            { Feeling.Mysterious, new List<string> { "Mystery", "Thriller", "Crime" } },
            { Feeling.Scary, new List<string> { "Horror", "Thriller" } },
            //{ Feeling.Nostalgic, new List<string> { "Family", "History", "Western", "War" } },
            //{ Feeling.Musical, new List<string> { "Music", "Romance", "Drama" } },
            { Feeling.Inspiring, new List<string> { "Documentary", "Drama", "History", "War" } }
        };
        }
    }
}
