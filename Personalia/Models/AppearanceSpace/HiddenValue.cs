namespace Personalia.Models.AppearanceSpace;

/// <summary>
/// HiddenValue&lt;T&gt; — wraps a value with a visibility flag.
///
/// Represents whether an attribute is currently concealed from outside
/// observers (скрыто упоминанием / одеждой).
///
/// Improvements over the original:
///   • Immutable-by-default construction — set explicitly to mutate.
///   • <see cref="ValueChanged"/> event for reactive downstream updates.
///   • Fluent <see cref="Hidden"/> / <see cref="Revealed"/> factory helpers.
/// </summary>
/// <typeparam name="T">Type of the wrapped value.</typeparam>
public sealed class HiddenValue<T>
{
    private T _value;
    private bool _isHidden;

    // ── Construction ──────────────────────────────────────────────────────────

    public HiddenValue(T value, bool isHidden = false)
    {
        _value = value;
        _isHidden = isHidden;
    }

    /// <summary>Creates a hidden wrapper around <paramref name="value"/>.</summary>
    public static HiddenValue<T> Hidden(T value) => new(value, isHidden: true);

    /// <summary>Creates a visible wrapper around <paramref name="value"/>.</summary>
    public static HiddenValue<T> Revealed(T value) => new(value, isHidden: false);

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>The underlying value.</summary>
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// When <c>true</c>, observers cannot determine this value without
    /// the character explicitly revealing it.
    /// </summary>
    public bool IsHidden
    {
        get => _isHidden;
        set
        {
            if (_isHidden != value)
            {
                _isHidden = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Raised whenever <see cref="Value"/> or <see cref="IsHidden"/> changes.</summary>
    public event EventHandler? ValueChanged;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see cref="Value"/> when visible to the observer,
    /// or <paramref name="fallback"/> when hidden.
    /// </summary>
    public T Resolve(T fallback, bool privilegedObserver = false)
        => privilegedObserver || !_isHidden ? _value : fallback;

    public override string ToString()
        => _isHidden ? "[скрыто]" : (_value?.ToString() ?? string.Empty);
}