using System.Security.Cryptography;

namespace EatMyMoviesSite.Services
{
    internal static class AdminPasswordHasher
    {
        private const string Algorithm = "pbkdf2-sha256";
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int DefaultIterations = 100_000;

        public static string HashPassword(string password, int iterations = DefaultIterations)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be blank.", nameof(password));
            }

            if (iterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be positive.");
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return $"{Algorithm}:{iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || !TryParse(passwordHash, out var iterations, out var salt, out var expectedHash))
            {
                return false;
            }

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        public static bool IsSupportedHash(string passwordHash)
        {
            return TryParse(passwordHash, out _, out _, out _);
        }

        private static bool TryParse(
            string passwordHash,
            out int iterations,
            out byte[] salt,
            out byte[] expectedHash)
        {
            iterations = 0;
            salt = Array.Empty<byte>();
            expectedHash = Array.Empty<byte>();

            var parts = passwordHash?.Split(':');
            if (parts?.Length != 4 || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out iterations) || iterations <= 0)
            {
                return false;
            }

            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            return salt.Length > 0 && expectedHash.Length > 0;
        }
    }
}
