using Personalia.Common;

namespace Personalia.Localization;

/// <summary>
/// ILocalizationProvider — contract for locale-aware string resolution.
///
/// Implementations supply translations for UI labels, SmartEnum display names,
/// and character-description templates. The default implementation is
/// <see cref="En.EnglishLocalizationProvider"/>; a Russian alternative is
/// <see cref="Ru.RussianLocalizationProvider"/>.
///
/// Where grammatical agreement is required,
/// pass a <see cref="LocalizationContext"/> to <see cref="Get"/> or
/// <see cref="GetEnumValue"/>. English implementations may safely ignore the context.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>
    /// Returns the localised string for the given key.
    /// </summary>
    /// <param name="key">Localisation key (e.g. <c>Lk.Height.Tall</c>).</param>
    /// <param name="context">
    /// Optional grammatical context. Implementations use this to select
    /// correctly inflected variants.
    /// </param>
    string Get(string key, LocalizationContext? context = null);

    /// <summary>
    /// Formats the localised template stored under <paramref name="key"/>
    /// with the supplied <paramref name="args"/> using <see cref="string.Format"/>.
    /// </summary>
    string Format(string key, LocalizationContext? context, params string[] args);

    /// <summary>
    /// TODO
    /// </summary>
    string Format(string key, params string[] args);

    /// <summary>
    /// Returns the localised display name for a SmartEnum value.
    /// Key convention: <c>"{TypeSimpleName}.{ValueName}"</c>
    /// (e.g. <c>"HairColor.DarkBrown"</c>, <c>"Month.January"</c>).
    /// </summary>
    /// <param name="typeName">Simple name of the SmartEnum type (e.g. <c>"HairColor"</c>).</param>
    /// <param name="valueName">Name of the enum value (e.g. <c>"DarkBrown"</c>).</param>
    /// <param name="context">
    /// Optional grammatical context. When provided, implementations may look up a
    /// context-specific variant before falling back to the base key.
    /// </param>
    string GetEnumValue(string typeName, string valueName, LocalizationContext? context = null);

    /// <summary>
    /// TODO
    /// </summary>
    public string GetEnumValue<T>(T value, LocalizationContext? context = null) where T : SmartEnum<T>;
}