using Personalia.Models.AppearanceSpace;
using Personalia.Models.AppearanceSpace.BodyParts;
using Personalia.Models.ClothingSpace;
using Personalia.Models.PersonalitySpace;

namespace Personalia.Models;

/// <summary>
/// Character — the root aggregate of the Personalia domain.
///
/// A character is composed of three orthogonal concerns:
///
///   1. <see cref="Appearance"/>  — Внешность
///      Physical and identity attributes; some hidden, some visible.
///      Drives both the character's own behaviour and how others treat them.
///
///   2. <see cref="Clothing"/>    — Одежда
///      Clothing items that cover body slots, providing protection and
///      optionally concealing appearance attributes from observers.
///
///   3. <see cref="Personality"/> — Характер
///      Behavioural traits that govern the character's decisions.
///
/// Relationships with other characters are held in the shared
/// <c>ConnectionGraph</c> rather than per-character lists, providing a single
/// source of truth for the social graph.
/// </summary>
public sealed class Character
{
    public Guid Id { get; } = Guid.NewGuid();

    // ── The three pillars ─────────────────────────────────────────────────────

    public Appearance Appearance { get; } = new();
    public Clothing Clothing { get; } = new();
    public Personality Personality { get; } = new();

    // ── Vital / social state ──────────────────────────────────────────────────

    /// <summary>
    /// Whether the character is currently alive.
    /// Defaults to <c>true</c>; set to <c>false</c> for deceased characters
    /// (e.g. deceased family members in a life-connections graph).
    /// </summary>
    public bool IsAlive { get; set; } = true;

    /// <summary>
    /// Current occupation or workplace description.
    /// <c>null</c>        — unemployed / too young to work.
    /// <c>"retired"</c>   — retired.
    /// Any other string   — workplace name / description (e.g. "tech startup").
    /// </summary>
    public string? Occupation { get; set; }

    // ── Derived / convenience ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the character's display name, respecting hidden flags.
    /// If the first or last name is hidden, it is replaced with "???" for
    /// any observer who does not have privileged knowledge.
    /// </summary>
    public string GetDisplayName(bool privilegedObserver = false)
    {
        var first = privilegedObserver || !Appearance.FirstName.IsHidden
            ? Appearance.FirstName.Value
            : "???";
        var last = privilegedObserver || !Appearance.LastName.IsHidden
            ? Appearance.LastName.Value
            : "???";
        return $"{first} {last}".Trim();
    }

    /// <summary>
    /// Resolves which body slots are currently concealed by worn clothing.
    /// </summary>
    public ISet<IBodyPart> GetConcealedSlots()
        => Clothing.GetConcealedSlots();
}