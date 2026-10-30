using System.IO;
using System.Text.Json;

namespace CyberCacti.Services;

public static class ProgressService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CyberCacti");

    private static string FilePath => Path.Combine(Folder, "progress.json");

    public static void Load(AppSession session)
    {
        try
        {
            Directory.CreateDirectory(Folder);

            if (!File.Exists(FilePath))
                return;

            var data = JsonSerializer.Deserialize<SavedProgress>(File.ReadAllText(FilePath));
            if (data == null)
                return;

            session.Tasks = data.Tasks is { Length: 5 } ? data.Tasks : new bool[5];
            session.HighScore = Math.Max(0, data.HighScore);
        }
        catch
        {
            session.Tasks = new bool[5];
            session.HighScore = 0;
        }
    }

    public static bool Save(AppSession session)
    {
        try
        {
            Directory.CreateDirectory(Folder);

            File.WriteAllText(
                FilePath,
                JsonSerializer.Serialize(
                    new SavedProgress
                    {
                        Tasks = session.Tasks,
                        HighScore = session.HighScore
                    },
                    JsonOptions));

            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class SavedProgress
    {
        public bool[] Tasks { get; set; } = new bool[5];
        public int HighScore { get; set; }
    }
}
