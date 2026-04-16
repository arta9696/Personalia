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

    /// <summary>Human-readable label, ready for localisation.</summary>
    public string DisplayName { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    protected SmartEnum(int value, string name, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Value = value;
        Name = name;
        DisplayName = displayName;

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

    public static bool TryFromValue(int value, out TSelf? result)
    {
        Initialise();
        return ByValue.TryGetValue(value, out result);
    }

    public static bool TryFromName(string name, out TSelf? result)
    {
        Initialise();
        return ByName.TryGetValue(name, out result);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    public bool Equals(SmartEnum<TSelf>? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is SmartEnum<TSelf> other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => DisplayName;

    public static bool operator ==(SmartEnum<TSelf>? a, SmartEnum<TSelf>? b)
        => ReferenceEquals(a, b) || (a is not null && a.Equals(b));

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