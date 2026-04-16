namespace Personalia.Models.AppearanceSpace.BodyParts.Limbs;

// ── Leg segments ──────────────────────────────────────────────────────────────

public sealed class Thigh : IBodyPart
{
    public string DisplayName => "Бедро";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

public sealed class Shin : IBodyPart
{
    public string DisplayName => "Голень";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

public sealed class Foot : IBodyPart
{
    public string DisplayName => "Ступня";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

// ── Arm segments ──────────────────────────────────────────────────────────────

public sealed class Shoulder : IBodyPart
{
    public string DisplayName => "Плечо";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

public sealed class Forearm : IBodyPart
{
    public string DisplayName => "Предплечье";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

public sealed class Wrist : IBodyPart
{
    public string DisplayName => "Запястье";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

public sealed class Hand : IBodyPart
{
    public string DisplayName => "Кисть";
    public IReadOnlyList<IBodyPart> SubParts => [];
}

// ── Composite limbs ───────────────────────────────────────────────────────────

public sealed class Leg : IBodyPart
{
    public string DisplayName => "Нога";
    public IReadOnlyList<IBodyPart> SubParts => [Thigh, Shin, Foot];

    public Thigh Thigh { get; } = new();
    public Shin Shin { get; } = new();
    public Foot Foot { get; } = new();
}

/// <summary>Рука — one arm (left or right).</summary>
public sealed class Arm : IBodyPart
{
    public string DisplayName => "Рука";
    public IReadOnlyList<IBodyPart> SubParts => [Shoulder, Forearm, Wrist, Hand];

    public Shoulder Shoulder { get; } = new();
    public Forearm Forearm { get; } = new();
    public Wrist Wrist { get; } = new();
    public Hand Hand { get; } = new();
}

// ── Composite: all limbs ──────────────────────────────────────────────────────

/// <summary>LimbSet — aggregate of all limbs (usually four).</summary>
public sealed class LimbSet : IBodyPart
{
    public string DisplayName => "Конечности";
    public IReadOnlyList<IBodyPart> SubParts => [LeftLeg, RightLeg, LeftArm, RightArm];

    public Leg LeftLeg { get; } = new();
    public Leg RightLeg { get; } = new();
    public Arm LeftArm { get; } = new();
    public Arm RightArm { get; } = new();
}