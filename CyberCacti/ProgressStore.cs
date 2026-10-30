
using System;
using System.IO;
using System.Text.Json;

namespace CyberCacti
{
    public static class ProgressStore
    {
        private static readonly string Folder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "CyberCacti");

        private static readonly string FilePath =
            Path.Combine(Folder, "progress.json");

        public static bool[] Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    bool[]? saved = JsonSerializer.Deserialize<bool[]>(json);

                    if (saved != null && saved.Length == 5)
                        return saved;
                }
            }
            catch
            {
                // Start with an empty checklist if saved data cannot be read.
            }

            return new bool[5];
        }

        public static void Save(bool[] progress)
        {
            try
            {
                Directory.CreateDirectory(Folder);

                string json = JsonSerializer.Serialize(progress);
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // The app can continue even if progress cannot be saved.
            }
        }

        public static void Reset()
        {
            Save(new bool[5]);
        }
    }
}
