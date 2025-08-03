using System.Text.Json;

namespace EatMyMoviesSite.Services
{
    public static class LanguageHelper
    {
        private static readonly Dictionary<string, string> _languageDictionary;

        static LanguageHelper()
        {
            // Path to the JSON file. Adjust the path as necessary.
            string jsonFilePath = "./Languages.json";
            string jsonContent = File.ReadAllText(jsonFilePath);
            _languageDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
        }

        public static string GetLanguageName(string languageCode)
        {
            if (_languageDictionary.TryGetValue(languageCode, out string languageName))
            {
                return languageName;
            }
            else
            {
                return "Unknown Language"; // Fallback for unrecognized language codes
            }
        }
    }
}
