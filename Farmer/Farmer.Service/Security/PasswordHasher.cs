using System;
using System.Security.Cryptography;
using System.Text;

namespace Farmer.Service.Security
{
    /// <summary>
    /// Salted SHA-256 password hashing. Format: HEX( SHA256( UTF8(salt + password) ) ), upper-case.
    /// The seed script uses the same formula in T-SQL so demo accounts can log in.
    /// (For a production system prefer PBKDF2/bcrypt/Argon2 - noted as a future improvement.)
    /// </summary>
    public static class PasswordHasher
    {
        public static string GenerateSalt()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return ToHex(bytes);
        }

        public static string Hash(string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
            }
        }

        public static bool Verify(string password, string salt, string expectedHash)
        {
            string actual = Hash(password, salt);
            return string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
