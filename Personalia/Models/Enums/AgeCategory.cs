using Personalia.Common;

namespace Personalia.Models.Enums;

/// <summary>
/// AgeCategory — broad developmental stage of a character.
/// Carries the typical age range so business rules can reason about it
/// without hard-coding numbers at every call site (OCP).
/// </summary>
public sealed class AgeCategory : SmartEnum<AgeCategory>
{
    // ── Registered instances ──────────────────────────────────────────────────

    public static readonly AgeCategory Child = new(0, nameof(Child), 0, 12);
    public static readonly AgeCategory Teen = new(1, nameof(Teen), 13, 17);
    public static readonly AgeCategory YoungAdult = new(2, nameof(YoungAdult), 18, 25);
    public static readonly AgeCategory Adult = new(3, nameof(Adult), 26, 45);
    public static readonly AgeCategory MiddleAged = new(4, nameof(MiddleAged), 46, 64);
    public static readonly AgeCategory Senior = new(5, nameof(Senior), 65, 100);

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Inclusive lower bound of the typical age range.</summary>
    public int MinAge { get; }

    /// <summary>Inclusive upper bound of the typical age range.</summary>
    public int MaxAge { get; }

    public bool IsMinor => this == Child || this == Teen;

    // ── Constructor ───────────────────────────────────────────────────────────

    private AgeCategory(int value, string name, int minAge, int maxAge)
        : base(value, name)
    {
        MinAge = minAge;
        MaxAge = maxAge;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives the appropriate category from an exact age in years.
    /// </summary>
    public static AgeCategory FromAge(int age) =>
        All.FirstOrDefault(c => age >= c.MinAge && age <= c.MaxAge)
        ?? Senior; // fallback for ages above all defined ranges
}