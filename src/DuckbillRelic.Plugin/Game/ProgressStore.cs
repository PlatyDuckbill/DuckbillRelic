using System.Globalization;
using System.Text.Json;
using Dalamud.Plugin.Services;
using DuckbillRelic.Core.Progress;

namespace DuckbillRelic.Plugin.Game;

/// <summary>One JSON file per character in the plugin config directory. Nothing leaves the PC.</summary>
internal sealed class ProgressStore(string configDirectory, IPluginLog log)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string directory = Path.Combine(configDirectory, "characters");

    private string PathFor(ulong contentId) =>
        Path.Combine(directory, contentId.ToString("X16", CultureInfo.InvariantCulture) + ".json");

    public CharacterProgress Load(ulong contentId)
    {
        var path = PathFor(contentId);
        try
        {
            if (File.Exists(path) && JsonSerializer.Deserialize<CharacterProgress>(File.ReadAllText(path), Options) is { } saved && saved.ContentId == contentId)
            {
                saved.Normalize();
                return saved;
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Keep the unreadable file for the user; start a fresh record instead of guessing.
            log.Warning(e, "Could not read relic progress {Path}; keeping it and starting empty.", path);
            TryBackup(path);
        }
        return new CharacterProgress { ContentId = contentId };
    }

    public void Save(CharacterProgress progress)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var path = PathFor(progress.ContentId);
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(progress, Options));
            File.Move(temp, path, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            log.Warning(e, "Could not save relic progress.");
        }
    }

    private void TryBackup(string path)
    {
        try { File.Copy(path, path + ".unreadable", true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
