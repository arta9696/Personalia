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

        // ── Resolved appearance values ─────────────────────────────────────────
        var p = app.Physique;

        string skin = _loc.GetEnumValue("SkinColor", p.Torso.Organs.Skin.Color.Name);
        string eyeCol = _loc.GetEnumValue("EyeColor", p.Head.Eyes.Color.Name);
        string eyeShp = _loc.GetEnumValue("EyeShape", p.Head.Eyes.Shape.Name);
        string hairLen = _loc.GetEnumValue("HairLength", p.Head.Hair.Length.Name);
        string hairCol = _loc.GetEnumValue("HairColor", p.Head.Hair.Color.Name);

        string birthMonth = _loc.GetEnumValue("Month", app.BirthdayMonth.Value.Name);
        int birthDay = app.BirthdayDay.Value;
        int age = app.Age.Value;

        // ── Header ────────────────────────────────────────────────────────────
        sb.AppendLine(_loc.Format(Lk.Describer.Header,
            app.FirstName.Value, app.LastName.Value, age, birthMonth, birthDay));

        // ── Appearance ────────────────────────────────────────────────────────
        sb.AppendLine(_loc.Get(Lk.Describer.SectionAppearance));

        sb.Append(_loc.Format(Lk.Describer.AppearanceLine1,
            AgeGroupLabel(age),
            GenderLabel(app.BiologicalGender.Value),
            OrientationLabel(app.SexualOrientation.Value),
            HeightLabel(p.Torso.Organs.Skeleton.HeightCm),
            BuildLabel(p.Torso.Organs.Muscles.Volume, p.Torso.Organs.FattyTissue.Volume)));

        sb.AppendLine(_loc.Format(Lk.Describer.AppearanceLine2,
            skin, eyeCol, eyeShp, hairLen, hairCol));

        if (app.DistinctiveFeatures.Count == 1)
        {
            sb.AppendLine(_loc.Format(Lk.Describer.DistinctiveFeature,
                app.DistinctiveFeatures[0]));
        }
        else if (app.DistinctiveFeatures.Count > 1)
        {
            sb.AppendLine(_loc.Format(Lk.Describer.DistinctiveFeatures,
                JoinWithAnd(app.DistinctiveFeatures)));
        }

        // ── Clothing ──────────────────────────────────────────────────────────
        sb.AppendLine(_loc.Get(Lk.Describer.SectionClothing));

        var worn = character.Clothing.WornItems;
        string legs = FindByTag(worn, "legwear");
        string top = FindByTag(worn, "topwear");
        string feet = FindByTag(worn, "footwear");
        var accessories = worn
            .Where(i => i.Tags.Contains("accessory"))
            .Select(i => i.Name)
            .ToList();

        sb.Append(_loc.Format(Lk.Describer.ClothingBase, legs, top, feet));
        if (accessories.Count > 0)
            sb.Append(_loc.Format(Lk.Describer.ClothingAccessories, JoinWithAnd(accessories)));
        sb.AppendLine();

        // ── Status — read from the shared ConnectionGraph ─────────────────────
        sb.AppendLine(_loc.Get(Lk.Describer.SectionStatus));

        // Family
        var familyConns = graph.From(character.Id).Family().All;
        sb.Append(_loc.Get(Lk.Describer.FamilyPrefix));
        sb.AppendLine(familyConns.Count > 0
            ? string.Join(", ", familyConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                string role = LabelOrType(c);
                string alive = _loc.Get(rel.IsAlive ? Lk.Describer.Alive : Lk.Describer.Deceased);
                return _loc.Format(Lk.Describer.FamilyEntry,
                    role, rel.GetDisplayName(privilegedObserver: true),
                    rel.Appearance.Age.Value, alive);
            }))
            : _loc.Get(Lk.Describer.None));

        // Acquaintances
        var acqConns = graph.From(character.Id).OfType(ConnectionType.Acquaintance).All;
        sb.Append(_loc.Get(Lk.Describer.AcquaintancePrefix));
        sb.AppendLine(acqConns.Count > 0
            ? string.Join(", ", acqConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                return _loc.Format(Lk.Describer.AcquaintanceEntry,
                    rel.GetDisplayName(privilegedObserver: true),
                    rel.Appearance.Age.Value);
            }))
            : _loc.Get(Lk.Describer.None));

        // Partners
        var partnerConns = graph.From(character.Id).Partners().All;
        sb.Append(_loc.Get(Lk.Describer.PartnerPrefix));
        sb.AppendLine(partnerConns.Count > 0
            ? string.Join(", ", partnerConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                return _loc.Format(Lk.Describer.PartnerEntry,
                    rel.GetDisplayName(privilegedObserver: true),
                    LabelOrType(c), rel.Appearance.Age.Value);
            }))
            : _loc.Get(Lk.Describer.None));

        // Occupation
        sb.AppendLine(character.Occupation switch
        {
            null => _loc.Get(Lk.Describer.Unemployed),
            "retired" => _loc.Get(Lk.Describer.Retired),
            var w => _loc.Format(Lk.Describer.Working, w)
        });

        return sb.ToString().TrimEnd();
    }

    // ── Label helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the localised display string for a connection's role.
    /// Uses the typed <see cref="ConnectionLabel"/> when set;
    /// falls back to the localised <see cref="ConnectionType"/> name.
    /// </summary>
    private string LabelOrType(Connection c)
        => c.Label is not null
            ? _loc.GetEnumValue("ConnectionLabel", c.Label.Name)
            : _loc.GetEnumValue("ConnectionType", c.Type.Name);

    private string AgeGroupLabel(int age)
    {
        var cat = AgeCategory.FromAge(age);
        if (cat == AgeCategory.Child) return _loc.GetEnumValue("AgeCategory", AgeCategory.Child.Name);
        if (cat == AgeCategory.Teen) return _loc.GetEnumValue("AgeCategory", AgeCategory.Teen.Name);
        if (cat == AgeCategory.YoungAdult) return _loc.GetEnumValue("AgeCategory", AgeCategory.YoungAdult.Name);
        if (cat == AgeCategory.Adult) return _loc.GetEnumValue("AgeCategory", AgeCategory.Adult.Name);
        if (cat == AgeCategory.MiddleAged) return _loc.GetEnumValue("AgeCategory", AgeCategory.MiddleAged.Name);
        return _loc.GetEnumValue("AgeCategory", AgeCategory.Senior.Name);
    }

    private string GenderLabel(BiologicalGender g)
        => g == BiologicalGender.Male
            ? _loc.GetEnumValue("BiologicalGender", BiologicalGender.Male.Name)
            : _loc.GetEnumValue("BiologicalGender", BiologicalGender.Female.Name);

    private string OrientationLabel(SexualOrientation o)
    {
        if (o == SexualOrientation.Heterosexual) return _loc.GetEnumValue("SexualOrientation", SexualOrientation.Heterosexual.Name);
        if (o == SexualOrientation.Homosexual) return _loc.GetEnumValue("SexualOrientation", SexualOrientation.Homosexual.Name);
        if (o == SexualOrientation.Bisexual) return _loc.GetEnumValue("SexualOrientation", SexualOrientation.Bisexual.Name);
        return _loc.GetEnumValue("SexualOrientation", SexualOrientation.Asexual.Name);
    }

    private string HeightLabel(float cm) => cm switch
    {
        < 160f => _loc.Get(Lk.Height.VeryShort),
        < 167f => _loc.Get(Lk.Height.Short),
        < 172f => _loc.Get(Lk.Height.SlightlyShort),
        < 178f => _loc.Get(Lk.Height.Average),
        < 183f => _loc.Get(Lk.Height.SlightlyTall),
        < 190f => _loc.Get(Lk.Height.Tall),
        _ => _loc.Get(Lk.Height.VeryTall)
    };

    private string BuildLabel(float muscle, float fat)
    {
        bool lowM = muscle < 0.33f;
        bool highM = muscle > 0.66f;
        bool lowF = fat < 0.33f;
        bool highF = fat > 0.66f;

        string key = (lowM, highM, lowF, highF) switch
        {
            (true, _, true, _) => Lk.Build.Skinny,
            (true, _, false, false) => Lk.Build.Thin,
            (true, _, _, true) => Lk.Build.Plump,
            (false, false, true, _) => Lk.Build.Lean,
            (false, false, _, true) => Lk.Build.Stocky,
            (_, true, true, _) => Lk.Build.Ripped,
            (_, true, false, false) => Lk.Build.Muscular,
            (_, true, _, true) => Lk.Build.Brawny,
            _ => Lk.Build.Average
        };
        return _loc.Get(key);
    }

    // ── Clothing helpers ──────────────────────────────────────────────────────

    private static string FindByTag(
        IReadOnlyList<Models.ClothingSpace.ClothingItem> items, string tag)
        => items.FirstOrDefault(i => i.Tags.Contains(tag))?.Name ?? "unknown";

    // ── String helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Joins items with ", " and replaces the final ", " with " and ".
    /// Works for any item count including single-item lists.
    /// </summary>
    private static string JoinWithAnd(IEnumerable<string> items)
    {
        var joined = string.Join(", ", items);
        int last = joined.LastIndexOf(", ", StringComparison.Ordinal);
        return last == -1 ? joined : joined.Remove(last, 2).Insert(last, " and ");
    }
}