using Personalia.Models.AppearanceSpace.BodyParts;
using Personalia.Models.Enums;

namespace Personalia.Models.AppearanceSpace;

/// <summary>
/// Appearance — a character's visible and hidden external attributes.
///
/// Affects:
///   • How the character behaves.
///   • How others behave toward the character.
///
/// Each attribute carries an <c>Hidden</c> flag representing
/// whether it is currently concealed from outside observers
/// (скрыто упоминанием / одеждой).
/// </summary>
public sealed class Appearance
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>First name. Can be hidden.</summary>
    public HiddenValue<string> FirstName { get; set; } = new(string.Empty);

    /// <summary>Last name. Can be hidden.</summary>
    public HiddenValue<string> LastName { get; set; } = new(string.Empty);

    // ── Age ───────────────────────────────────────────────────────────────────

    private HiddenValue<int> _age = new(0);

    /// <summary>
    /// Exact age in years — can be hidden.
    /// Automatically keeps <see cref="AgeCategory"/> in sync.
    /// </summary>
    public HiddenValue<int> Age
    {
        get => _age;
        set
        {
            _age = value;
            // Keep the derived category in sync; preserve the hidden flag.
            AgeCategory = new HiddenValue<AgeCategory>(
                Enums.AgeCategory.FromAge(value.Value),
                value.IsHidden);
        }
    }

    /// <summary>
    /// AgeCategory — derived from <see cref="Age"/>.
    /// Read-only externally; set by assigning <see cref="Age"/>.
    /// Can be independently hidden from observers.
    /// </summary>
    public HiddenValue<AgeCategory> AgeCategory { get; private set; }
        = new(Enums.AgeCategory.Child);

    // ── Birthday ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Birth month as a typed <see cref="Month"/> enum value.
    /// Can be hidden from observers (e.g. the character keeps it private).
    /// The birth year is not tracked separately; <see cref="Age"/> carries that
    /// information.
    /// </summary>
    public HiddenValue<Month> BirthdayMonth { get; set; }
        = new(Month.January);

    /// <summary>
    /// Day-of-month of the character's birthday (1–31).
    /// Can be hidden from observers.
    /// </summary>
    public HiddenValue<int> BirthdayDay { get; set; }
        = new(1);

    // ── Orientation ───────────────────────────────────────────────────────────

    /// <summary>SexualOrientation — can be hidden.</summary>
    public HiddenValue<SexualOrientation> SexualOrientation { get; set; }
        = HiddenValue<SexualOrientation>.Hidden(Enums.SexualOrientation.Heterosexual);
 
    /// <summary>BiologicalGender — can be hidden.</summary>
    public HiddenValue<BiologicalGender> BiologicalGender { get; set; }
        = new(Enums.BiologicalGender.Male);

    // ── Physique ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Physique — the full body structure.
    /// Parts can be individually concealed by clothing slots.
    /// </summary>
    public Physique Physique { get; } = new();

    // ── Distinctive features ──────────────────────────────────────────────────

    /// <summary>
    /// Zero, one, or two notable appearance phrases that set this character apart
    /// (e.g. "crooked nose", "wide-set eyes").
    /// Populated by the character-generation layer; empty by default.
    /// </summary>
    public IList<string> DistinctiveFeatures { get; init; } = [];
}
