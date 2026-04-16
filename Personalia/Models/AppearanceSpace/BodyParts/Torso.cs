namespace Personalia.Models.AppearanceSpace.BodyParts.Body;
using Personalia.Models.Enums;

// ── Internal tissues / systems ────────────────────────────────────────────────

/// <summary>Skin — visible outer surface; affects observation.</summary>
public sealed class Skin : IBodyPart
{
    public string DisplayName => "Кожа";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public SkinColor Color { get; set; } = SkinColor.Light;

    /// <summary>Sensitivity — how sensitive the skin is (0.0–1.0).</summary>
    public float Sensitivity { get; set; } = 0.5f;
}

/// <summary>Skeleton — determines height and fragility.</summary>
public sealed class Skeleton : IBodyPart
{
    public string DisplayName => "Скелет";
    public IReadOnlyList<IBodyPart> SubParts => [];

    /// <summary>Height in centimetres.</summary>
    public float HeightCm { get; set; } = 170f;

    /// <summary>Fragility factor (0.0 = robust, 1.0 = very fragile).</summary>
    public float Fragility { get; set; } = 0.3f;
}

/// <summary>Muscles — muscle volume factor.</summary>
public sealed class Muscles : IBodyPart
{
    public string DisplayName => "Мышцы";
    public IReadOnlyList<IBodyPart> SubParts => [];

    /// <summary>Muscle volume (0.0 = skinny, 1.0 = muscular).</summary>
    public float Volume { get; set; } = 0.4f;
}

/// <summary>Fat tissue volume factor.</summary>
public sealed class FattyTissue : IBodyPart
{
    public string DisplayName => "Жировая ткань";
    public IReadOnlyList<IBodyPart> SubParts => [];

    /// <summary>Fat volume (0.0 = thin, 1.0 = full-figured).</summary>
    public float Volume { get; set; } = 0.3f;
}

/// <summary>Organs — internal organs placeholder (to be extended).</summary>
public sealed class Organs : IBodyPart
{
    public string DisplayName => "Органы";
    public IReadOnlyList<IBodyPart> SubParts => [Skin, Skeleton, Muscles, FattyTissue];

    public Skin Skin { get; } = new();
    public Skeleton Skeleton { get; } = new();
    public Muscles Muscles { get; } = new();
    public FattyTissue FattyTissue { get; } = new();
}

// ── External torso sections ───────────────────────────────────────────────────

public sealed class Neck : IBodyPart
{
    public string DisplayName => "Шея";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public FeatureShape Shape { get; set; } = FeatureShape.Medium;
}

public sealed class Chest : IBodyPart
{
    public string DisplayName => "Грудь";
    public IReadOnlyList<IBodyPart> SubParts => [];

    /// <summary>Size — relative size (0.0 = flat, 1.0 = 25 cm cup, can be bigger).</summary>
    public float Size { get; set; } = 0.5f;
}

public sealed class Belly : IBodyPart
{
    public string DisplayName => "Живот";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public float Size { get; set; } = 0.5f;
}

public sealed class Back : IBodyPart
{
    public string DisplayName => "Спина";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

public sealed class Waist : IBodyPart
{
    public string DisplayName => "Талия";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public float Size { get; set; } = 0.5f;
}

public sealed class Hips : IBodyPart
{
    public string DisplayName => "Бёдра";
    public IReadOnlyList<IBodyPart> SubParts => [];

    public float Size { get; set; } = 0.5f;
}

public sealed class Groin : IBodyPart
{
    public string DisplayName => "Пах";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

// ── Composite: Torso body ─────────────────────────────────────────────────────

/// <summary>
/// Тело — the torso aggregate, containing all torso sections plus internal tissues.
/// </summary>
public sealed class Torso : IBodyPart
{
    public string DisplayName => "Тело";
    public IReadOnlyList<IBodyPart> SubParts =>
        [Organs, Neck, Chest, Belly, Back, Waist, Hips, Groin];

    public Organs Organs { get; } = new();
    public Neck Neck { get; } = new();
    public Chest Chest { get; } = new();
    public Belly Belly { get; } = new();
    public Back Back { get; } = new();
    public Waist Waist { get; } = new();
    public Hips Hips { get; } = new();
    public Groin Groin { get; } = new();
}