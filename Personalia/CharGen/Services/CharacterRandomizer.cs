using Personalia.CharGen.Data;
using Personalia.Models;
using Personalia.Models.AppearanceSpace;
using Personalia.Models.AppearanceSpace.BodyParts;
using Personalia.Models.ClothingSpace;
using Personalia.Models.ClothingSpace.Components;
using Personalia.Models.ConnectionSpace;
using Personalia.Models.Enums;

namespace Personalia.CharGen.Services;

/// <summary>
/// Generates randomised <see cref="GeneratedCharacter"/> instances by
/// populating the Personalia domain model with plausible random data.
///
/// All social relations (family, acquaintances, partners) are represented as
/// first-class <see cref="Character"/> objects and linked through
/// <see cref="Character.LifeConnections"/>, using the appropriate
/// <see cref="ConnectionType"/> and an optional <see cref="Connection.Label"/>
/// for fine-grained role context (e.g. "mother", "romantic partner").
///
/// Clothing is built as fully formed <see cref="ClothingItem"/> objects —
/// with slots and components assigned — before being worn by the character.
/// Plain string names from the data pools are used only because no enum
/// covering clothing-item names exists in the domain model.
/// </summary>
public sealed class CharacterRandomizer
{
    // ── Distribution constants ─────────────────────────────────────────────────

    private const double MeanAge = 35.0;
    private const double StdDevAge = 15.0;
    private const int MinAge = 1;
    private const int MaxAge = 100;

    // Male mean ~175 cm, female ~163 cm; shared standard deviation.
    private const double MaleMeanHeightCm = 175.0;
    private const double FemaleMeanHeightCm = 163.0;
    private const double HeightStdDevCm = 8.0;

    // ── Family role age-delta table ────────────────────────────────────────────

    private static readonly IReadOnlyDictionary<string, (int Min, int Max)> FamilyRoleDeltas =
        new Dictionary<string, (int, int)>
        {
            ["mother"] = (20, 35),
            ["father"] = (20, 35),
            ["brother"] = (-5, 5),
            ["sister"] = (-5, 5),
            ["son"] = (-35, -20),
            ["daughter"] = (-35, -20)
        };

    private readonly Random _rng;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <param name="seed">Optional seed for reproducible output.</param>
    public CharacterRandomizer(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates and returns a fully populated <see cref="Character"/>.
    /// Social connections are stored inside <see cref="Character.LifeConnections"/>.
    /// </summary>
    public Character Generate()
    {
        var character = new Character();
        bool isMale = _rng.Next(2) == 0;
        string gender = isMale ? "Male" : "Female";   // matches BiologicalGender.Name

        SetIdentity(character, isMale);
        SetAge(character);
        SetPhysique(character, isMale);
        SetBirthday(character);
        SetDistinctiveFeatures(character);
        AddClothing(character, gender);
        GenerateFamily(character);
        GenerateAcquaintances(character);
        GeneratePartners(character);

        character.Occupation = GenerateWork(character.Appearance.Age.Value);

        return character;
    }

    // ── Identity ──────────────────────────────────────────────────────────────

    private void SetIdentity(Character character, bool isMale)
    {
        character.Appearance.BiologicalGender =
            new HiddenValue<BiologicalGender>(
                isMale ? BiologicalGender.Male : BiologicalGender.Female);

        // Sexual orientation is hidden by default (matches Appearance defaults).
        var orientations = SexualOrientation.All.ToList();
        character.Appearance.SexualOrientation =
            HiddenValue<SexualOrientation>.Hidden(orientations[_rng.Next(orientations.Count)]);

        var firstPool = isMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
        character.Appearance.FirstName = new HiddenValue<string>(Pick(firstPool));
        character.Appearance.LastName = new HiddenValue<string>(Pick(NamePool.LastNames));
    }

    // ── Age ───────────────────────────────────────────────────────────────────

    private void SetAge(Character character)
    {
        int age = Math.Clamp(
            (int)Math.Round(NextGaussian(MeanAge, StdDevAge)),
            MinAge, MaxAge);
        character.Appearance.Age = new HiddenValue<int>(age);
    }

    // ── Physique ──────────────────────────────────────────────────────────────

    private void SetPhysique(Character character, bool isMale)
    {
        var p = character.Appearance.Physique;

        // Height — gender-biased Gaussian
        double meanH = isMale ? MaleMeanHeightCm : FemaleMeanHeightCm;
        p.Torso.Organs.Skeleton.HeightCm =
            (float)Math.Clamp(NextGaussian(meanH, HeightStdDevCm), 130.0, 220.0);

        p.Torso.Organs.Muscles.Volume = (float)_rng.NextDouble();
        p.Torso.Organs.FattyTissue.Volume = (float)_rng.NextDouble();

        // Skin
        var skinColors = SkinColor.All.ToList();
        p.Torso.Organs.Skin.Color = skinColors[_rng.Next(skinColors.Count)];

        // Eyes
        var eyeColors = EyeColor.All.ToList();
        var eyeShapes = EyeShape.All.ToList();
        p.Head.Eyes.Color = eyeColors[_rng.Next(eyeColors.Count)];
        p.Head.Eyes.Shape = eyeShapes[_rng.Next(eyeShapes.Count)];

        // Hair — exclude meta-category "Dyed" from random selection
        var hairColors = HairColor.All.Where(c => c != HairColor.Dyed).ToList();
        var hairLengths = HairLength.All.ToList();
        var hairShapes = HairShape.All.ToList();
        p.Head.Hair.Color = hairColors[_rng.Next(hairColors.Count)];
        p.Head.Hair.Length = hairLengths[_rng.Next(hairLengths.Count)];
        p.Head.Hair.Shape = hairShapes[_rng.Next(hairShapes.Count)];

        // Other facial features
        var featureShapes = FeatureShape.All.ToList();
        p.Head.Nose.Shape = featureShapes[_rng.Next(featureShapes.Count)];
        p.Head.Ears.Shape = featureShapes[_rng.Next(featureShapes.Count)];
        p.Head.Mouth.Lips.Shape = featureShapes[_rng.Next(featureShapes.Count)];
        p.Head.Mouth.Teeth.Shape = featureShapes[_rng.Next(featureShapes.Count)];
    }

    // ── Birthday ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Picks a random <see cref="Month"/> and a valid day within that month,
    /// storing both as <see cref="HiddenValue{T}"/> on the character's appearance.
    /// </summary>
    private void SetBirthday(Character character)
    {
        var months = Month.All.OrderBy(m => m.Value).ToList();
        var month = months[_rng.Next(months.Count)];
        int day = _rng.Next(1, month.DaysInMonth + 1);

        character.Appearance.BirthdayMonth = new HiddenValue<Month>(month);
        character.Appearance.BirthdayDay = new HiddenValue<int>(day);
    }

    // ── Distinctive features ──────────────────────────────────────────────────

    private void SetDistinctiveFeatures(Character character)
    {
        int count = _rng.Next(3);   // 0, 1, or 2
        if (count == 0) return;

        var types = FeaturePool.DistinctiveFeatures.Keys.ToList();
        Shuffle(types);

        foreach (var type in types.Take(count))
            character.Appearance.DistinctiveFeatures.Add(
                $"{Pick(FeaturePool.DistinctiveFeatures[type])} {type}");
    }

    // ── Clothing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects clothing items from the data pools by name, constructs each as
    /// a fully formed <see cref="ClothingItem"/> (slots + components), and
    /// equips it on the character.  Plain string names are used because the
    /// domain model contains no enum for clothing-item names.
    /// </summary>
    private void AddClothing(Character character, string gender)
    {
        var p = character.Appearance.Physique;

        // Legwear — covers all leg segments bilaterally
        WearItem(character, Pick(ClothingPool.Legwear[gender]), "legwear", conceals: true,
            p.Limbs.LeftLeg.Thigh, p.Limbs.LeftLeg.Shin,
            p.Limbs.RightLeg.Thigh, p.Limbs.RightLeg.Shin);

        // Topwear — covers the torso exterior
        WearItem(character, Pick(ClothingPool.Topwear[gender]), "topwear", conceals: true,
            p.Torso.Chest, p.Torso.Back, p.Torso.Belly, p.Torso.Waist);

        // Footwear — covers both feet
        WearItem(character, Pick(ClothingPool.Footwear[gender]), "footwear", conceals: true,
            p.Limbs.LeftLeg.Foot, p.Limbs.RightLeg.Foot);

        // Accessory — 30 % chance, placed on the wrist slot
        if (_rng.NextDouble() < 0.3)
            WearItem(character, Pick(ClothingPool.Accessories[gender]), "accessory",
                     conceals: false, p.Limbs.LeftArm.Wrist);
    }

    /// <summary>
    /// Constructs a <see cref="ClothingItem"/> with the given body slots and
    /// concealment component, then equips it on the character.
    /// </summary>
    private static void WearItem(
        Character character, string name, string tag,
        bool conceals, params IBodyPart[] slots)
    {
        var item = new ClothingItem
        {
            Name = name,
            Tags = [tag],
            OccupiedSlots = new HashSet<IBodyPart>(slots),
            Components = conceals ? [new SlotConcealment { Conceals = true }] : []
        };
        character.Clothing.Wear(item);
    }

    // ── Social connections ────────────────────────────────────────────────────

    /// <summary>
    /// Generates family members as proper <see cref="Character"/> objects and
    /// links them to <paramref name="character"/> via <see cref="LifeConnections"/>.
    /// <para>
    /// ConnectionType: <see cref="ConnectionType.CloseFamily"/> for parents;
    ///                 <see cref="ConnectionType.Family"/> for siblings / children.
    /// Connection.Label carries the specific role ("mother", "father", etc.).
    /// </para>
    /// </summary>
    private void GenerateFamily(Character character)
    {
        int age = character.Appearance.Age.Value;
        string gender = character.Appearance.BiologicalGender.Value.Name; // "Male" | "Female"
        string lastName = character.Appearance.LastName.Value;

        var roles = new List<string> { "mother", "father" };
        var extras = new List<string> { "brother", "sister" };
        if (age >= 20) extras.AddRange(["son", "daughter"]);

        while (_rng.NextDouble() < 0.3)
            roles.Add(extras[_rng.Next(extras.Count)]);

        foreach (var role in roles)
        {
            var (dMin, dMax) = FamilyRoleDeltas[role];
            int relAge = Math.Max(0, age + _rng.Next(dMin, dMax + 1));

            bool alive = _rng.NextDouble() < 0.9;
            if (relAge > 100) alive = false;
            else if (alive && role is "mother" or "father" && age > 70)
                alive = _rng.NextDouble() < 0.3;

            bool relIsMale = role is "father" or "brother" or "son";
            var namePool = relIsMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;

            bool shareLastName =
                role is "father" or "brother" ||
                (role == "son" && gender == "Male") ||
                (role == "daughter" && gender == "Female");

            var relative = CreateMinimalCharacter(
                relIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first: Pick(namePool),
                last: shareLastName ? lastName : Pick(NamePool.LastNames),
                age: relAge,
                isAlive: alive);

            character.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode = new ConnectionNode { Character = relative },
                Type = ConnectionType.CloseFamily,
                Label = role,
                Strength = 0.8f
            });

            var reverceRole = role switch
            {
                "father" or "mother" => gender == "Male" ? "son" : "daughter",
                "son" or "daughter" => gender == "Male" ? "father" : "mother",
                "brother" or "sister" => gender == "Male" ? "brother" : "sister",
                _ => ""
            };
            relative.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = relative },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = ConnectionType.CloseFamily,
                Label = reverceRole,
                Strength = 0.8f
            });
        }
    }

    /// <summary>
    /// Generates acquaintances as <see cref="Character"/> objects and links them
    /// via <see cref="ConnectionType.Acquaintance"/>.
    /// </summary>
    private void GenerateAcquaintances(Character character)
    {
        int age = character.Appearance.Age.Value;
        int count = _rng.Next(6); // 0–5

        for (int i = 0; i < count; i++)
        {
            bool relIsMale = _rng.Next(2) == 0;
            var namePool = relIsMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
            int relAge = Math.Max(1, age + _rng.Next(-5, 6));

            var acquaintance = CreateMinimalCharacter(
                relIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first: Pick(namePool),
                last: Pick(NamePool.LastNames),
                age: relAge,
                isAlive: true);

            character.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode = new ConnectionNode { Character = acquaintance },
                Type = ConnectionType.Acquaintance,
                Strength = 0.3f
            });
            acquaintance.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = acquaintance },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = ConnectionType.Acquaintance,
                Strength = 0.3f
            });
        }
    }

    /// <summary>
    /// Generates partners as <see cref="Character"/> objects and links them via
    /// <see cref="ConnectionType.Romantic"/> (romantic) or
    /// <see cref="ConnectionType.Friend"/> (platonic), with a
    /// <see cref="Connection.Label"/> of "romantic partner" or "platonic partner".
    /// </summary>
    private void GeneratePartners(Character character)
    {
        int    age         = character.Appearance.Age.Value;
        string gender      = character.Appearance.BiologicalGender.Value.Name;
        var    orientation = character.Appearance.SexualOrientation.Value;
 
        // Partners only apply from YoungAdult upward.
        if (AgeCategory.FromAge(age).IsMinor) return;
        if (orientation == SexualOrientation.Asexual && _rng.NextDouble() >= 0.1) return;
 
        int maxCount = age < 30 ? 1 : 2;
        int count    = _rng.Next(0, maxCount + 1);
 
        for (int i = 0; i < count; i++)
        {
            string partnerGender = orientation switch
            {
                _ when orientation == SexualOrientation.Heterosexual
                    => gender == "Male" ? "Female" : "Male",
                _ when orientation == SexualOrientation.Homosexual
                    => gender,
                _ => _rng.Next(2) == 0 ? "Male" : "Female"
            };
 
            bool   partnerIsMale = partnerGender == "Male";
            var    namePool      = partnerIsMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
            bool   isRomantic    = _rng.NextDouble() < 0.7;
            string label         = isRomantic ? "romantic partner" : "platonic partner";
 
            var partner = CreateMinimalCharacter(
                partnerIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first:   Pick(namePool),
                last:    Pick(NamePool.LastNames),
                age:     Math.Max(18, age + _rng.Next(-5, 6)),
                isAlive: true);
 
            character.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode   = new ConnectionNode { Character = partner },
                Type     = isRomantic ? ConnectionType.Romantic : ConnectionType.Friend,
                Label    = label,
                Strength = 0.9f
            });
            partner.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = partner },
                ToCharacterNode   = new ConnectionNode { Character = character },
                Type     = isRomantic ? ConnectionType.Romantic : ConnectionType.Friend,
                Label    = label,
                Strength = 0.9f
            });
        }
    }

    // ── Work ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives the character's occupation using <see cref="AgeCategory.FromAge"/>
    /// so that age-boundary definitions stay in the domain model.
    /// </summary>
    private string? GenerateWork(int age)
    {
        var category = AgeCategory.FromAge(age);

        if (category == AgeCategory.Child)
            return null;

        if (category == AgeCategory.Teen)
            return _rng.NextDouble() < 0.3
                ? Pick(WorkPool.ByAgeGroup[AgeCategory.Teen.Name])
                : null;

        if (category == AgeCategory.Senior)
            return _rng.NextDouble() < 0.6
                ? Pick(WorkPool.ByAgeGroup[AgeCategory.Senior.Name])
                : "retired";

        // YoungAdult, Adult, MiddleAged — always employed
        return Pick(WorkPool.ByAgeGroup[category.Name]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="Character"/> populated with just the minimum fields
    /// needed to represent a related person (name, age, gender, alive status).
    /// </summary>
    private static Character CreateMinimalCharacter(
        BiologicalGender gender,
        string first, string last,
        int age, bool isAlive)
    {
        var c = new Character { IsAlive = isAlive };
        c.Appearance.BiologicalGender = new HiddenValue<BiologicalGender>(gender);
        c.Appearance.FirstName = new HiddenValue<string>(first);
        c.Appearance.LastName = new HiddenValue<string>(last);
        c.Appearance.Age = new HiddenValue<int>(age);
        return c;
    }

    private T Pick<T>(IReadOnlyList<T> list) => list[_rng.Next(list.Count)];

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>Box-Muller transform — Gaussian sample with given mean and σ.</summary>
    private double NextGaussian(double mean, double stdDev)
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }
}