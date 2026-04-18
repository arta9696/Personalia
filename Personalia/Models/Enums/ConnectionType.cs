using Personalia.Common;

namespace Personalia.Models.Enums;

/// <summary>
/// ConnectionType — the category of a directed character relationship.
///
/// Carries semantic flags (IsFamily, IsRomantic) so calling code can
/// branch on relationship nature without re-enumerating cases (OCP).
/// </summary>
public sealed class ConnectionType : SmartEnum<ConnectionType>
{
    // ── Registered instances ──────────────────────────────────────────────────

    public static readonly ConnectionType CloseFamily = new(0, nameof(CloseFamily));   //Blood family
    public static readonly ConnectionType Family = new(1, nameof(Family));                   //Legal family
    public static readonly ConnectionType Acquaintance = new(2, nameof(Acquaintance));
    public static readonly ConnectionType Friend = new(3, nameof(Friend));
    public static readonly ConnectionType Colleague = new(4, nameof(Colleague));
    public static readonly ConnectionType Romantic = new(5, nameof(Romantic));

    // ── Behaviour flags ───────────────────────────────────────────────────────

    /// <summary>True if this connection represents a blood or legal family tie.</summary>
    public bool IsFamily => Equals(CloseFamily) || Equals(Family);

    /// <summary>True if this connection represents a romantic attachment.</summary>
    public bool IsRomantic => Equals(Romantic);

    /// <summary>True for any non-familial social connection.</summary>
    public bool IsSocial => !IsFamily && !IsRomantic;

    // ── Constructor ───────────────────────────────────────────────────────────

    private ConnectionType(int value, string name)
        : base(value, name)
    {
    }
}