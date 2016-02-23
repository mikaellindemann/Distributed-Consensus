using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Server.Storage
{
    /// <summary>
    /// PasswordHasher provided mechanisms for verifying and hashing passwords.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltValueSize = 4;
        private const int SaltLength = SaltValueSize * UnicodeEncoding.CharSize;
        private static readonly UnicodeEncoding Unicode = new UnicodeEncoding();
        private static readonly HashAlgorithm Hash = new SHA512Managed();
        
        /// <summary>
        /// Generates a salt.
        /// </summary>
        /// <returns></returns>
        public static string GenerateSaltValue()
        {
            // Create a random number object seeded from the value
            // of the last random seed value. This is done
            // interlocked because it is a static value and we want
            // it to roll forward safely.
            var random = new Random(unchecked((int)DateTime.Now.Ticks));

            // Create an array of random values.
            var saltValue = new byte[SaltValueSize];

            random.NextBytes(saltValue);

            // Convert the salt value to a string. Note that the resulting string
            // will still be an array of binary values and not a printable string. 
            // Also it does not convert each byte to a double byte.
            var salt = new StringBuilder();

            foreach (var hexdigit in saltValue)
            {
                salt.Append(hexdigit.ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
            }

            // Return the salt value as a string.
            return salt.ToString();
        }

        /// <summary>
        /// Hashes a password 
        /// </summary>
        /// <param name="clearData">String, that should be hashed</param>
        /// <param name="saltValue">Saltvalue. If none is provided, a salt will be generated.</param>
        /// <returns>Hash based on the string and salt</returns>
        public static string HashPassword(string clearData, string saltValue = null)
        {
            if (clearData == null || Hash == null) return null;
            // If the salt string is null or the length is invalid then
            // create a new valid salt value.

            if (saltValue == null)
            {
                // Generate a salt string.
                saltValue = GenerateSaltValue();
            }

            // Convert the salt string and the password string to a single
            // array of bytes. Note that the password string is Unicode and
            // therefore may or may not have a zero in every other byte.

            // var binarySaltValue = new byte[SaltValueSize];

            var binarySaltValue = Unicode.GetBytes(saltValue);

            /*for (var i = 0; i < SaltValueSize; i++)
            {
                binarySaltValue[i] = byte.Parse(saltValue.Substring(i*2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture.NumberFormat);
            }*/

            //var valueToHash = new byte[SaltValueSize + Unicode.GetByteCount(clearData)];
            var binaryPassword = Unicode.GetBytes(clearData);

            // Copy the salt value and the password to the hash buffer.

            var valueToHash = binarySaltValue.Concat(binaryPassword).ToArray();

            var hashValue = Hash.ComputeHash(valueToHash);

            // The hashed password is the salt plus the hash value (as a string).

            var hashedPassword = new StringBuilder(saltValue);

            foreach (var hexdigit in hashValue)
            {
                hashedPassword.Append(hexdigit.ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
            }

            // Return the hashed password as a string.

            return hashedPassword.ToString();
        }

        /// <summary>
        /// Will verify a password, given a claimed password and an actual password. 
        /// </summary>
        /// <param name="password">Claimed password</param>
        /// <param name="profilePassword">Actual password, that claimed password should be tested against</param>
        /// <returns></returns>
        public static bool VerifyHashedPassword(string password, string profilePassword)
        {
            if (string.IsNullOrEmpty(profilePassword) ||
                string.IsNullOrEmpty(password) ||
                profilePassword.Length < SaltLength)
            {
                return false;
            }

            // Strip the salt value off the front of the stored password.
            var saltValue = profilePassword.Substring(0, SaltLength);

            var hashedPassword = HashPassword(password, saltValue);
            
            // If the hashedPassword matches the profilePassword return true.
            // Otherwise the password could not be verified..
            return profilePassword.Equals(hashedPassword, StringComparison.Ordinal);
        }
    }
}