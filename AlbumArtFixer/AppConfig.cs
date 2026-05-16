using System.Text.Json;

namespace AlbumArtFixer;

record AppConfig(string[] SupportedExtensions)
{
    public static readonly AppConfig Default = new([".mp3", ".mp4", ".m4a"]);

    public static AppConfig Load(string path = "appsettings.json")
    {
        if (!File.Exists(path)) return Default;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("SupportedExtensions", out var prop))
            {
                var exts = prop.EnumerateArray()
                    .Select(e => e.GetString())
                    .OfType<string>()
                    .ToArray();
                return new AppConfig(exts.Length > 0 ? exts : Default.SupportedExtensions);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: could not read {path}: {ex.Message} — using defaults");
        }

        return Default;
    }
}
