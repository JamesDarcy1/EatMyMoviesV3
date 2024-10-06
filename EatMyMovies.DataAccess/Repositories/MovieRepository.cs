using EatMyMovies.DataAccess.Models;
using System.Collections.Generic;

namespace EatMyMovies.DataAccess.Repositories
{
	public class MovieRepository : IMovieRepository
	{
		private readonly EatMyMoviesContext _dbContext;

		public MovieRepository(EatMyMoviesContext dbContext)
		{
			_dbContext = dbContext;
		}

		public Movie SaveTmdbMovie(string title, int tmdbId, decimal? imdbRating)
		{
			var movie = _dbContext.Movies.Add(new Movie()
			{
				Title = title,
				TmdbId = tmdbId,
			});


			_dbContext.SaveChanges();

			return movie.Entity;
		}

		public Movie GetMovieByTitle(string title)
		{
			var movie = _dbContext.Movies.FirstOrDefault(x => x.Title == title);
			return movie;
		}

        public IEnumerable<Movie> GetAllMovies()
        {
            var movies = _dbContext.Movies;
            return movies;
        }

        public void SaveGenres(Movie movie, IEnumerable<string> genres)
        {
			var existingGenres = _dbContext.Genres.Select(x => x.Name).ToList();
			foreach (var genre in genres)
			{
				if (!existingGenres.Contains(genre))
				{
					_dbContext.Genres.Add(new Genre() { Name = genre });
                    _dbContext.SaveChanges();
                }
				var genreStore = _dbContext.Genres.FirstOrDefault(x => x.Name == genre);
				_dbContext.MovieGenres.Add(new MovieGenre() { Genre = genreStore, Movie = movie });
                _dbContext.SaveChanges();
            }
        }

		public List<Genre> GetAllGenres()
		{
			var genres = _dbContext.Genres.ToList();
			return genres;
		}

		public List<Movie> GetMoviesOfGenres(List<string> genres)
		{
			var moviesOfGenre = _dbContext.MovieGenres.Where(x => genres.Contains(x.Genre.Name)).Select(x => x.Movie).ToList();
			return moviesOfGenre;
		}

        public IQueryable<string> GetGenresOfMovie(Movie movie)
        {
			var genresOfMovie = _dbContext.MovieGenres.Where(x => x.Movie.MovieId == movie.MovieId).Select(x => x.Genre.Name);
            return genresOfMovie;
        }
    }
}
