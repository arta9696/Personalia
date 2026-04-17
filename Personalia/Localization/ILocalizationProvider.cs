namespace Personalia.Localization;

/// <summary>
/// ILocalizationProvider — contract for locale-aware string resolution.
///
/// Implementations supply translations for UI labels, SmartEnum display names,
/// and character-description templates. The default implementation is
/// <see cref="En.EnglishLocalizationProvider"/>; a Russian alternative is
/// <see cref="Ru.RussianLocalizationProvider"/>.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>Returns the localised string for the given key.</summary>
    string Get(string key);

    /// <summary>
    /// Formats the localised template stored under <paramref name="key"/>
    /// with the supplied <paramref name="args"/> using <see cref="string.Format"/>.
    /// </summary>
    string Format(string key, params object?[] args);

    /// <summary>
    /// Returns the localised display name for a SmartEnum value.
    /// Key convention: <c>"{TypeSimpleName}.{ValueName}"</c>
    /// (e.g. <c>"HairColor.DarkBrown"</c>, <c>"Month.January"</c>).
    /// </summary>
    string GetEnumValue(string typeName, string valueName);
}