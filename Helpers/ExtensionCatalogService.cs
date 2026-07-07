using LegendBar.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace LegendBar.Helpers
{
    public static class ExtensionCatalogService
    {
        private const string ManifestUrl =
            "https://raw.githubusercontent.com/Baldev8910/LegendBar/main/extensions/manifest.json";

        private static readonly HttpClient _http = new();

        public static async Task<List<ExtensionCatalogEntry>> FetchCatalogAsync()
        {
            try
            {
                var json = await _http.GetStringAsync(ManifestUrl);
                return JsonSerializer.Deserialize<List<ExtensionCatalogEntry>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<ExtensionCatalogEntry>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Extensions] Failed to fetch catalog: {ex.Message}");
                return new List<ExtensionCatalogEntry>();
            }
        }

        public static bool IsInstalled(ExtensionCatalogEntry entry)
        {
            string path = Path.Combine(ExtensionLoader.ExtensionsFolderPath, entry.FileName);
            return File.Exists(path);
        }

        public static async Task<bool> InstallAsync(ExtensionCatalogEntry entry)
        {
            try
            {
                Directory.CreateDirectory(ExtensionLoader.ExtensionsFolderPath);
                string destPath = Path.Combine(ExtensionLoader.ExtensionsFolderPath, entry.FileName);

                var bytes = await _http.GetByteArrayAsync(entry.DownloadUrl);

                // Verify the file matches what the manifest says it should be
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(bytes);
                    string hashString = Convert.ToHexString(hash).ToLowerInvariant();

                    if (!string.Equals(hashString, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Extensions] Hash mismatch for {entry.Id}. Expected {entry.Sha256}, got {hashString}");
                        return false;
                    }
                }

                await File.WriteAllBytesAsync(destPath, bytes);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Extensions] Install failed for {entry.Id}: {ex.Message}");
                return false;
            }
        }

        public static bool Remove(ExtensionCatalogEntry entry)
        {
            try
            {
                string path = Path.Combine(ExtensionLoader.ExtensionsFolderPath, entry.FileName);
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Extensions] Remove failed for {entry.Id}: {ex.Message}");
                return false;
            }
        }
    }
}