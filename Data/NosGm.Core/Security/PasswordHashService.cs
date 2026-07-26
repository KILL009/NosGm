using System;
using System.Globalization;
using System.Security.Cryptography;

namespace NosGm.Core
{
    public static class PasswordHashService
    {
        private const string Scheme = "nosgm";
        private const string Algorithm = "pbkdf2-sha256";
        private const string Version = "v1";
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int MinimumAcceptedIterations = 10000;
        private const int MaximumAcceptedIterations = 2000000;

        public const int CurrentIterations = 600000;
        public const int MaximumCredentialLength = 1024;

        public static bool IsVersionedHash(string storedPassword)
        {
            return storedPassword?.StartsWith(Scheme + "$", StringComparison.Ordinal) == true;
        }

        public static bool TryHashPassword(string clearPassword, out string encodedHash)
        {
            encodedHash = null;
            if (!IsCredentialValid(clearPassword))
            {
                return false;
            }

            var salt = new byte[SaltSize];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            byte[] hash = Derive(clearPassword, salt, CurrentIterations);
            encodedHash = string.Join(
                "$",
                Scheme,
                Algorithm,
                Version,
                CurrentIterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
            return true;
        }

        public static bool VerifyPassword(
            string storedPassword,
            string clearPassword,
            bool legacyUsesSha512,
            out bool needsUpgrade)
        {
            needsUpgrade = false;
            if (storedPassword == null || !IsCredentialValid(clearPassword))
            {
                return false;
            }

            if (IsVersionedHash(storedPassword))
            {
                return VerifyVersionedHash(storedPassword, clearPassword, out needsUpgrade);
            }

            string legacyCandidate = legacyUsesSha512
                ? CryptographyBase.Sha512(clearPassword)
                : clearPassword;
            bool matches = string.Equals(
                storedPassword,
                legacyCandidate,
                legacyUsesSha512 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            needsUpgrade = matches;
            return matches;
        }

        private static byte[] Derive(string clearPassword, byte[] salt, int iterations)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(
                clearPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(HashSize);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            int difference = left.Length ^ right.Length;
            int maximumLength = Math.Max(left.Length, right.Length);
            for (int i = 0; i < maximumLength; i++)
            {
                byte leftValue = i < left.Length ? left[i] : (byte)0;
                byte rightValue = i < right.Length ? right[i] : (byte)0;
                difference |= leftValue ^ rightValue;
            }

            return difference == 0;
        }

        private static bool IsCredentialValid(string clearPassword)
        {
            return clearPassword != null && clearPassword.Length <= MaximumCredentialLength;
        }

        private static bool VerifyVersionedHash(
            string storedPassword,
            string clearPassword,
            out bool needsUpgrade)
        {
            needsUpgrade = false;
            string[] parts = storedPassword.Split('$');
            if (parts.Length != 6 ||
                !string.Equals(parts[0], Scheme, StringComparison.Ordinal) ||
                !string.Equals(parts[1], Algorithm, StringComparison.Ordinal) ||
                !string.Equals(parts[2], Version, StringComparison.Ordinal) ||
                !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out int iterations) ||
                iterations < MinimumAcceptedIterations ||
                iterations > MaximumAcceptedIterations)
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[4]);
                expectedHash = Convert.FromBase64String(parts[5]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (salt.Length != SaltSize || expectedHash.Length != HashSize)
            {
                return false;
            }

            byte[] actualHash = Derive(clearPassword, salt, iterations);
            bool matches = FixedTimeEquals(actualHash, expectedHash);
            needsUpgrade = matches && iterations < CurrentIterations;
            return matches;
        }
    }
}
