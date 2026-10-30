using System;
using UnityEngine;

/// <summary>The character's chosen name, independent of their combat class.</summary>
public static class PlayerIdentity
{
    public const string NameKey = "PlayerName";
    public const string DefaultName = "Vô danh";

    // Read on demand so loading another save cannot leave a previous name cached.
    public static string Name => NormalizeName(PlayerPrefs.GetString(NameKey, DefaultName));

    public static string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultName : value.Trim();
    }

    public static void SetName(string value)
    {
        PlayerPrefs.SetString(NameKey, NormalizeName(value));
    }

    // Old scenes contain these speaker identifiers in serialized dialogue arrays.
    // Resolve them when displayed, without rewriting the story or NPC names.
    public static string ResolveSpeaker(string scriptedSpeaker)
    {
        if (string.IsNullOrWhiteSpace(scriptedSpeaker)) return scriptedSpeaker;
        string speaker = scriptedSpeaker.Trim();
        int separator = speaker.IndexOf('—');
        if (separator < 0) separator = speaker.IndexOf('–');
        string role = separator < 0 ? speaker : speaker.Substring(0, separator).Trim();
        bool isHero = string.Equals(role, "NHÂN VẬT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "NHÂN VẬT CHÍNH", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "HIỆP SĨ", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "PHÁP SƯ", StringComparison.OrdinalIgnoreCase);
        if (!isHero) return scriptedSpeaker;
        return Name + (separator < 0 ? string.Empty : " " + speaker.Substring(separator));
    }

    public static string Literal(string value)
    {
        // Names are text, never TMP markup. Break possible closing noparse tags
        // with an invisible separator while keeping the visible name unchanged.
        if (string.IsNullOrEmpty(value) || value.IndexOf('<') < 0) return value;
        return "<noparse>" + value.Replace("<", "<\u200B") + "</noparse>";
    }
}
