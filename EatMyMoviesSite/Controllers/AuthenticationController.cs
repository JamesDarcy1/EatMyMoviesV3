using EatMyMovies.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{
	public class AuthenticationController : Controller
	{
        public AuthenticationController()
        {
			// External auth service
        }

		public IActionResult Login()
		{
			return View("~/Views/Shared/Login.cshtml");
		}

		[HttpPost]
		public bool Authenticate(string username, string password)
		{
			return true;
		}
	}
}
