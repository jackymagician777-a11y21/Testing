using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using SteamAutoLauncher.Core.Logging;

namespace SteamAutoLauncher.Core.SteamGuard
{
    public class MaFileData
    {
        public string? shared_secret { get; set; }
        public string? account_name { get; set; }
    }

    public static class MaFileParser
    {
        /// <summary>
        /// Extracts shared_secret from MaFile using Python helper script
        /// Falls back to direct JSON parsing if Python is not available
        /// </summary>
        public static string? ExtractSharedSecret(string maFilePath)
        {
            Logger.LogInfo($"[MaFileParser] Attempting to read MaFile from: {maFilePath}");

            if (!File.Exists(maFilePath))
            {
                Logger.LogError($"[MaFileParser] File does not exist: {maFilePath}");
                throw new FileNotFoundException($"MaFile not found: {maFilePath}");
            }

            // Try Python first (more reliable for complex JSON)
            try
            {
                var pythonSecret = ExtractSharedSecretViaPython(maFilePath);
                if (!string.IsNullOrEmpty(pythonSecret))
                {
                    Logger.LogSuccess("[MaFileParser] Successfully extracted shared_secret via Python");
                    return pythonSecret;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[MaFileParser] Python extraction failed, trying direct parsing: {ex.Message}");
            }

            // Fallback to regex-based extraction
            var regexSecret = ExtractSharedSecretViaRegex(maFilePath);
            if (!string.IsNullOrEmpty(regexSecret))
            {
                return regexSecret;
            }

            // Last resort: throw error
            throw new InvalidOperationException($"Failed to extract shared_secret from MaFile: {maFilePath}");
        }

        /// <summary>
        /// Uses Python script to extract shared_secret
        /// </summary>
        private static string? ExtractSharedSecretViaPython(string maFilePath)
        {
            try
            {
                Logger.LogInfo("[MaFileParser] Using Python helper to extract shared_secret...");

                // Get the directory where the executable is running
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var exeDir = Path.GetDirectoryName(exePath);
                var pythonScriptPath = Path.Combine(exeDir ?? "", "extract_shared_secret.py");

                Logger.LogInfo($"[MaFileParser] Python script path: {pythonScriptPath}");

                if (!File.Exists(pythonScriptPath))
                {
                    Logger.LogWarning($"[MaFileParser] Python script not found at: {pythonScriptPath}");
                    return null;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{pythonScriptPath}\" \"{maFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        Logger.LogWarning("[MaFileParser] Failed to start Python process");
                        return null;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    process.WaitForExit(5000);

                    Logger.LogInfo($"[MaFileParser] Python stdout: {output.Trim()}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Logger.LogInfo($"[MaFileParser] Python stderr: {error.Trim()}");
                    }

                    // Parse output: "SUCCESS:shared_secret_value" or "ERROR:message"
                    if (output.StartsWith("SUCCESS:"))
                    {
                        var secret = output.Substring("SUCCESS:".Length).Trim();
                        Logger.LogSuccess($"[MaFileParser] Python extracted shared_secret: {secret.Substring(0, Math.Min(10, secret.Length))}...");
                        return secret;
                    }
                    else if (output.StartsWith("ERROR:"))
                    {
                        var errorMsg = output.Substring("ERROR:".Length).Trim();
                        Logger.LogWarning($"[MaFileParser] Python error: {errorMsg}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[MaFileParser] Python extraction exception: {ex.GetType().Name} - {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Fallback: Extracts shared_secret using regex pattern matching
        /// More tolerant of malformed JSON
        /// </summary>
        private static string? ExtractSharedSecretViaRegex(string maFilePath)
        {
            try
            {
                Logger.LogInfo("[MaFileParser] Using regex-based extraction (fallback)...");

                var fileContent = File.ReadAllText(maFilePath);
                Logger.LogInfo($"[MaFileParser] File size: {fileContent.Length} characters");

                // Pattern: "shared_secret":"<value>"
                var regex = new Regex(@"""shared_secret""\s*:\s*""([^""]*)""", RegexOptions.IgnoreCase);
                var match = regex.Match(fileContent);

                if (match.Success)
                {
                    var secret = match.Groups[1].Value.Trim();
                    Logger.LogSuccess($"[MaFileParser] Regex extracted shared_secret: {secret.Substring(0, Math.Min(10, secret.Length))}...");
                    return secret;
                }

                Logger.LogError("[MaFileParser] Regex failed to find shared_secret pattern");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MaFileParser] Regex extraction error: {ex.Message}");
                return null;
            }
        }
    }
}
