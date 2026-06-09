using System;
using System.IO;
using Newtonsoft.Json;

namespace SteamAutoLauncher.Core.SteamGuard
{
    public static class MaFileParser
    {
        public static string? ExtractSharedSecret(string maFilePath)
        {
            if (!File.Exists(maFilePath))
            {
                throw new FileNotFoundException($"MaFile not found: {maFilePath}");
            }

            try
            {
                var json = File.ReadAllText(maFilePath);
                var maFileData = JsonConvert.DeserializeObject<MaFileData>(json);
                
                if (maFileData?.shared_secret == null)
                {
                    throw new InvalidOperationException($"shared_secret not found in MaFile: {maFilePath}");
                }

                return maFileData.shared_secret;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse MaFile JSON: {maFilePath}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract shared_secret from MaFile: {ex.Message}", ex);
            }
        }
    }
}