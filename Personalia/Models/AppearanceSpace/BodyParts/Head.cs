namespace Personalia.Models.AppearanceSpace.BodyParts.HeadPart;
using Personalia.Models.Enums;

// ── Leaf parts ────────────────────────────────────────────────────────────────

public sealed class Hair : IBodyPart
{
    public string DisplayName => "Волосы";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public HairColor Color { get; set; } = HairColor.Brown;
    public HairLength Length { get; set; } = HairLength.Medium;
    public HairShape Shape { get; set; } = HairShape.Straight;
}

public sealed class Eyes : IBodyPart
{
    public string DisplayName => "Глаза";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public EyeColor Color { get; set; } = EyeColor.Brown;
    public EyeShape Shape { get; set; } = EyeShape.Almond;
}

public sealed class Nose : IBodyPart
{
    public string DisplayName => "Нос";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public FeatureShape Shape { get; set; } = FeatureShape.Medium;
}

public sealed class Ears : IBodyPart
{
    public string DisplayName => "Уши";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public FeatureShape Shape { get; set; } = FeatureShape.Medium;
}

public sealed class Lips : IBodyPart
{
    public string DisplayName => "Губы";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public FeatureShape Shape { get; set; } = FeatureShape.Medium;
}

public sealed class Teeth : IBodyPart
{
    public string DisplayName => "Зубы";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public FeatureShape Shape { get; set; } = FeatureShape.Medium;
}

// ── Composite: Mouth ─────────────────────────────────────────────────────────

public sealed class Mouth : IBodyPart
{
    public string DisplayName => "Рот";
    public IReadOnlyList<IBodyPart> SubParts => [Lips, Teeth];

    public Lips Lips { get; } = new();
    public Teeth Teeth { get; } = new();
}

// ── Composite: Head ───────────────────────────────────────────────────────────

public sealed class Head : IBodyPart
{
    public string DisplayName => "Голова";
    public IReadOnlyList<IBodyPart> SubParts => [Hair, Eyes, Nose, Ears, Mouth];

    public Hair Hair { get; } = new();
    public Eyes Eyes { get; } = new();
    public Nose Nose { get; } = new();
    public Ears Ears { get; } = new();
    public Mouth Mouth { get; } = new();
}