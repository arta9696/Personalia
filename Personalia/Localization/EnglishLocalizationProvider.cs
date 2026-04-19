using Personalia.Common;
using System.Text.RegularExpressions;

namespace Personalia.Localization.En;

/// <summary>
/// EnglishLocalizationProvider — the default locale for <c>CharacterDescriber</c> output.
///
/// SmartEnum value display names are derived automatically from PascalCase value names
/// via regex splitting, so no explicit mapping is required when new enum values are added.
/// Some names are exceptions: they are values that must retain proper
/// capitalisation or punctuation in running prose, so they are listed explicitly.
///
/// Grammatical context (<see cref="LocalizationContext"/>) is accepted by all methods
/// but intentionally ignored — English adjectives and nouns do not inflect for gender
/// or number in the patterns this provider handles.
/// </summary>
public sealed partial class EnglishLocalizationProvider : ILocalizationProvider
{
    private static readonly IReadOnlyDictionary<string, string> _strings =
        new Dictionary<string, string>
        {
            // ── Describer ─────────────────────────────────────────────────────
            ["Describer.Header"]
                = "Your name is {0} {1}. You are {2} years old. Your birthday is {3} {4}.",
            ["Describer.SectionAppearance"] = "Appearance",
            ["Describer.AppearanceLine1"]
                = "You are {0} {1}. You are {2}. You have {3} height and your build would best be described as {4}. ",
            ["Describer.AppearanceLine2"]
                = "You have {0} skin, {1} {2} eyes and your {3} hair is {4}.",
            ["Describer.DistinctiveFeature"] = "Your most distinguishing feature is {0}.",
            ["Describer.DistinctiveFeatures"] = "Your most distinguishing features are {0}.",
            ["Describer.SectionClothing"] = "Clothing",
            ["Describer.ClothingBase"] = "You are wearing {0}, {1} and {2}.",
            ["Describer.ClothingAccessories"] = " You also have {0}.",
            ["Describer.SectionStatus"] = "Status",
            ["Describer.FamilyPrefix"] = "Your family: ",
            ["Describer.AcquaintancePrefix"] = "Your acquaintances: ",
            ["Describer.PartnerPrefix"] = "Your partners: ",
            ["Describer.None"] = "None",
            ["Describer.FamilyEntry"] = "{0} named {1} (age {2}, {3})",
            ["Describer.AcquaintanceEntry"] = "{0} (age {1})",
            ["Describer.PartnerEntry"] = "{0} ({1}, age {2})",
            ["Describer.Alive"] = "alive",
            ["Describer.Deceased"] = "deceased",
            ["Describer.Unemployed"] = "You're currently unemployed.",
            ["Describer.Retired"] = "You're currently retired.",
            ["Describer.Working"] = "You're currently working at {0}.",

            // ── Height ────────────────────────────────────────────────────────
            ["Height.VeryShort"] = "very short",
            ["Height.Short"] = "short",
            ["Height.SlightlyShort"] = "slightly short",
            ["Height.Average"] = "average",
            ["Height.SlightlyTall"] = "slightly tall",
            ["Height.Tall"] = "tall",
            ["Height.VeryTall"] = "very tall",

            // ── Build ─────────────────────────────────────────────────────────
            ["Build.Skinny"] = "skinny",
            ["Build.Thin"] = "thin",
            ["Build.Plump"] = "plump",
            ["Build.Lean"] = "lean",
            ["Build.Stocky"] = "stocky",
            ["Build.Ripped"] = "ripped",
            ["Build.Muscular"] = "muscular",
            ["Build.Brawny"] = "brawny",
            ["Build.Average"] = "average build",
        };

    /// <summary>
    /// Some names must retain proper capitalisation or punctuation in running prose.
    /// Single-word PascalCase values are fully split and lower-cased by the fallback splitter,
    /// so these are listed here explicitly as overrides.
    /// </summary>
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

    /// <inheritdoc/>
    /// <remarks>Context is accepted for interface compliance but not used — English
    /// adjectives do not inflect for grammatical gender in these patterns.</remarks>
    public string Get(string key, LocalizationContext? context = null)
        => _strings.TryGetValue(key, out var val) ? val : key;

    /// <inheritdoc/>
    public string Format(string key, LocalizationContext? context, params string[] args)
        => string.Format(Get(key, context), args);

    /// <inheritdoc/>
    public string Format(string key, params string[] args)
        => string.Format(Get(key), args);

    /// <summary>
    /// Converts a PascalCase SmartEnum value name to a lowercase spaced phrase.
    /// Examples: "DarkBrown" → "dark brown", "CloseCropped" → "close cropped".
    /// Some names are returned with proper capitalisation or punctuation from the override table.
    /// No explicit mapping is needed when new enum values are added.
    /// Context is accepted for interface compliance but not used in English.
    /// </summary>
    public string GetEnumValue(string typeName, string valueName, LocalizationContext? context = null)
        => _enumOverrides.TryGetValue($"{typeName}.{valueName}", out var overrideVal)
            ? overrideVal
            : TextFormatter.CamelCaseSplitter().Replace(valueName, " ").ToLower();

    /// <summary>
    /// TODO
    /// </summary>
    public string GetEnumValue<T>(T value, LocalizationContext? context = null) where T : SmartEnum<T> => GetEnumValue(value.GetType().Name, value.Name, context);
}