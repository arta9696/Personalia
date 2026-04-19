using Personalia.Models.AppearanceSpace.BodyParts.Body;
using Personalia.Models.AppearanceSpace.BodyParts.HeadPart;
using Personalia.Models.AppearanceSpace.BodyParts.Limbs;

namespace Personalia.Models.AppearanceSpace.BodyParts;

/// /// <summary>
/// Physique — the complete physical build of a character.
///
/// Implements the Composite pattern root: Head, Torso, and LimbSet; can be partially concealed by clothing.
/// Affects both how the character acts and how others act toward them.
/// Provides helpers for flat traversal and for resolving parent -> child
/// concealment propagation used by the clothing system.
/// </summary>
public sealed class Physique : IBodyPart
{
    /// <summary>
    /// TODO
    /// </summary>
    public string DisplayName => "Телосложение";

    /// <summary>
    /// TODO
    /// </summary>
    public IReadOnlyList<IBodyPart> SubParts => [Head, Torso, Limbs];

    /// <summary>
    /// TODO
    /// </summary>
    public Head Head { get; } = new();

    /// <summary>
    /// TODO
    /// </summary>
    public Torso Torso { get; } = new();

    /// <summary>
    /// TODO
    /// </summary>
    public LimbSet Limbs { get; } = new();

    // ── Traversal helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Flattens the entire body-part tree into a single sequence
    /// (pre-order depth-first), including the root.
    /// </summary>
    public IEnumerable<IBodyPart> AllParts() => Flatten(this);

    /// <summary>
    /// Returns <paramref name="ancestor"/> itself plus all of its descendants.
    /// Used by the clothing system to propagate slot concealment downward:
    /// if a parent slot is concealed, all child slots are implicitly concealed.
    /// </summary>
    public IEnumerable<IBodyPart> DescendantsOf(IBodyPart ancestor)
        => Flatten(ancestor);

    // ── Private ───────────────────────────────────────────────────────────────

    private static IEnumerable<IBodyPart> Flatten(IBodyPart part)
    {
        yield return part;
        foreach (var child in part.SubParts)
            foreach (var descendant in Flatten(child))
                yield return descendant;
    }
}