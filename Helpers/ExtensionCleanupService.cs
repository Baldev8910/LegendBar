using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.Storage;

namespace LegendBar.Helpers
{
    public static class ExtensionCleanupService
    {
        private static string PendingListPath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "pending-removals.json");

        public static List<string> LoadPending()
        {
            try
            {
                if (!File.Exists(PendingListPath)) return new List<string>();
                var json = File.ReadAllText(PendingListPath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void SavePending(List<string> fileNames)
        {
            try
            {
                File.WriteAllText(PendingListPath, JsonSerializer.Serialize(fileNames));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Extensions] Failed to save pending removals: {ex.Message}");
            }
        }

        public static void MarkForRemoval(string fileName)
        {
            var pending = LoadPending();
            if (!pending.Contains(fileName))
                pending.Add(fileName);
            SavePending(pending);
        }

        public static bool IsPendingRemoval(string fileName)
        {
            return LoadPending().Contains(fileName);
        }

        // Call this at startup, BEFORE loading any extensions —
        // files aren't locked yet at this point, so deletion actually works.
        public static void ProcessPendingRemovals()
        {
            var pending = LoadPending();
            if (pending.Count == 0) return;

            var stillFailed = new List<string>();

            foreach (var fileName in pending)
            {
                try
                {
                    string path = Path.Combine(ExtensionLoader.ExtensionsFolderPath, fileName);
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Extensions] Could not remove {fileName} yet: {ex.Message}");
                    stillFailed.Add(fileName);
                }
            }

            SavePending(stillFailed); // clears out whatever succeeded, keeps whatever's still stuck
        }
    }
}