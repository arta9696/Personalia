namespace Personalia.Localization;

/// <summary>
/// General-purpose grammatical context/>.
///
/// Wraps one or more grammatical tags (e.g. <c>"Male"</c>, <c>"Singular"</c>)
/// that localization providers use to look up correctly inflected string variants.
/// Tags are joined with <c>'.'</c> to form a compound lookup suffix
/// (e.g. <c>"Male.Singular"</c>), allowing providers to support arbitrary
/// combinations of grammatical categories with a single dictionary.
///
/// Instances are created exclusively by <see cref="ILocalizationProvider"/> factory
/// methods, so callers never depend on this concrete type directly.
///
/// English implementations safely ignore this type; Slavic-language implementations
/// use <see cref="Context"/> as a key suffix.
/// </summary>
public sealed record LocalizationContext
{
    private readonly string[] _context;

    /// <summary>
    /// Initialises the context with one or more grammatical tags that together
    /// identify the inflected variant required.
    /// </summary>
    /// <param name="context">
    /// One or more tags that together identify the grammatical variant required,
    /// for example <c>"Female"</c> or <c>"Singular"</c>. Multiple tags are
    /// joined by <c>'.'</c> in <see cref="Context"/>.
    /// </param>
    public LocalizationContext(params string[] context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the compound context string used as a dictionary key suffix when
    /// looking up inflected variants, for example <c>"Male"</c> or
    /// <c>"Male.Singular"</c>.
    /// </summary>
    public string Context => string.Join('.', _context);
}