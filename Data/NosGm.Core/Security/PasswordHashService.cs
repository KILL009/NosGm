using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NosGm.Core
{
    public static class PasswordHashService
    {
        private const string Scheme = "nosgm";
        private const string Algorithm = "pbkdf2-sha256";
        private const string Version = "v1";
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Sha512HexLength = 128;
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
                ? ComputeSha512Hex(clearPassword)
                : clearPassword;
            bool matches = string.Equals(
                storedPassword,
                legacyCandidate,
                legacyUsesSha512 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            needsUpgrade = matches;
            return matches;
        }

        public static bool VerifyLoginPayload(
            string storedPassword,
            string packetPassword,
            bool useOldCrypto,
            bool loginUsesPrehashedSha512,
            out string clearPassword,
            out bool needsUpgrade)
        {
            clearPassword = null;
            needsUpgrade = false;
            if (storedPassword == null || string.IsNullOrWhiteSpace(packetPassword) ||
                packetPassword.Length > MaximumCredentialLength)
            {
                return false;
            }

            if (useOldCrypto)
            {
                if (LooksLikeLegacyPasswordPayload(packetPassword) &&
                    TryDecodeLegacyPassword(packetPassword, out string decodedPassword) &&
                    TryVerifyCandidate(
                        storedPassword,
                        decodedPassword,
                        true,
                        out clearPassword,
                        out needsUpgrade))
                {
                    return true;
                }

                return TryVerifyCandidate(
                    storedPassword,
                    packetPassword,
                    true,
                    out clearPassword,
                    out needsUpgrade);
            }

            // The prehashed route is enabled only by explicit deployment configuration.
            // It is never available to the legacy decoder path, where a stored digest must
            // not become a reusable login credential.
            if (loginUsesPrehashedSha512)
            {
                return TryVerifyPrehashedSha512(storedPassword, packetPassword);
            }

            return TryVerifyCandidate(
                storedPassword,
                packetPassword,
                false,
                out clearPassword,
                out needsUpgrade);
        }

        private static string ComputeSha512Hex(string clearPassword)
        {
            using (SHA512 hash = SHA512.Create())
            {
                byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(clearPassword));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte value in bytes)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
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

        private static bool FixedTimeHexEquals(string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            int difference = left.Length ^ right.Length;
            int maximumLength = Math.Max(left.Length, right.Length);
            for (int i = 0; i < maximumLength; i++)
            {
                int leftValue = i < left.Length ? GetHexValue(left[i]) : 0;
                int rightValue = i < right.Length ? GetHexValue(right[i]) : 0;
                difference |= leftValue ^ rightValue;
            }

            return difference == 0;
        }

        private static int GetHexValue(char value)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }

            if (value >= 'a' && value <= 'f')
            {
                return value - 'a' + 10;
            }

            if (value >= 'A' && value <= 'F')
            {
                return value - 'A' + 10;
            }

            return -1;
        }

        private static bool IsCredentialValid(string clearPassword)
        {
            return clearPassword != null && clearPassword.Length <= MaximumCredentialLength;
        }

        private static bool IsHexDigit(char value)
        {
            return value >= '0' && value <= '9' ||
                   value >= 'a' && value <= 'f' ||
                   value >= 'A' && value <= 'F';
        }

        private static bool IsSha512Hex(string value)
        {
            if (value == null || value.Length != Sha512HexLength)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (!IsHexDigit(value[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LooksLikeLegacyPasswordPayload(string packetPassword)
        {
            if (string.IsNullOrEmpty(packetPassword))
            {
                return false;
            }

            int startIndex = packetPassword.Length % 2 == 0 ? 3 : 4;
            int selectedCount = CountSelectedCharacters(packetPassword.Length, startIndex);
            if (selectedCount % 2 != 0)
            {
                startIndex = 2;
                selectedCount = CountSelectedCharacters(packetPassword.Length, startIndex);
            }

            if (selectedCount == 0 || selectedCount % 2 != 0)
            {
                return false;
            }

            for (int i = startIndex; i < packetPassword.Length; i += 2)
            {
                if (!IsHexDigit(packetPassword[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountSelectedCharacters(int length, int startIndex)
        {
            return startIndex >= length ? 0 : (length - startIndex + 1) / 2;
        }

        private static bool TryDecodeLegacyPassword(string packetPassword, out string clearPassword)
        {
            clearPassword = null;
            try
            {
                clearPassword = LoginCryptography.GetPassword(packetPassword);
                return !string.IsNullOrEmpty(clearPassword) &&
                       clearPassword.Length <= MaximumCredentialLength;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryVerifyCandidate(
            string storedPassword,
            string candidate,
            bool legacyUsesSha512,
            out string clearPassword,
            out bool needsUpgrade)
        {
            clearPassword = null;
            needsUpgrade = false;
            if (string.IsNullOrEmpty(candidate) || candidate.Length > MaximumCredentialLength ||
                !VerifyPassword(storedPassword, candidate, legacyUsesSha512, out bool candidateNeedsUpgrade))
            {
                return false;
            }

            clearPassword = candidate;
            needsUpgrade = candidateNeedsUpgrade;
            return true;
        }

        private static bool TryVerifyPrehashedSha512(string storedPassword, string packetPassword)
        {
            return IsSha512Hex(storedPassword) &&
                   IsSha512Hex(packetPassword) &&
                   FixedTimeHexEquals(storedPassword, packetPassword);
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