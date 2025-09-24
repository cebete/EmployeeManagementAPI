using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace EmployeeManagementAPI.Handlers
{
    public class PasswordHashHandler
    {
        private static int _iterationCount = 100000;
        private static RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();

        public static string HashPassword(string password)
        {
            // salt size in bytes
            int saltSize = 128 / 8;
            var salt = new byte[saltSize];
            _randomNumberGenerator.GetBytes(salt);

            // derive a 256-bit subkey (32 bytes) using HMACSHA512
            var subkey = KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: _iterationCount,
                numBytesRequested: 256 / 8);

            // format: 1 byte marker | 4 bytes prf | 4 bytes iterCount | 4 bytes saltSize | salt | subkey
            var outputBytes = new byte[13 + salt.Length + subkey.Length];
            outputBytes[0] = 0x01; // format marker
            WriteNetworkByteOrder(outputBytes, 1, (uint)KeyDerivationPrf.HMACSHA512);
            WriteNetworkByteOrder(outputBytes, 5, (uint)_iterationCount);
            WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);

            return Convert.ToBase64String(outputBytes);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (hashedPassword == null) throw new ArgumentNullException(nameof(hashedPassword));

            byte[] decoded;
            try
            {
                decoded = Convert.FromBase64String(hashedPassword);
            }
            catch (FormatException)
            {
                // not a valid base64 string
                return false;
            }

            // Must be at least header size (1 + 4 + 4 + 4 = 13) + salt + subkey (subkey at least 1)
            if (decoded.Length < 13)
                return false;

            // check format marker
            if (decoded[0] != 0x01)
                return false;

            var prf = (KeyDerivationPrf)ReadNetworkByteOrder(decoded, 1);
            var iterCount = (int)ReadNetworkByteOrder(decoded, 5);
            var saltSize = (int)ReadNetworkByteOrder(decoded, 9);

            if (saltSize < 0 || saltSize > decoded.Length - 13)
                return false;

            // compute positions
            int saltStart = 13;
            if (decoded.Length < saltStart + saltSize)
                return false;

            var salt = new byte[saltSize];
            Buffer.BlockCopy(decoded, saltStart, salt, 0, saltSize);

            int subkeyStart = saltStart + saltSize;
            var subkeyLength = decoded.Length - subkeyStart;
            if (subkeyLength <= 0)
                return false;

            var expectedSubkey = new byte[subkeyLength];
            Buffer.BlockCopy(decoded, subkeyStart, expectedSubkey, 0, subkeyLength);

            // derive the subkey for the provided password using the same params
            byte[] actualSubkey;
            try
            {
                actualSubkey = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: prf,
                    iterationCount: iterCount,
                    numBytesRequested: subkeyLength);
            }
            catch
            {
                // if any parameter is invalid (shouldn't happen), fail verification
                return false;
            }

            return ByteArraysEqual(actualSubkey, expectedSubkey);
        }

        // helper: write uint in big-endian into buffer at offset
        private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)(value);
        }

        // helper: read uint in big-endian from buffer at offset
        private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset + 0] << 24)
                 | ((uint)buffer[offset + 1] << 16)
                 | ((uint)buffer[offset + 2] << 8)
                 | buffer[offset + 3];
        }

        // constant-time comparison to avoid timing attacks
        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            uint diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }

            return diff == 0;
        }
    }
}
