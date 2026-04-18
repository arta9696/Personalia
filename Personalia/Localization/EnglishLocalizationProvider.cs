using System.Text.RegularExpressions;

namespace Personalia.Localization.En;

/// <summary>
/// EnglishLocalizationProvider — the default locale for <c>CharacterDescriber</c> output.
///
/// SmartEnum value display names are derived automatically from PascalCase value names
/// via regex splitting, so no explicit mapping is required when new enum values are added.
/// Some names are exception: they are values that must retain proper
/// capitalisation or punctuation in running prose, so they are listed explicitly.
/// </summary>
public sealed partial class EnglishLocalizationProvider : ILocalizationProvider
{
    private static readonly IReadOnlyDictionary<string, string> _strings =
        new Dictionary<string, string>
        {
            // ── Describer ─────────────────────────────────────────────────────
            [Lk.Describer.Header]
                = "Your name is {0} {1}. You are {2} years old. Your birthday is {3} {4}.",
            [Lk.Describer.SectionAppearance] = "Appearance",
            [Lk.Describer.AppearanceLine1]
                = "You are {0} {1}. You are {2}. You have {3} height and your build would best be described as {4}. ",
            [Lk.Describer.AppearanceLine2]
                = "You have {0} skin, {1} {2} eyes and your {3} hair is {4}.",
            [Lk.Describer.DistinctiveFeature] = "Your most distinguishing feature is {0}.",
            [Lk.Describer.DistinctiveFeatures] = "Your most distinguishing features are {0}.",
            [Lk.Describer.SectionClothing] = "Clothing",
            [Lk.Describer.ClothingBase] = "You are wearing {0}, {1} and {2}.",
            [Lk.Describer.ClothingAccessories] = " You also have {0}.",
            [Lk.Describer.SectionStatus] = "Status",
            [Lk.Describer.FamilyPrefix] = "Your family: ",
            [Lk.Describer.AcquaintancePrefix] = "Your acquaintances: ",
            [Lk.Describer.PartnerPrefix] = "Your partners: ",
            [Lk.Describer.None] = "None",
            [Lk.Describer.FamilyEntry] = "{0} named {1} (age {2}, {3})",
            [Lk.Describer.AcquaintanceEntry] = "{0} (age {1})",
            [Lk.Describer.PartnerEntry] = "{0} ({1}, age {2})",
            [Lk.Describer.Alive] = "alive",
            [Lk.Describer.Deceased] = "deceased",
            [Lk.Describer.Unemployed] = "You're currently unemployed.",
            [Lk.Describer.Retired] = "You're currently retired.",
            [Lk.Describer.Working] = "You're currently working at {0}.",

            // ── Height ────────────────────────────────────────────────────────
            [Lk.Height.VeryShort] = "very short",
            [Lk.Height.Short] = "short",
            [Lk.Height.SlightlyShort] = "slightly short",
            [Lk.Height.Average] = "average",
            [Lk.Height.SlightlyTall] = "slightly tall",
            [Lk.Height.Tall] = "tall",
            [Lk.Height.VeryTall] = "very tall",

            // ── Build ─────────────────────────────────────────────────────────
            [Lk.Build.Skinny] = "skinny",
            [Lk.Build.Thin] = "thin",
            [Lk.Build.Plump] = "plump",
            [Lk.Build.Lean] = "lean",
            [Lk.Build.Stocky] = "stocky",
            [Lk.Build.Ripped] = "ripped",
            [Lk.Build.Muscular] = "muscular",
            [Lk.Build.Brawny] = "brawny",
            [Lk.Build.Average] = "average build",
        };

    // Some names must retain proper capitalisation or punctuation in running prose.
    // Single-word PascalCase values are fully split and lower-cased by the fallback splitter,
    // so this are listed here explicitly as an override.
    private static readonly IReadOnlyDictionary<string, string> _enumOverrides =
        new Dictionary<string, string>
        {
            ["Month.January"] = "January",
            ["Month.February"] = "February",
            ["Month.March"] = "March",
            ["Month.April"] = "April",
            ["Month.May"] = "May",
            ["Month.June"] = "June",
            ["Month.July"] = "July",
            ["Month.August"] = "August",
            ["Month.September"] = "September",
            ["Month.October"] = "October",
            ["Month.November"] = "November",
            ["Month.December"] = "December",

            ["AgeCategory.MiddleAged"] = "middle-aged",
            ["AgeCategory.Senior"] = "elderly",
            ["SexualOrientation.Heterosexual"] = "straight",
            ["SexualOrientation.Homosexual"] = "gay",
        };

    // ── ILocalizationProvider ─────────────────────────────────────────────────

    public string Get(string key)
        => _strings.TryGetValue(key, out var val) ? val : key;

    public string Format(string key, params object?[] args)
        => string.Format(Get(key), args);

    /// <summary>
    /// Converts a PascalCase SmartEnum value name to a lowercase spaced phrase.
    /// Examples: "DarkBrown" → "dark brown", "CloseCropped" → "close cropped".
    /// Month names are returned with proper capitalisation from the override table.
    /// No explicit mapping is needed when new enum values are added.
    /// </summary>
    public string GetEnumValue(string typeName, string valueName) 
        => _enumOverrides.TryGetValue($"{typeName}.{valueName}", out var overrideVal) 
        ? overrideVal 
        : CamelCaseSplitter().Replace(valueName, " ").ToLower();

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelCaseSplitter();
}