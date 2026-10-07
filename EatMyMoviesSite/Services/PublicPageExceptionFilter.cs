using EatMyMoviesSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EatMyMoviesSite.Services;

internal sealed class PublicPageExceptionFilter(ILogger<PublicPageExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch
        {
            MovieNotFoundException or InvalidListPageException => 404,
            MovieDataUnavailableException => 503,
            _ => 0
        };
        if (status == 0 || context.HttpContext.RequestAborted.IsCancellationRequested) return;

        logger.LogWarning(context.Exception, "Request could not be completed: HTTP {StatusCode}.", status);
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        if (controller == "List" || controller == "Home" || (controller == "Movie" && action == "Detail"))
        {
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/Error.cshtml",
                StatusCode = status,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState)
                {
                    Model = new ErrorViewModel { StatusCode = status }
                }
            };
        }
        else context.Result = new StatusCodeResult(status);
        context.ExceptionHandled = true;
    }
}
