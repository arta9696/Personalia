using Personalia.Models.AppearanceSpace;
using Personalia.Models.ClothingSpace;
using Personalia.Models.ConnectionSpace;
using Personalia.Models.PersonalitySpace;

namespace Personalia.Models;

/// <summary>
/// Character — the root aggregate of the Personlia domain.
///
/// A character is composed of four orthogonal concerns:
///
///   1. <see cref="Appearance"/>   — Внешность
///      Physical and identity attributes; some hidden, some visible.
///      Drives both the character's own behaviour and how others treat them.
///
///   2. <see cref="Clothing"/>     — Одежда
///      Clothing items that cover body slots, providing protection and
///      optionally concealing appearance attributes from observers.
///
///   3. <see cref="Personality"/>  — Характер
///      Behavioural traits that govern the character's decisions.
///
///   4. <see cref="LifeConnections"/> — Жизненные Связи
///      A directed multigraph of relationships with other characters.
/// </summary>
public sealed class Character
{
    public Guid Id { get; } = Guid.NewGuid();

    // ── The four pillars ──────────────────────────────────────────────────────

    public Appearance Appearance { get; } = new();
    public Clothing Clothing { get; } = new();
    public Personality Personality { get; } = new();
    public LifeConnections LifeConnections { get; } = new();

    // ── Vital / social state ──────────────────────────────────────────────────

    /// <summary>
    /// Whether the character is currently alive.
    /// Defaults to <c>true</c>; set to <c>false</c> for deceased characters
    /// (e.g. deceased family members in a life-connections graph).
    /// </summary>
    public bool IsAlive { get; set; } = true;

    /// <summary>
    /// Current occupation or workplace description.
    /// <c>null</c>  — unemployed / too young to work.
    /// <c>"retired"</c> — retired.
    /// Any other string — workplace name / description (e.g. "tech startup").
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
    /// Resolves which body slots are currently concealed —
    /// combining explicit <see cref="Wardrobe"/> coverage with any
    /// appearance-level hidden flags.
    /// </summary>
    public ISet<AppearanceSpace.BodyParts.IBodyPart> GetConcealedSlots()
        => Clothing.GetConcealedSlots();
}