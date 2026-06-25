using System.ComponentModel.DataAnnotations;

namespace EatMyMovies.DataAccess.Models
{
    public sealed class MovieOfTheWeekSelection
    {
        public const int SingletonId = 1;

        [Key]
        public int MovieOfTheWeekSelectionId { get; set; } = SingletonId;

        public Guid MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public DateTime UpdatedUtc { get; set; }
    }
}
