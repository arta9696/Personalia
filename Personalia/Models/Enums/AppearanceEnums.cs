using Personalia.Common;

namespace Personalia.Models.Enums;

// ── Hair ─────────────────────────────────────────────────────────────────────

/// <summary>
/// HairColor — natural and dyed hair colours.
/// Each value carries a representative hex swatch so UI renderers
/// can display a colour without coupling to a specific graphics library.
/// </summary>
public sealed class HairColor : SmartEnum<HairColor>
{
    public static readonly HairColor Black = new(0, nameof(Black), "Чёрный", "#1C1008");
    public static readonly HairColor DarkBrown = new(1, nameof(DarkBrown), "Тёмно-каштановый", "#2C1A0E");
    public static readonly HairColor Brown = new(2, nameof(Brown), "Каштановый", "#5C3D1E");
    public static readonly HairColor LightBrown = new(3, nameof(LightBrown), "Светло-каштановый", "#8B6343");
    public static readonly HairColor Blonde = new(4, nameof(Blonde), "Светлый", "#D4A853");
    public static readonly HairColor Platinum = new(5, nameof(Platinum), "Платиновый", "#E8E0C8");
    public static readonly HairColor Red = new(6, nameof(Red), "Рыжий", "#B34A2A");
    public static readonly HairColor Auburn = new(7, nameof(Auburn), "Медно-рыжий", "#922B21");
    public static readonly HairColor Grey = new(8, nameof(Grey), "Серый", "#9E9E9E");
    public static readonly HairColor White = new(9, nameof(White), "Белый", "#F5F5F5");
    public static readonly HairColor Dyed = new(10, nameof(Dyed), "Крашеный", "");

    /// <summary>Approximate RGB swatch as a CSS hex string.</summary>
    public string RepresentativeHex { get; }

    private HairColor(int value, string name, string displayName, string hex)
        : base(value, name, displayName)
    {
        RepresentativeHex = hex;
    }
}

/// <summary>
/// HairLength — from shaved to waist-length.
/// Carries an approximate centimetre range for downstream calculations
/// (e.g. concealment checks, grooming time).
/// </summary>
public sealed class HairLength : SmartEnum<HairLength>
{
    public static readonly HairLength Bald = new(0, nameof(Bald), "Лысый", 0, 0);
    public static readonly HairLength CloseCropped = new(1, nameof(CloseCropped), "Очень короткие", 1, 3);
    public static readonly HairLength Short = new(2, nameof(Short), "Короткие", 4, 10);
    public static readonly HairLength EarLength = new(3, nameof(EarLength), "До ушей", 11, 15);
    public static readonly HairLength Medium = new(4, nameof(Medium), "Средние", 16, 30);
    public static readonly HairLength Long = new(5, nameof(Long), "Длинные", 31, 60);
    public static readonly HairLength WaistLength = new(6, nameof(WaistLength), "До талии", 61, 100);

    /// <summary>Approximate minimum length in centimetres.</summary>
    public int MinCm { get; }

    /// <summary>Approximate maximum length in centimetres.</summary>
    public int MaxCm { get; }

    private HairLength(int value, string name, string displayName, int minCm, int maxCm)
        : base(value, name, displayName)
    {
        MinCm = minCm;
        MaxCm = maxCm;
    }
}

/// <summary>HairShape — curl pattern of the hair strand.</summary>
public sealed class HairShape : SmartEnum<HairShape>
{
    public static readonly HairShape Straight = new(0, nameof(Straight), "Прямые");
    public static readonly HairShape Wavy = new(1, nameof(Wavy), "Волнистые");
    public static readonly HairShape Curly = new(2, nameof(Curly), "Кудрявые");
    public static readonly HairShape Coily = new(3, nameof(Coily), "Вьющиеся");

    private HairShape(int value, string name, string displayName)
        : base(value, name, displayName) { }
}

// ── Eyes ─────────────────────────────────────────────────────────────────────

/// <summary>EyeColor — iris colour with a representative hex swatch.</summary>
public sealed class EyeColor : SmartEnum<EyeColor>
{
    public static readonly EyeColor Brown = new(0, nameof(Brown), "Карие", "#6B3A2A");
    public static readonly EyeColor Blue = new(1, nameof(Blue), "Голубые", "#4A8AB5");
    public static readonly EyeColor Green = new(2, nameof(Green), "Зелёные", "#5A8C5A");
    public static readonly EyeColor Grey = new(3, nameof(Grey), "Серые", "#8A8A9A");
    public static readonly EyeColor Hazel = new(4, nameof(Hazel), "Ореховые", "#7B6832");
    public static readonly EyeColor Amber = new(5, nameof(Amber), "Янтарные", "#C08B30");

    /// <summary>Approximate iris hex swatch.</summary>
    public string RepresentativeHex { get; }

    private EyeColor(int value, string name, string displayName, string hex)
        : base(value, name, displayName)
    {
        RepresentativeHex = hex;
    }
}

/// <summary>EyeShape — geometry of the eye opening.</summary>
public sealed class EyeShape : SmartEnum<EyeShape>
{
    public static readonly EyeShape Almond = new(0, nameof(Almond), "Миндалевидные");
    public static readonly EyeShape Round = new(1, nameof(Round), "Круглые");
    public static readonly EyeShape Hooded = new(2, nameof(Hooded), "Нависшие");
    public static readonly EyeShape Upturned = new(3, nameof(Upturned), "Приподнятые");
    public static readonly EyeShape Downturned = new(4, nameof(Downturned), "Опущенные");

    private EyeShape(int value, string name, string displayName)
        : base(value, name, displayName) { }
}

// ── Generic feature ───────────────────────────────────────────────────────────

/// <summary>FeatureShape — generic relative size/shape for facial features.</summary>
public sealed class FeatureShape : SmartEnum<FeatureShape>
{
    public static readonly FeatureShape Small = new(0, nameof(Small), "Маленький");
    public static readonly FeatureShape Medium = new(1, nameof(Medium), "Средний");
    public static readonly FeatureShape Large = new(2, nameof(Large), "Большой");
    public static readonly FeatureShape Narrow = new(3, nameof(Narrow), "Узкий");
    public static readonly FeatureShape Wide = new(4, nameof(Wide), "Широкий");
    public static readonly FeatureShape Pointed = new(5, nameof(Pointed), "Заострённый");
    public static readonly FeatureShape Rounded = new(6, nameof(Rounded), "Округлый");

    private FeatureShape(int value, string name, string displayName)
        : base(value, name, displayName) { }
}

// ── Skin ─────────────────────────────────────────────────────────────────────

/// <summary>SkinColor — Fitzpatrick-inspired skin tone scale with hex swatch.</summary>
public sealed class SkinColor : SmartEnum<SkinColor>
{
    public static readonly SkinColor Pale = new(0, nameof(Pale), "Бледный", "#F5E6DA");
    public static readonly SkinColor Fair = new(1, nameof(Fair), "Светлый", "#F1D5B8");
    public static readonly SkinColor Light = new(2, nameof(Light), "Молочный", "#DEB887");
    public static readonly SkinColor Olive = new(3, nameof(Olive), "Оливковый", "#C8A97E");
    public static readonly SkinColor Beige = new(4, nameof(Beige), "Бежевый", "#BA8C63");
    public static readonly SkinColor Tan = new(5, nameof(Tan), "Загорелый", "#A07040");
    public static readonly SkinColor Brown = new(6, nameof(Brown), "Коричневый", "#7D5130");
    public static readonly SkinColor Dark = new(7, nameof(Dark), "Тёмный", "#4A2E1A");
    public static readonly SkinColor Ebony = new(8, nameof(Ebony), "Эбонитовый", "#2C1810");

    /// <summary>Approximate skin-tone hex swatch.</summary>
    public string RepresentativeHex { get; }

    private SkinColor(int value, string name, string displayName, string hex)
        : base(value, name, displayName)
    {
        RepresentativeHex = hex;
    }
}