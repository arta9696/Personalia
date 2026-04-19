namespace Personalia.Localization;

/// <summary>
/// LocalizationContext — optional grammatical metadata supplied to
/// <see cref="ILocalizationProvider"/> so that implementations can produce
/// properly inflected output.
///
/// English implementations typically ignore this; Slavic-language implementations
/// use it to select the correct gender/number agreement for adjectives and nouns.
/// </summary>
public sealed record LocalizationContext
{

    private readonly string[] _context;

    /// <summary>
    /// TODO
    /// </summary>
    public LocalizationContext(params string[] context)
    {
        _context = context;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public string Context => string.Join('.', _context);
}