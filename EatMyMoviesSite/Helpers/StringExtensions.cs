using System.Text;
using System.Text.RegularExpressions;

public static class StringExtensions
{
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Convert to lowercase
        value = value.ToLowerInvariant();

        // Replace spaces with hyphens
        value = Regex.Replace(value, @"\s+", "-");

        // Remove invalid characters
        value = Regex.Replace(value, @"[^a-z0-9\-]", "");

        // Remove multiple hyphens in a row
        value = Regex.Replace(value, @"-+", "-");

        // Trim hyphens from ends
        value = value.Trim('-');

        return value;
    }
}
