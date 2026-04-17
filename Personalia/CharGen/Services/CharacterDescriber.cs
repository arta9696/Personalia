using Personalia.Models;
using Personalia.Models.ConnectionSpace;
using Personalia.Models.Enums;
using System.Text;
using System.Text.RegularExpressions;

namespace Personalia.CharGen.Services;

/// <summary>
/// Formats a <see cref="Character"/> as a human-readable English description.
///
/// Social connections (family, acquaintances, partners) are read directly
/// from <see cref="Character.LifeConnections"/> — no separate DTO is required.
/// Height and build labels are derived on-the-fly from physique model data.
/// </summary>
public sealed partial class CharacterDescriber
{
    /// <summary>
    /// Produces the full description block for <paramref name="character"/>.
    /// </summary>
    public string Describe(Character character)
    {
        var app = character.Appearance;
        var sb = new StringBuilder();

        // ── Resolved values ───────────────────────────────────────────────────
        string firstName = app.FirstName.Value;
        string lastName = app.LastName.Value;
        int age = app.Age.Value;
        string gender = app.BiologicalGender.Value.Name.ToLower();

        var p = app.Physique;
        string skin = FormatName(p.Torso.Organs.Skin.Color.Name);
        string eyeCol = FormatName(p.Head.Eyes.Color.Name);
        string eyeShp = FormatName(p.Head.Eyes.Shape.Name);
        string hairLen = FormatName(p.Head.Hair.Length.Name);
        string hairCol = FormatName(p.Head.Hair.Color.Name);

        // Birthday — read from the typed HiddenValue properties.
        string birthdayMonth = app.BirthdayMonth.Value.Name;
        int birthdayDay = app.BirthdayDay.Value;

        // ── Header ────────────────────────────────────────────────────────────
        sb.AppendLine(
            $"Your name is {firstName} {lastName}. " +
            $"You are {age} years old. " +
            $"Your birthday is {birthdayMonth} {birthdayDay}.");

        // ── Appearance ────────────────────────────────────────────────────────
        sb.AppendLine("Appearance");
        sb.Append(
            $"You are {AgeGroupLabel(age)} {gender}. " +
            $"You are {OrientationLabel(app.SexualOrientation.Value)}. " +
            $"You have {HeightToDescription(p.Torso.Organs.Skeleton.HeightCm)} height " +
            $"and your build would best be described as " +
            $"{BuildToDescription(p.Torso.Organs.Muscles.Volume, p.Torso.Organs.FattyTissue.Volume)}. ");
        sb.AppendLine(
            $"You have {skin} skin, {eyeCol} {eyeShp} eyes " +
            $"and your {hairLen} hair is {hairCol}.");

        if (app.DistinctiveFeatures.Count > 0)
        {
            string suffix = app.DistinctiveFeatures.Count == 1 ? " is " : "s are ";
            sb.AppendLine(
                $"Your most distinguishing feature{suffix}" +
                ReplaceLastOccurrence(string.Join(", ", app.DistinctiveFeatures), ", ", " and ") + ".");
        }

        // ── Clothing ──────────────────────────────────────────────────────────
        sb.AppendLine("Clothing");

        var worn = character.Clothing.WornItems;
        string legs = FindByTag(worn, "legwear");
        string top = FindByTag(worn, "topwear");
        string feet = FindByTag(worn, "footwear");
        var accessories = worn
            .Where(i => i.Tags.Contains("accessory"))
            .Select(i => i.Name)
            .ToList();

        sb.Append($"You are wearing {legs}, {top} and {feet}.");
        if (accessories.Count > 0)
            sb.Append($" You also have {ReplaceLastOccurrence(string.Join(", ", accessories), ", ", " and ")}.");
        sb.AppendLine();

        // ── Status — read from LifeConnections ────────────────────────────────
        sb.AppendLine("Status");

        // All outbound connections from this character, using the typed filter.
        var outbound = character.LifeConnections.From(character.Id).All;

        // Family (CloseFamily + Family connections)
        var familyConns = outbound.Where(c => c.Type.IsFamily).ToList();
        sb.Append("Your family: ");
        sb.AppendLine(familyConns.Count > 0
            ? string.Join(", ", familyConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                string role = c.Label ?? c.Type.DisplayName;
                return $"{role} named {rel.GetDisplayName(privilegedObserver: true)} " +
                       $"(age {rel.Appearance.Age.Value}, {(rel.IsAlive ? "alive" : "deceased")})";
            }))
            : "None");

        // Acquaintances
        var acqConns = outbound.Where(c => c.Type == ConnectionType.Acquaintance).ToList();
        sb.Append("Your acquaintances: ");
        sb.AppendLine(acqConns.Count > 0
            ? string.Join(", ", acqConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                return $"{rel.GetDisplayName(privilegedObserver: true)} (age {rel.Appearance.Age.Value})";
            }))
            : "None");

        // Partners — identified by the "partner" label suffix set during generation
        var partnerConns = outbound
            .Where(c => c.Label is not null && c.Label.EndsWith("partner"))
            .ToList();
        sb.Append("Your partners: ");
        sb.AppendLine(partnerConns.Count > 0
            ? string.Join(", ", partnerConns.Select(c =>
            {
                var rel = c.ToCharacterNode.Character;
                return $"{rel.GetDisplayName(privilegedObserver: true)} ({c.Label}, age {rel.Appearance.Age.Value})";
            }))
            : "None");

        // Work / Occupation
        sb.Append("You're currently ");
        sb.AppendLine(character.Occupation switch
        {
            null => "unemployed.",
            "retired" => "retired.",
            var w => $"working at {w}."
        });

        return sb.ToString().TrimEnd();
    }

    // ── Appearance helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Converts skeleton height in centimetres to a descriptive label.
    /// </summary>
    private static string HeightToDescription(float cm)
    {
        return cm switch
        {
            < 160f => "very short",
            < 167f => "short",
            < 172f => "slightly short",
            < 178f => "average",
            < 183f => "slightly tall",
            < 190f => "tall",
            _ => "very tall"
        };
    }

    /// <summary>
    /// Converts muscle-volume and fat-volume model floats (0–1) to a build label.
    /// </summary>
    private static string BuildToDescription(float muscle, float fat)
    {
        bool lowMuscle = muscle < 0.33f;
        bool highMuscle = muscle > 0.66f;
        bool lowFat = fat < 0.33f;
        bool highFat = fat > 0.66f;

        return (lowMuscle, highMuscle, lowFat, highFat) switch
        {
            (true, _, true, _) => "skinny",
            (true, _, false, false) => "thin",
            (true, _, _, true) => "plump",
            (false, false, true, _) => "lean",
            (false, false, _, true) => "stocky",
            (_, true, true, _) => "ripped",
            (_, true, false, false) => "muscular",
            (_, true, _, true) => "brawny",
            _ => "average build"
        };
    }

    // ── Label mappers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a PascalCase SmartEnum name to a lowercase human-readable phrase.
    /// e.g. "DarkBrown" → "dark brown", "CloseCropped" → "close cropped".
    /// </summary>
    private static string FormatName(string pascalName)
        => CamelCaseSplitter().Replace(pascalName, " ").ToLower();

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelCaseSplitter();

    private static string OrientationLabel(SexualOrientation o)
    {
        if (o == SexualOrientation.Heterosexual) return "straight";
        if (o == SexualOrientation.Homosexual) return "gay";
        if (o == SexualOrientation.Bisexual) return "bisexual";
        if (o == SexualOrientation.Asexual) return "asexual";
        return "unknown";
    }

    /// <summary>
    /// Maps age to a display label by delegating to <see cref="AgeCategory.FromAge"/>.
    /// This keeps the boundary definitions in one place: the domain model.
    /// </summary>
    private static string AgeGroupLabel(int age)
    {
        var category = AgeCategory.FromAge(age);

        if (category == AgeCategory.Child) return "child";
        if (category == AgeCategory.Teen) return "teenager";
        if (category == AgeCategory.YoungAdult) return "young adult";
        if (category == AgeCategory.Adult) return "adult";
        if (category == AgeCategory.MiddleAged) return "middle-aged";
        if (category == AgeCategory.Senior) return "elderly";
        return "unknown";
    }

    // ── Clothing helpers ──────────────────────────────────────────────────────

    private static string FindByTag(
        IReadOnlyList<Models.ClothingSpace.ClothingItem> items, string tag)
        => items.FirstOrDefault(i => i.Tags.Contains(tag))?.Name ?? "unknown";

    // ── String helpers ────────────────────────────────────────────────────────

    private static string ReplaceLastOccurrence(string source, string find, string replace)
    {
        int place = source.LastIndexOf(find);
        if (place == -1) return source;
        return source.Remove(place, find.Length).Insert(place, replace);
    }
}