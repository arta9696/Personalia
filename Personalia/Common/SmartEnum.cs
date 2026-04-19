using System.Runtime.CompilerServices;

namespace Personalia.Common;

/// <summary>
/// Base class for the Smart Enum pattern.
///
/// Replaces plain C# enums with type-safe, OCP-friendly objects that can
/// carry data, behaviour, and be extended without modifying existing code.
///
/// Each closed generic type (<typeparamref name="TSelf"/>) maintains its own
/// static registry of instances — safe because generic statics are per-instantiation.
///
/// Usage:
/// <code>
///   public sealed class HairColor : SmartEnum&lt;HairColor&gt;
///   {
///       public static readonly HairColor Brown = new(0, nameof(Brown), "Каштановый");
///       private HairColor(int v, string n, string d) : base(v, n, d) { }
///   }
/// </code>
/// </summary>
public abstract class SmartEnum<TSelf> : IEquatable<SmartEnum<TSelf>>
    where TSelf : SmartEnum<TSelf>
{
    // ── Per-enum-type registries (each closed generic gets its own statics) ──

    private static readonly Dictionary<int, TSelf> ByValue = [];
    private static readonly Dictionary<string, TSelf> ByName
        = new(StringComparer.OrdinalIgnoreCase);

    // ── Core properties ───────────────────────────────────────────────────────

    /// <summary>Numeric discriminator — unique within the enum type.</summary>
    public int Value { get; }

    /// <summary>Code-friendly identifier — matches the static field name.</summary>
    public string Name { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a new instance and registers it in the per-type lookup tables
    /// by both numeric <paramref name="value"/> and <paramref name="name"/>.
    ///
    /// Called from derived-type static field initialisers, so the registry is
    /// fully populated before any consumer can call <see cref="All"/>,
    /// <see cref="FromValue"/>, or <see cref="FromName"/>.
    /// </summary>
    /// <param name="value">Numeric discriminator, unique within the enum type.</param>
    /// <param name="name">Code-friendly identifier matching the static field name.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    protected SmartEnum(int value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Value = value;
        Name = name;

        // Self-registration: runs at type-initialisation of the derived class.
        ByValue[value] = (TSelf)this;
        ByName[name] = (TSelf)this;
    }

    // ── Registry API ──────────────────────────────────────────────────────────

    /// <summary>All registered instances of this enum type.</summary>
    public static IReadOnlyCollection<TSelf> All
    {
        get { Initialise(); return ByValue.Values; }
    }

    /// <summary>Look up by numeric value. Throws if not found.</summary>
    public static TSelf FromValue(int value)
    {
        Initialise();
        return ByValue.TryGetValue(value, out var result)
            ? result
            : throw new InvalidOperationException(
                $"No {typeof(TSelf).Name} with value {value}.");
    }

    /// <summary>Look up by name (case-insensitive). Throws if not found.</summary>
    public static TSelf FromName(string name)
    {
        Initialise();
        return ByName.TryGetValue(name, out var result)
            ? result
            : throw new InvalidOperationException(
                $"No {typeof(TSelf).Name} with name '{name}'.");
    }

    /// <summary>
    /// Tries to find an instance by its numeric discriminator.
    /// Returns <c>true</c> and sets <paramref name="result"/> when found;
    /// returns <c>false</c> and sets <paramref name="result"/> to <c>null</c> otherwise.
    /// </summary>
    /// <param name="value">The numeric discriminator to look up.</param>
    /// <param name="result">
    /// The matching instance when the method returns <c>true</c>; otherwise <c>null</c>.
    /// </param>
    public static bool TryFromValue(int value, out TSelf? result)
    {
        Initialise();
        return ByValue.TryGetValue(value, out result);
    }

    /// <summary>
    /// Tries to find an instance by its name (case-insensitive).
    /// Returns <c>true</c> and sets <paramref name="result"/> when found;
    /// returns <c>false</c> and sets <paramref name="result"/> to <c>null</c> otherwise.
    /// </summary>
    /// <param name="name">The name to look up (matched case-insensitively).</param>
    /// <param name="result">
    /// The matching instance when the method returns <c>true</c>; otherwise <c>null</c>.
    /// </param>
    public static bool TryFromName(string name, out TSelf? result)
    {
        Initialise();
        return ByName.TryGetValue(name, out result);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when <paramref name="other"/> is not <c>null</c>
    /// and shares this instance's numeric <see cref="Value"/>.
    /// </summary>
    /// <param name="other">The instance to compare against.</param>
    public bool Equals(SmartEnum<TSelf>? other) => other is not null && Value == other.Value;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="obj"/> is a
    /// <see cref="SmartEnum{TSelf}"/> whose <see cref="Value"/> equals this instance's.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    public override bool Equals(object? obj) => obj is SmartEnum<TSelf> other && Equals(other);

    /// <summary>
    /// Returns the numeric <see cref="Value"/>, which is unique within the enum type,
    /// making it a suitable hash code.
    /// </summary>
    public override int GetHashCode() => Value;

    /// <summary>
    /// Returns the code-friendly <see cref="Name"/> of this instance
    /// (e.g. <c>"Brown"</c>, <c>"YoungAdult"</c>).
    /// </summary>
    public override string ToString() => Name;

    /// <summary>
    /// Returns <c>true</c> when both operands are <c>null</c>, or both reference
    /// an instance with the same numeric <see cref="Value"/>.
    /// </summary>
    public static bool operator ==(SmartEnum<TSelf>? a, SmartEnum<TSelf>? b)
        => ReferenceEquals(a, b) || (a is not null && a.Equals(b));

    /// <summary>
    /// Returns <c>true</c> when the operands do not compare equal under <c>==</c>.
    /// </summary>
    public static bool operator !=(SmartEnum<TSelf>? a, SmartEnum<TSelf>? b)
        => !(a == b);

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Forces the derived type's static constructor to run, populating the
    /// registry before any lookup is attempted.
    /// </summary>
    private static void Initialise()
        => RuntimeHelpers.RunClassConstructor(typeof(TSelf).TypeHandle);
}