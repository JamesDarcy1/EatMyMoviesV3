using System.Text.RegularExpressions;

namespace EatMyMoviesSite
{
    public static class StringHelpers
    {
        public static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Add space before each uppercase letter that is not at the start of the string
            text = Regex.Replace(text, "(?<!^)([A-Z])", " $1");

            // Add space between digits and letters
            text = Regex.Replace(text, "(\\d)([A-Za-z])", "$1 $2");

            // Add space between letters and digits
            text = Regex.Replace(text, "([A-Za-z])(\\d)", "$1 $2");

            return text.Trim();
        }
    }
}
