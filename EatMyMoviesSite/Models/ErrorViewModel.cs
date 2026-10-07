namespace EatMyMoviesSite.Models
{
    public class ErrorViewModel
    {
        public int StatusCode { get; set; } = 500;
        public string Heading => StatusCode switch
        {
            404 => "Page not found",
            503 => "Temporarily unavailable",
            _ => "Something went wrong"
        };
        public string Message => StatusCode switch
        {
            404 => "We couldn't find that page or movie. Try a movie search or explore our recommendations.",
            503 => "We can't load the movie information right now. Please try again shortly.",
            _ => "We couldn't complete your request. Please try again or head back home."
        };
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
