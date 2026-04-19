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
/// Grammatical agreement is handled through <see cref="LocalizationContext"/>
/// instances created by the provider's own methods.  Callers obtain
/// contexts, keeping them locale-agnostic: English return <c>null</c> because English
/// adjectives do not inflect, while Russian return a populated
/// <see cref="LocalizationContext"/> carrying the relevant grammatical tags.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>
    /// Returns the localised string for the given key.
    /// </summary>
    /// <param name="key">Localisation key (e.g. <c>Height.Tall</c>).</param>
    /// <param name="context">
    /// Optional grammatical context. Implementations use this to select correctly
    /// inflected variants; English implementations ignore it.
    /// </param>
    string Get(string key, LocalizationContext? context = null);

    /// <summary>
    /// Formats the localised template stored under <paramref name="key"/>
    /// with the supplied <paramref name="args"/> using <see cref="string.Format"/>.
    /// </summary>
    /// <param name="key">Localisation key of the format template.</param>
    /// <param name="context">
    /// Optional grammatical context used to resolve the inflected template before
    /// formatting. English implementations ignore it.
    /// </param>
    /// <param name="args">Arguments interpolated into the resolved template.</param>
    string Format(string key, LocalizationContext? context, params string[] args);

    /// <summary>
    /// Formats the localised template stored under <paramref name="key"/>
    /// with the supplied <paramref name="args"/> without a grammatical context.
    /// </summary>
    /// <param name="key">Localisation key of the format template.</param>
    /// <param name="args">Arguments interpolated into the template.</param>
    string Format(string key, params string[] args);

    /// <summary>
    /// Returns the localised display name for a SmartEnum value.
    /// </summary>
    /// <typeparam name="T">The SmartEnum type.</typeparam>
    /// <param name="value">The SmartEnum instance to localise.</param>
    /// <param name="context">
    /// Optional grammatical context. When provided, implementations may look up a
    /// context-specific variant before falling back to the base key.
    /// </param>
    string GetEnumValue<T>(T value, LocalizationContext? context = null) where T : SmartEnum<T>;

    /// <summary>
    /// TODO
    /// </summary>
    LocalizationContext? Context(string contextType, params object[] contextItems);
}