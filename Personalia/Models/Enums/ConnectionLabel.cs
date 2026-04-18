using Personalia.Common;

namespace Personalia.Models.Enums;

/// <summary>
/// ConnectionLabel — the specific role label of a directed character relationship.
///
/// Replaces raw string labels so that role identity is type-safe and display
/// text is resolved through <see cref="Personalia.Localization.ILocalizationProvider"/>
/// rather than being hard-coded inside the model (OCP, SRP).
///
/// Semantic flag groups allow callers to branch on role category without
/// enumerating individual values at every call site.
/// </summary>
public sealed class ConnectionLabel : SmartEnum<ConnectionLabel>
{
    // ── Family — parental ─────────────────────────────────────────────────────

    public static readonly ConnectionLabel Mother = new(0, nameof(Mother));
    public static readonly ConnectionLabel Father = new(1, nameof(Father));

    // ── Family — children ─────────────────────────────────────────────────────

    public static readonly ConnectionLabel Son = new(2, nameof(Son));
    public static readonly ConnectionLabel Daughter = new(3, nameof(Daughter));

    // ── Family — siblings ─────────────────────────────────────────────────────

    public static readonly ConnectionLabel Brother = new(4, nameof(Brother));
    public static readonly ConnectionLabel Sister = new(5, nameof(Sister));

    // ── Partners ──────────────────────────────────────────────────────────────

    public static readonly ConnectionLabel RomanticPartner = new(6, nameof(RomanticPartner));
    public static readonly ConnectionLabel PlatonicPartner = new(7, nameof(PlatonicPartner));

    // ── Semantic flag groups ──────────────────────────────────────────────────

    /// <summary>True if this label represents a parental role (mother or father).</summary>
    public bool IsParent => this == Mother || this == Father;

    /// <summary>True if this label represents a child role (son or daughter).</summary>
    public bool IsChild => this == Son || this == Daughter;

    /// <summary>True if this label represents a sibling role (brother or sister).</summary>
    public bool IsSibling => this == Brother || this == Sister;

    /// <summary>True if this label represents a partner role (romantic or platonic).</summary>
    public bool IsPartner => this == RomanticPartner || this == PlatonicPartner;

    // ── Constructor ───────────────────────────────────────────────────────────

    private ConnectionLabel(int value, string name)
        : base(value, name) { }
}