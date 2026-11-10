using System.Globalization;
using System.Resources;

namespace DuckbillRelic.Plugin.Localization;

public enum UiLanguage { Korean, English }

internal static class UiText
{
    private static readonly ResourceManager Resources = new("DuckbillRelic.Plugin.Localization.Strings", typeof(UiText).Assembly);
    private static CultureInfo culture = CultureInfo.GetCultureInfo("ko");

    // Never change the host game's CurrentCulture or CurrentUICulture.
    public static CultureInfo Culture => Volatile.Read(ref culture);

    /// <summary>Korean on the KR client or a Korean Dalamud UI, English otherwise.
    /// The Korean ClientLanguage value exists only in KR Dalamud builds, so compare the runtime enum name.</summary>
    public static UiLanguage DefaultFor(Dalamud.Game.ClientLanguage client, string? dalamudUiLanguage) =>
        client.ToString() == "Korean" || string.Equals(dalamudUiLanguage, "ko", StringComparison.OrdinalIgnoreCase)
            ? UiLanguage.Korean : UiLanguage.English;

    public static void SetLanguage(UiLanguage language) =>
        Volatile.Write(ref culture, CultureInfo.GetCultureInfo(language == UiLanguage.Korean ? "ko" : "en"));

    public static string Get(TextKey key, params object[] arguments)
    {
        var template = Resources.GetString(key.ToString(), Culture)
            ?? Resources.GetString(key.ToString(), CultureInfo.InvariantCulture) ?? key.ToString();
        return arguments.Length == 0 ? template : string.Format(Culture, template, arguments);
    }

    /// <summary>Translated label with a fixed ImGui ID, so changing the language keeps widget state.</summary>
    public static string Label(TextKey key, string id) => $"{Get(key)}###{id}";

    /// <summary>Stage label for a series id and 1-based stage, e.g. StageZodiac3.</summary>
    public static string Stage(string seriesId, int stage) =>
        Enum.TryParse<TextKey>($"Stage{char.ToUpperInvariant(seriesId[0])}{seriesId[1..]}{stage}", out var key) ? Get(key) : stage.ToString(CultureInfo.InvariantCulture);
}
