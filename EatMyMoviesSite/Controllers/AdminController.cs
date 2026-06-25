using EatMyMoviesSite.Models.Admin;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EatMyMoviesSite.Controllers
{
    [Route("admin")]
    [AutoValidateAntiforgeryToken]
    [Authorize(Policy = AdminAuthorization.PolicyName)]
    public class AdminController : Controller
    {
        private readonly IAdminContentService _adminContentService;
        private readonly AdminAuthOptions _authOptions;

        public AdminController(IAdminContentService adminContentService, IOptions<AdminAuthOptions> authOptions)
        {
            _adminContentService = adminContentService;
            _authOptions = authOptions.Value;
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new AdminLoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!IsValidAdminLogin(model.Username, model.Password))
            {
                ModelState.AddModelError(string.Empty, "The username or password is incorrect.");
                return View(model);
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, _authOptions.Username),
                new Claim(AdminAuthorization.ClaimType, AdminAuthorization.ClaimValue)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await _adminContentService.BuildDashboardAsync(cancellationToken);
            return View(model);
        }

        [HttpGet("lists")]
        public async Task<IActionResult> Lists(CancellationToken cancellationToken)
        {
            var model = await _adminContentService.BuildListsAsync(cancellationToken);
            return View(model);
        }

        [HttpGet("movie-of-the-week")]
        public async Task<IActionResult> MovieOfTheWeek(string? tmdbQuery, CancellationToken cancellationToken)
        {
            var model = await _adminContentService.BuildMovieOfTheWeekAsync(tmdbQuery, cancellationToken);
            return View(model);
        }

        [HttpPost("movie-of-the-week")]
        public async Task<IActionResult> SetMovieOfTheWeek(
            int tmdbId,
            string? tmdbQuery,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.SetMovieOfTheWeekAsync(tmdbId, cancellationToken),
                "Movie of the Week updated.");

            return RedirectToAction(nameof(MovieOfTheWeek), new { tmdbQuery });
        }

        [HttpPost("movie-of-the-week/clear")]
        public async Task<IActionResult> ClearMovieOfTheWeek(CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.ClearMovieOfTheWeekAsync(cancellationToken),
                "Movie of the Week cleared.");

            return RedirectToAction(nameof(MovieOfTheWeek));
        }

        [HttpPost("lists")]
        public async Task<IActionResult> CreateList(string listName, string? description, CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.CreateListAsync(listName, description ?? string.Empty, cancellationToken),
                "List created.");

            return RedirectToAction(nameof(Lists));
        }

        [HttpGet("lists/{listId:guid}")]
        public async Task<IActionResult> ListDetail(Guid listId, string? tmdbQuery, CancellationToken cancellationToken)
        {
            var model = await _adminContentService.BuildListDetailAsync(listId, tmdbQuery, cancellationToken);
            return View(model);
        }

        [HttpPost("lists/{listId:guid}/update")]
        public async Task<IActionResult> UpdateList(
            Guid listId,
            string listName,
            string? description,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.UpdateListAsync(listId, listName, description ?? string.Empty, cancellationToken),
                "List updated.");

            return RedirectToAction(nameof(ListDetail), new { listId });
        }

        [HttpPost("lists/{listId:guid}/movies/tmdb")]
        public async Task<IActionResult> AddTmdbMovieToList(
            Guid listId,
            int tmdbId,
            int ranking,
            string? tmdbQuery,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.AddTmdbMovieToListAsync(listId, tmdbId, ranking, cancellationToken),
                "Movie added to list.");

            return RedirectToAction(nameof(ListDetail), new { listId, tmdbQuery });
        }

        [HttpPost("lists/{listId:guid}/movies/{movieId:guid}/ranking")]
        public async Task<IActionResult> MoveMovieInList(
            Guid listId,
            Guid movieId,
            int ranking,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.MoveMovieWithinListAsync(listId, movieId, ranking, cancellationToken),
                "Ranking updated.");

            return RedirectToAction(nameof(ListDetail), new { listId });
        }

        [HttpPost("lists/{listId:guid}/movies/{movieId:guid}/remove")]
        public async Task<IActionResult> RemoveMovieFromList(
            Guid listId,
            Guid movieId,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.RemoveMovieFromListAsync(listId, movieId, cancellationToken),
                "Movie removed from list.");

            return RedirectToAction(nameof(ListDetail), new { listId });
        }

        [HttpGet("movies")]
        public async Task<IActionResult> Movies(string? searchTerm, int page = 1, CancellationToken cancellationToken = default)
        {
            var model = await _adminContentService.BuildMoviesAsync(searchTerm, page, cancellationToken);
            return View(model);
        }

        [HttpGet("movies/{movieId:guid}")]
        public async Task<IActionResult> MovieDetail(Guid movieId, CancellationToken cancellationToken)
        {
            var model = await _adminContentService.BuildMovieDetailAsync(movieId, cancellationToken);
            return View(model);
        }

        [HttpPost("movies/{movieId:guid}/lists/add")]
        public async Task<IActionResult> AddStoredMovieToList(
            Guid movieId,
            Guid listId,
            int ranking,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.AddStoredMovieToListAsync(listId, movieId, ranking, cancellationToken),
                "Movie added to list.");

            return RedirectToAction(nameof(MovieDetail), new { movieId });
        }

        [HttpPost("movies/{movieId:guid}/lists/{listId:guid}/ranking")]
        public async Task<IActionResult> MoveMovieFromMovieDetail(
            Guid movieId,
            Guid listId,
            int ranking,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.MoveMovieWithinListAsync(listId, movieId, ranking, cancellationToken),
                "Ranking updated.");

            return RedirectToAction(nameof(MovieDetail), new { movieId });
        }

        [HttpPost("movies/{movieId:guid}/lists/{listId:guid}/remove")]
        public async Task<IActionResult> RemoveMovieFromMovieDetail(
            Guid movieId,
            Guid listId,
            CancellationToken cancellationToken)
        {
            await RunAdminActionAsync(
                () => _adminContentService.RemoveMovieFromListAsync(listId, movieId, cancellationToken),
                "Movie removed from list.");

            return RedirectToAction(nameof(MovieDetail), new { movieId });
        }

        private bool IsValidAdminLogin(string username, string password)
        {
            return string.Equals(username?.Trim(), _authOptions.Username, StringComparison.Ordinal)
                && AdminPasswordHasher.Verify(password, _authOptions.PasswordHash);
        }

        private async Task RunAdminActionAsync(Func<Task> action, string successMessage)
        {
            try
            {
                await action();
                TempData["AdminSuccess"] = successMessage;
            }
            catch (Exception ex) when (IsHandledAdminException(ex))
            {
                TempData["AdminError"] = ex.Message;
            }
        }

        private static bool IsHandledAdminException(Exception exception)
        {
            return exception is ArgumentException
                or ArgumentOutOfRangeException
                or DbUpdateException
                or InvalidOperationException;
        }
    }
}
