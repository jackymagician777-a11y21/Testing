using System;
using System.Linq;
using System.Security.Cryptography;
using Org.BouncyCastle.Utilities.Encoders;

namespace SteamAutoLauncher.Core.SteamGuard
{
    public static class SteamGuardGenerator
    {
        private const string SteamAlphabet = "23456789BCDFGHJKMNPQRTVWXY";
        private const int CodeLength = 5;
        private const long WindowSeconds = 30;

        public static string GenerateCode(string sharedSecretBase64, long? unixTime = null)
        {
            unixTime ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            try
            {
                // Decode Base64 shared_secret
                var secretBytes = Base64.Decode(sharedSecretBase64);
                
                // Calculate counter
                long counter = unixTime.Value / WindowSeconds;
                var timeBuffer = BitConverter.GetBytes(counter);
                
                // Ensure big-endian format
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(timeBuffer);
                }

                // Generate HMAC-SHA1
                using (var hmac = new HMACSHA1(secretBytes))
                {
                    var hash = hmac.ComputeHash(timeBuffer);
                    
                    // Extract offset
                    int offset = hash[19] & 0x0F;
                    
                    // Get 32-bit value
                    uint value = ((uint)(hash[offset] & 0x7F) << 24) |
                                 ((uint)(hash[offset + 1] & 0xFF) << 16) |
                                 ((uint)(hash[offset + 2] & 0xFF) << 8) |
                                 ((uint)(hash[offset + 3] & 0xFF));

                    // Convert to Steam Guard code
                    var code = "";
                    for (int i = 0; i < CodeLength; i++)
                    {
                        code = SteamAlphabet[(int)(value % (uint)SteamAlphabet.Length)] + code;
                        value /= (uint)SteamAlphabet.Length;
                    }

                    return code;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate Steam Guard code: {ex.Message}", ex);
            }
        }

        public static int GetSecondsRemaining(long? unixTime = null)
        {
            unixTime ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long intoWindow = unixTime.Value % WindowSeconds;
            return (int)(WindowSeconds - intoWindow);
        }

        public static string GenerateCodeNow(string sharedSecretBase64)
        {
            return GenerateCode(sharedSecretBase64, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
    }
}