using System.ComponentModel.DataAnnotations;

namespace EatMyMovies.DataAccess.Models
{
    public class List
    {
		[Key]
		public Guid ListId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
