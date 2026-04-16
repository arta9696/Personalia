namespace Personalia.Models.PersonalitySpace;

/// <summary>
/// The personality of a character.
///
/// In the current planning stage this is an open extension point.
/// Personality objects define how a character behaves and
/// are intended to be composed of personality traits / traits collections
/// in future iterations.
/// </summary>
public sealed class Personality
{
    /// <summary>
    /// Open-ended dictionary of personality traits.
    /// Key: trait name (e.g. "Courage", "Empathy").
    /// Value: trait intensity 0.0 – 1.0.
    /// </summary>
    public IDictionary<string, float> Traits { get; init; }
        = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
}