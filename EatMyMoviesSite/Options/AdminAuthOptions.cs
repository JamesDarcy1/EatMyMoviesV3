using EatMyMoviesSite.Services;
using System.ComponentModel.DataAnnotations;

namespace EatMyMoviesSite.Options
{
    public sealed class AdminAuthOptions : IValidatableObject
    {
        public const string SectionName = "AdminAuth";

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(PasswordHash) && !AdminPasswordHasher.IsSupportedHash(PasswordHash))
            {
                yield return new ValidationResult(
                    "Admin password hash must use the format pbkdf2-sha256:<iterations>:<saltBase64>:<hashBase64>.",
                    new[] { nameof(PasswordHash) });
            }
        }
    }
}
