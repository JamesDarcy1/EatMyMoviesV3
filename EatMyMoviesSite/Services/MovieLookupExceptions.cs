namespace EatMyMoviesSite.Services;

internal sealed class MovieNotFoundException(string message, Exception? innerException = null)
    : Exception(message, innerException);

internal sealed class InvalidListPageException() : Exception("This list page does not exist.");

internal sealed class MovieDataUnavailableException(string message, Exception? innerException = null)
    : HttpRequestException(message, innerException);
