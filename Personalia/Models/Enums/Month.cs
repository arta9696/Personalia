using Personalia.Common;

namespace Personalia.Models.Enums;

/// <summary>
/// Month — calendar month, typed as a SmartEnum so it integrates cleanly
/// with <see cref="Personalia.Models.AppearanceSpace.HiddenValue{T}"/>
/// and carries <see cref="DaysInMonth"/> to avoid magic numbers at call sites.
///
/// February is given its standard 28 days; leap-year handling is intentionally
/// kept out of the domain model (birth-year is not tracked).
/// </summary>
public sealed class Month : SmartEnum<Month>
{
    // ── Registered instances ──────────────────────────────────────────────────

    public static readonly Month January = new(1, nameof(January), "Январь", 31);
    public static readonly Month February = new(2, nameof(February), "Февраль", 28);
    public static readonly Month March = new(3, nameof(March), "Март", 31);
    public static readonly Month April = new(4, nameof(April), "Апрель", 30);
    public static readonly Month May = new(5, nameof(May), "Май", 31);
    public static readonly Month June = new(6, nameof(June), "Июнь", 30);
    public static readonly Month July = new(7, nameof(July), "Июль", 31);
    public static readonly Month August = new(8, nameof(August), "Август", 31);
    public static readonly Month September = new(9, nameof(September), "Сентябрь", 30);
    public static readonly Month October = new(10, nameof(October), "Октябрь", 31);
    public static readonly Month November = new(11, nameof(November), "Ноябрь", 30);
    public static readonly Month December = new(12, nameof(December), "Декабрь", 31);

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// Standard number of days in this month (28 for February; leap years ignored).
    /// </summary>
    public int DaysInMonth { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    private Month(int value, string name, string displayName, int daysInMonth)
        : base(value, name, displayName)
    {
        DaysInMonth = daysInMonth;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="Month"/> for the given 1-based month number (1 = January).
    /// </summary>
    /// <exception cref="InvalidOperationException">If the number is outside 1–12.</exception>
    public static Month FromNumber(int monthNumber) => FromValue(monthNumber);
}