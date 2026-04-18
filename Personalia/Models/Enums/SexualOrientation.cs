using Personalia.Common;

namespace Personalia.Models.Enums;

/// <summary>
/// SexualOrientation — the character's pattern of attraction.
/// </summary>
public sealed class SexualOrientation : SmartEnum<SexualOrientation>
{
    public static readonly SexualOrientation Heterosexual = new(0, nameof(Heterosexual));
    public static readonly SexualOrientation Homosexual = new(1, nameof(Homosexual));
    public static readonly SexualOrientation Bisexual = new(2, nameof(Bisexual));
    public static readonly SexualOrientation Asexual = new(3, nameof(Asexual));

    private SexualOrientation(int value, string name)
        : base(value, name) { }
}