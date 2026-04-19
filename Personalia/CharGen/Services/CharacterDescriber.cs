using Personalia.Common;
using Personalia.Localization;
using Personalia.Localization.En;
using Personalia.Models;
using Personalia.Models.ConnectionSpace;
using Personalia.Models.Enums;
using System.Text;

namespace Personalia.CharGen.Services;

/// <summary>
/// CharacterDescriber — formats a <see cref="Character"/> as a human-readable description.
///
/// Reads social connections from the shared <see cref="ConnectionGraph"/> rather than
/// from per-character lists. All display strings — including connection labels and
/// connection type names — are resolved through <see cref="ILocalizationProvider"/>
/// so that no locale-specific text is embedded in the domain model.
///
/// A <see cref="LocalizationContext"/> derived from the character's biological gender
/// is passed to locale-sensitive lookups (e.g. age-group adjectives) so that
/// providers such as <c>RussianLocalizationProvider</c> can apply correct
/// grammatical agreement without altering templates or adding extra format parameters.
///
/// String-formatting utilities are provided by <see cref="TextFormatter"/>.
/// The default locale is English.
/// </summary>
public sealed class CharacterDescriber
{
    private readonly ILocalizationProvider _loc;

    /// <param name="loc">
    /// Localisation provider (defaults to <see cref="EnglishLocalizationProvider"/>).
    /// </param>
    public CharacterDescriber(ILocalizationProvider? loc = null)
    {
        _loc = loc ?? new EnglishLocalizationProvider();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Produces the full description block for <paramref name="character"/>.</summary>
    public string Describe(Character character, ConnectionGraph graph)
    {
        var app = character.Appearance;
        var sb = new StringBuilder();

        // ── Derived context values ─────────────────────────────────────────────
        bool isMale = app.BiologicalGender.Value == BiologicalGender.Male;
        var genderCtx = new LocalizationContext(app.BiologicalGender.Value.Name);
        var ageCtx = ContextFromAge(app.Age.Value);
        var p = app.Physique;

        string skin = _loc.GetEnumValue(p.Torso.Organs.Skin.Color);
        string eyeCol = _loc.GetEnumValue(p.Head.Eyes.Color);
        string eyeShp = _loc.GetEnumValue(p.Head.Eyes.Shape);
        string hairLen = _loc.GetEnumValue(p.Head.Hair.Length);
        string hairCol = _loc.GetEnumValue(p.Head.Hair.Color);

        string birthMonth = _loc.GetEnumValue(app.BirthdayMonth.Value);
        int birthDay = app.BirthdayDay.Value;
        int age = app.Age.Value;

        // ── Header ────────────────────────────────────────────────────────────
        sb.AppendLine(_loc.Format("Describer.Header", ageCtx,
            app.FirstName.Value, app.LastName.Value, age.ToString(), birthMonth, birthDay.ToString()));

        // ── Appearance ────────────────────────────────────────────────────────
        sb.AppendLine(_loc.Get("Describer.SectionAppearance"));

        sb.Append(_loc.Format("Describer.AppearanceLine1",
            _loc.GetEnumValue(AgeCategory.FromAge(age), genderCtx),
            _loc.GetEnumValue(app.BiologicalGender.Value),
            _loc.GetEnumValue(app.SexualOrientation.Value),
            HeightLabel(p.Torso.Organs.Skeleton.HeightCm),
            BuildLabel(p.Torso.Organs.Muscles.Volume, p.Torso.Organs.FattyTissue.Volume)));

        sb.AppendLine(_loc.Format("Describer.AppearanceLine2",
            skin, eyeCol, eyeShp, hairLen, hairCol));

        if (app.DistinctiveFeatures.Count == 1)
        {
            sb.AppendLine(_loc.Format("Describer.DistinctiveFeature",
                app.DistinctiveFeatures[0]));
        }
        else if (app.DistinctiveFeatures.Count > 1)
        {
            sb.AppendLine(_loc.Format("Describer.DistinctiveFeatures",
                TextFormatter.JoinWithAnd(app.DistinctiveFeatures)));
        }

        // ── Clothing ──────────────────────────────────────────────────────────
        sb.AppendLine(_loc.Get("Describer.SectionClothing"));

        var worn = character.Clothing.WornItems;
        string legs = FindByTag(worn, "legwear");
        string top = FindByTag(worn, "topwear");
        string feet = FindByTag(worn, "footwear");
        var accessories = worn
            .Where(i => i.Tags.Contains("accessory"))
            .Select(i => i.Name)
            .ToList();

        sb.Append(_loc.Format("Describer.ClothingBase", legs, top, feet));
        if (accessories.Count > 0)
            sb.Append(_loc.Format("Describer.ClothingAccessories",
                TextFormatter.JoinWithAnd(accessories)));
        sb.AppendLine();

        // ── Status — read from the shared ConnectionGraph ─────────────────────
        sb.AppendLine(_loc.Get("Describer.SectionStatus"));

        // Family
        var familyConns = graph.From(character.Id).Family().All;
        sb.Append(_loc.Get("Describer.FamilyPrefix"));
        sb.AppendLine(familyConns.Count > 0
            ? string.Join(", ", familyConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                string role = LabelOrType(c);
                string alive = _loc.Get(rel.IsAlive ? "Describer.Alive" : "Describer.Deceased", new LocalizationContext(rel.Appearance.BiologicalGender.Value.Name));
                return _loc.Format("Describer.FamilyEntry",
                    role, rel.GetDisplayName(privilegedObserver: true),
                    rel.Appearance.Age.Value.ToString(), alive);
            }))
            : _loc.Get("Describer.None"));

        // Acquaintances
        var acqConns = graph.From(character.Id).OfType(ConnectionType.Acquaintance).All;
        sb.Append(_loc.Get("Describer.AcquaintancePrefix"));
        sb.AppendLine(acqConns.Count > 0
            ? string.Join(", ", acqConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                return _loc.Format("Describer.AcquaintanceEntry",
                    rel.GetDisplayName(privilegedObserver: true),
                    rel.Appearance.Age.Value.ToString());
            }))
            : _loc.Get("Describer.None"));

        // Partners
        var partnerConns = graph.From(character.Id).Partners().All;
        sb.Append(_loc.Get("Describer.PartnerPrefix"));
        sb.AppendLine(partnerConns.Count > 0
            ? string.Join(", ", partnerConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                return _loc.Format("Describer.PartnerEntry",
                    rel.GetDisplayName(privilegedObserver: true),
                    LabelOrType(c), rel.Appearance.Age.Value.ToString());
            }))
            : _loc.Get("Describer.None"));

        // Occupation
        sb.AppendLine(character.Occupation switch
        {
            null => _loc.Get("Describer.Unemployed"),
            "retired" => _loc.Get("Describer.Retired"),
            var w => _loc.Format("Describer.Working", w)
        });

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="value"></param>
    private LocalizationContext? ContextFromAge(int value)
    {
        if (value % 10 == 1 && value / 10 % 10 != 1)
        {
            return new LocalizationContext("Singular");
        }
        else if (value % 10 < 5 && value / 10 % 10 != 1)
        {
            return new LocalizationContext("Dual");
        }
        else
        {
            return null;
        }
    }

    // ── Label helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the localised display string for a connection's role.
    /// Uses the typed <see cref="ConnectionLabel"/> when set;
    /// falls back to the localised <see cref="ConnectionType"/> name.
    /// </summary>
    private string LabelOrType(Connection c)
        => c.Label is not null
            ? _loc.GetEnumValue(c.Label, new LocalizationContext(c.ToCharacterNode.Character.Appearance.BiologicalGender.Value.Name))
            : _loc.GetEnumValue(c.Type, new LocalizationContext(c.ToCharacterNode.Character.Appearance.BiologicalGender.Value.Name));

    private string HeightLabel(float cm) => cm switch
    {
        < 160f => _loc.Get("Height.VeryShort"),
        < 167f => _loc.Get("Height.Short"),
        < 172f => _loc.Get("Height.SlightlyShort"),
        < 178f => _loc.Get("Height.Average"),
        < 183f => _loc.Get("Height.SlightlyTall"),
        < 190f => _loc.Get("Height.Tall"),
        _ => _loc.Get("Height.VeryTall")
    };

    private string BuildLabel(float muscle, float fat)
    {
        bool lowM = muscle < 0.33f;
        bool highM = muscle > 0.66f;
        bool lowF = fat < 0.33f;
        bool highF = fat > 0.66f;

        string key = (lowM, highM, lowF, highF) switch
        {
            (true, _, true, _) => "Build.Skinny",
            (true, _, false, false) => "Build.Thin",
            (true, _, _, true) => "Build.Plump",
            (false, false, true, _) => "Build.Lean",
            (false, false, _, true) => "Build.Stocky",
            (_, true, true, _) => "Build.Ripped",
            (_, true, false, false) => "Build.Muscular",
            (_, true, _, true) => "Build.Brawny",
            _ => "Build.Average"
        };
        return _loc.Get(key);
    }

    // ── Clothing helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the name of the first worn item that carries <paramref name="tag"/>,
    /// or <c>"unknown"</c> when no matching item is found.
    /// </summary>
    private static string FindByTag(
        IReadOnlyList<Models.ClothingSpace.ClothingItem> items, string tag)
        => items.FirstOrDefault(i => i.Tags.Contains(tag))?.Name ?? "unknown";
}