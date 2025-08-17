using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace EatMyMovies.DataAccess.Repositories
{
    public class RankingRepository : IRankingRepository
    {
        private readonly EatMyMoviesContext _dbContext;
        private int MoviesPerPage = 10;

        public RankingRepository(EatMyMoviesContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<Movie> GetMoviesForListByPage(string listName, int page = 1)
        {
            var movies = _dbContext.ListRankings.Where(l => l.List.Name == listName)
                                                .OrderBy(m => m.Ranking)
                                                .Skip((page - 1) * MoviesPerPage)
                                                .Take(MoviesPerPage)
                                                .Select(m => m.Movie);
            return movies;
        }

        public IQueryable<Movie> GetAllMoviesInList(string listName)
        {
            var movies = _dbContext.ListRankings.Where(l => l.List.Name == listName)
                                                .Select(m => m.Movie);
            return movies;
        }


        public int GetListCount(string listName)
        {
            var count = _dbContext.ListRankings.Count(l => l.List.Name == listName);

            return count;
        }

        public int GetRankingOfMovie(Guid movieId, string listName)
        {
            var listRanking = _dbContext.ListRankings.FirstOrDefault(r => r.List.Name == listName && r.Movie.MovieId == movieId);
            if (listRanking == null)
            {
                throw new Exception("Film does not exist in list");
            }
            return listRanking.Ranking;
        }

        public ListRanking InsertMovieToList(Movie movie, List list, int ranking)
        {
            var result = _dbContext.Add(new ListRanking()
            {
                List = list,
                Movie = movie,
                Ranking = ranking
            });

            _dbContext.SaveChanges();

            return result.Entity;
        }

        public ListRanking GetMovieAtRanking(List list, int ranking)
        {
            var listRanking = _dbContext.ListRankings.FirstOrDefault(lr => lr.List == list && lr.Ranking == ranking);
            return listRanking;
        }

        public bool FilmExistsInList(Guid movieId, Guid listId)
        {
            return _dbContext.ListRankings.Any(lr => lr.Movie.MovieId == movieId && lr.List.ListId == listId);
        }

        public IEnumerable<ListRanking> GetAllRankingsInList(List list)
        {
            var listRankings = _dbContext.ListRankings.Where(lr => lr.List == list)
                                                      .Include(lr => lr.Movie)
                                                      .OrderBy(lr => lr.Ranking).ToList();
            return listRankings;
        }

        public ListRanking UpdateRanking(ListRanking listRanking, int newRanking)
        {
            listRanking.Ranking = newRanking;
            var updatedListRanking = _dbContext.ListRankings.Update(listRanking);
            _dbContext.SaveChanges();

            return updatedListRanking.Entity;
        }

        public void RemoveRanking(int ranking, Guid listId)
        {
            var listRanking = _dbContext.ListRankings.FirstOrDefault(lr => lr.List.ListId == listId && lr.Ranking == ranking);
            if (listRanking != null)
            {
                _dbContext.ListRankings.Remove(listRanking);
                _dbContext.SaveChanges();
            }
        }

        public List<ListRanking> GetListRankingsForMovie(Guid movieId)
        {
            var listRankings = _dbContext.ListRankings.Include(lr => lr.List).Where(lr => lr.Movie.MovieId == movieId);
            return listRankings.ToList();
        }

        public ListRanking GetListRanking(Guid movieId, Guid listId)
        {
            var listRanking = _dbContext.ListRankings.FirstOrDefault(lr => lr.Movie.MovieId == movieId && lr.List.ListId == listId);
            return listRanking;
        }

        public void RemoveListRanking(Guid movieId, Guid listId)
        {
            var listRanking = GetListRanking(movieId, listId);
            
            if (listRanking != null)
            {
                _dbContext.ListRankings.Remove(listRanking);
                _dbContext.SaveChanges();
            }
        }


    }
}
