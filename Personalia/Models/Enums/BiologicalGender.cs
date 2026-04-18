using Personalia.Common;

namespace Personalia.Models.Enums;

/// <summary>
/// BiologicalGender — the character's biological sex.
/// </summary>
public sealed class BiologicalGender : SmartEnum<BiologicalGender>
{
    public static readonly BiologicalGender Male = new(0, nameof(Male));
    public static readonly BiologicalGender Female = new(1, nameof(Female));

    private BiologicalGender(int value, string name)
        : base(value, name) { }
}