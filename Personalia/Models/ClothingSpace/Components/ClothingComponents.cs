using System.Drawing;

namespace Personalia.Models.ClothingSpace.Components;

/// <summary>
/// DiscomfortProtection — reduces discomfort the character feels
/// (e.g. scratching, rubbing, wetness) on the covered slots.
/// </summary>
public sealed class DiscomfortProtection : IClothingComponent
{
    /// <summary>Protection factor 0.0 (none) – 1.0 (full).</summary>
    public float Factor { get; set; } = 0.5f;
}

/// <summary>
/// TemperatureProtection — thermal insulation provided by the item.
/// </summary>
public sealed class TemperatureProtection : IClothingComponent
{
    /// <summary>Insulation value in arbitrary units (0 = none, 1 = full insulation).</summary>
    public float HeatInsulationValue { get; set; } = 0.5f;

    /// <summary>Reflectivity value in arbitrary units (0 = none, 1 = full reflection of heat).</summary>
    public float ReflectivityValue { get; set; } = 0.25f;
}

/// <summary>
/// SlotConcealment — conceals the covered body slots from
/// outside observers, setting the effective <c>IsHidden</c> for those slots.
/// </summary>
public sealed class SlotConcealment : IClothingComponent
{
    public bool Conceals { get; set; } = true;
}

/// <summary>
/// ColorComponent — visible colour of the clothing item, which can
/// influence how observers perceive the character.
/// </summary>
public sealed class ColorComponent : IClothingComponent
{
    private Color _color;
    public string HexColor { 
        get { return ColorTranslator.ToHtml(_color); } 
        set {
            if (value.StartsWith('#'))
            {
                _color = ColorTranslator.FromHtml(value);
            }
            else
            {
                try
                {
                    _color = Color.FromName(value);
                }
                catch
                {
                    _color = Color.White;
                }
            }
        } 
    }
    public string ColorName => GetColorName(HexColor);

    private string GetColorName(string hex)
    {
        foreach (KnownColor kc in Enum.GetValues(typeof(KnownColor)))
        {
            Color known = Color.FromKnownColor(kc);
            if (_color.ToArgb() == known.ToArgb())
            {
                return known.Name;
            }
        }
        return hex;
    }
}