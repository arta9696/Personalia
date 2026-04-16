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
/// Generates <see cref="Character"/> instances and maintains a per-instance
/// queue of partially-initialised characters so that every person referenced
/// as a social connection eventually receives a full profile.
///
/// Queue discipline
/// ────────────────
/// • When the queue is empty a brand-new <see cref="Character"/> is built from
///   scratch (identity, age, physique, clothing, connections, occupation).
/// • After completing any character — new or queued — all alive, not-yet-
///   processed characters found in its outbound connections are appended to the
///   queue, sorted oldest-to-youngest: family connections first, then non-family.
/// • Deceased minimal characters are never enqueued.
/// • Every <see cref="CharacterRandomizer"/> instance owns its own queue and
///   processed-ID set, so multiple generators run independently.
///
/// Queued-character completion
/// ───────────────────────────
/// A minimal character already carries: <see cref="BiologicalGender"/>,
/// <see cref="Appearance.FirstName"/>, <see cref="Appearance.LastName"/>,
/// <see cref="Appearance.Age"/>, and at least one reverse
/// <see cref="Connection"/>. Completion preserves all of these and only fills
/// in missing data (orientation, physique, birthday, features, clothing,
/// additional connections, occupation). <see cref="GenerateFamily"/> inspects
/// existing outbound family connections before adding new roles, preventing
/// contradictions such as a second mother or a duplicate parent that was
/// already wired as a reverse connection.
/// </summary>
public sealed class CharacterRandomizer
{
    // ── Distribution constants ────────────────────────────────────────────────

    private const double MeanAge = 35.0;
    private const double StdDevAge = 15.0;
    private const int MinAge = 1;
    private const int MaxAge = 100;

    // Male mean ~175 cm, female ~163 cm; shared standard deviation.
    private const double MaleMeanHeightCm = 175.0;
    private const double FemaleMeanHeightCm = 163.0;
    private const double HeightStdDevCm = 8.0;

    // ── Family role age-delta table ───────────────────────────────────────────

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

    // ── Instance state ────────────────────────────────────────────────────────

    private readonly Random _rng;

    /// <summary>
    /// Characters that have been created as minimal references and are
    /// waiting to be fully completed on a future <see cref="Generate"/> call.
    /// Sorted oldest-to-youngest (family first, then non-family) at the time
    /// each batch is appended.
    /// </summary>
    private readonly Queue<Character> _queue = new();

    /// <summary>
    /// Tracks every character ID that has already been enqueued or fully
    /// generated, preventing the same person from being enqueued twice.
    /// </summary>
    private readonly HashSet<Guid> _processed = new();

    // ── Construction ──────────────────────────────────────────────────────────

    /// <param name="seed">Optional RNG seed for reproducible output.</param>
    public CharacterRandomizer(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the next fully populated <see cref="Character"/>.
    ///
    /// If the internal queue is non-empty the front character (a previously
    /// created minimal character) is dequeued and completed without overwriting
    /// already-set fields. Otherwise a brand-new character is created from
    /// scratch. In both cases all alive, not-yet-processed connections produced
    /// during completion are sorted and appended to the queue.
    /// </summary>
    public Character Generate()
    {
        bool isFromQueue = _queue.Count > 0;
        Character character = isFromQueue ? _queue.Dequeue() : new Character();

        // Mark as processed before completing so that reverse connections
        // pointing back to already-processed characters are never re-enqueued.
        _processed.Add(character.Id);

        CompleteCharacter(character, isFromQueue);
        EnqueueMinimalCharacters(character);

        return character;
    }

    /// <summary>Number of characters currently waiting in the queue.</summary>
    public int QueueCount => _queue.Count;

    // ── Character completion ──────────────────────────────────────────────────

    /// <summary>
    /// Fully populates <paramref name="character"/>.
    ///
    /// When <paramref name="isFromQueue"/> is <c>true</c> the character already
    /// has gender, name, age, and at least one connection set; those fields are
    /// preserved. Only the missing pieces (orientation, physique, birthday,
    /// features, clothing, additional connections, occupation) are filled in.
    /// </summary>
    private void CompleteCharacter(Character character, bool isFromQueue)
    {
        bool isMale;

        if (isFromQueue)
        {
            // Minimal character: gender / name / age already set — derive isMale
            // and assign only the missing orientation.
            isMale = character.Appearance.BiologicalGender.Value == BiologicalGender.Male;
            SetOrientation(character);
        }
        else
        {
            // Brand-new character: assign full identity and age first.
            isMale = _rng.Next(2) == 0;
            SetIdentity(character, isMale);
            SetAge(character);
        }

        string gender = isMale ? "Male" : "Female";

        SetPhysique(character, isMale);
        SetBirthday(character);
        SetDistinctiveFeatures(character);
        AddClothing(character, gender);
        GenerateFamily(character);
        GenerateAcquaintances(character);
        GeneratePartners(character);
        character.Occupation = GenerateWork(character.Appearance.Age.Value);
    }

    // ── Queue management ──────────────────────────────────────────────────────

    /// <summary>
    /// Collects all alive, not-yet-processed characters from
    /// <paramref name="character"/>'s outbound connections, sorts them
    /// (older-to-youngest family first, then older-to-youngest non-family),
    /// and appends each to the queue exactly once.
    /// </summary>
    private void EnqueueMinimalCharacters(Character character)
    {
        var outbound = character.LifeConnections
            .From(character.Id)
            .ToList();

        IEnumerable<Character> familyCandidates = outbound
            .Where(c => c.Type.IsFamily)
            .Select(c => c.ToCharacterNode.Character)
            .Where(c => c.IsAlive)
            .OrderByDescending(c => c.Appearance.Age.Value);

        IEnumerable<Character> nonFamilyCandidates = outbound
            .Where(c => !c.Type.IsFamily)
            .Select(c => c.ToCharacterNode.Character)
            .Where(c => c.IsAlive)
            .OrderByDescending(c => c.Appearance.Age.Value);

        foreach (Character candidate in familyCandidates.Concat(nonFamilyCandidates))
        {
            // _processed.Add returns false if the ID is already present,
            // so each character is enqueued at most once across all passes.
            if (_processed.Add(candidate.Id))
                _queue.Enqueue(candidate);
        }
    }

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets all identity fields (gender, orientation, first name, last name)
    /// for a brand-new character. Not called for queued characters, whose
    /// identity fields were set by <see cref="CreateMinimalCharacter"/>.
    /// </summary>
    private void SetIdentity(Character character, bool isMale)
    {
        character.Appearance.BiologicalGender =
            new HiddenValue<BiologicalGender>(
                isMale ? BiologicalGender.Male : BiologicalGender.Female);

        SetOrientation(character);

        var firstPool = isMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
        character.Appearance.FirstName = new HiddenValue<string>(Pick(firstPool));
        character.Appearance.LastName = new HiddenValue<string>(Pick(NamePool.LastNames));
    }

    /// <summary>
    /// Assigns a random hidden <see cref="SexualOrientation"/>.
    /// Called for both brand-new characters (via <see cref="SetIdentity"/>) and
    /// queued characters (directly from <see cref="CompleteCharacter"/>), because
    /// <see cref="CreateMinimalCharacter"/> does not set orientation.
    /// </summary>
    private void SetOrientation(Character character)
    {
        var orientations = SexualOrientation.All.ToList();
        character.Appearance.SexualOrientation =
            HiddenValue<SexualOrientation>.Hidden(orientations[_rng.Next(orientations.Count)]);
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
    /// equips it on the character. Plain string names are used because the
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
    /// Generates family members and links them to <paramref name="character"/>.
    ///
    /// Before adding any role the method builds a <c>filledRoles</c> set from
    /// the character's existing outbound family connections. This prevents:
    /// <list type="bullet">
    ///   <item>Duplicate roles — e.g., a second mother.</item>
    ///   <item>Contradictory roles — e.g., a new father added when the character
    ///         was already created as someone else's son and carries a reverse
    ///         "father" connection back to that character.</item>
    /// </list>
    /// Sibling and child roles (brother / sister / son / daughter) are not
    /// treated as unique singletons, so multiples remain possible.
    /// </summary>
    private void GenerateFamily(Character character)
    {
        int age = character.Appearance.Age.Value;
        string gender = character.Appearance.BiologicalGender.Value.Name;
        string lastName = character.Appearance.LastName.Value;

        // Collect family roles that are already wired up as outbound connections
        // (this includes reverse-role connections added when the character was
        // first created as someone else's relative).
        var filledRoles = new HashSet<string>(
            character.LifeConnections.All
                .Where(c => c.FromCharacterNode.Character.Id == character.Id
                            && c.Type.IsFamily
                            && c.Label is not null)
                .Select(c => c.Label!),
            StringComparer.OrdinalIgnoreCase);

        // Mandatory singleton roles — add only when not already filled.
        var roles = new List<string>();
        if (!filledRoles.Contains("mother")) roles.Add("mother");
        if (!filledRoles.Contains("father")) roles.Add("father");

        // Optional repeatable extras (siblings and children).
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

            string reverseRole = role switch
            {
                "father" or "mother" => gender == "Male" ? "son" : "daughter",
                "son" or "daughter" => gender == "Male" ? "father" : "mother",
                "brother" or "sister" => gender == "Male" ? "brother" : "sister",
                _ => string.Empty
            };

            relative.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = relative },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = ConnectionType.CloseFamily,
                Label = reverseRole,
                Strength = 0.8f
            });
        }
    }

    /// <summary>
    /// Generates acquaintances as minimal <see cref="Character"/> objects and
    /// links them via <see cref="ConnectionType.Acquaintance"/>.
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
    /// Generates partners as minimal <see cref="Character"/> objects and links
    /// them via <see cref="ConnectionType.Romantic"/> (romantic) or
    /// <see cref="ConnectionType.Friend"/> (platonic), with a
    /// <see cref="Connection.Label"/> of "romantic partner" or "platonic partner".
    /// Only applies from <see cref="AgeCategory.YoungAdult"/> upward.
    /// </summary>
    private void GeneratePartners(Character character)
    {
        int age = character.Appearance.Age.Value;
        string gender = character.Appearance.BiologicalGender.Value.Name;
        var orientation = character.Appearance.SexualOrientation.Value;

        if (AgeCategory.FromAge(age).IsMinor) return;
        if (orientation == SexualOrientation.Asexual && _rng.NextDouble() >= 0.1) return;

        int maxCount = age < 30 ? 1 : 2;
        int count = _rng.Next(0, maxCount + 1);

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

            bool partnerIsMale = partnerGender == "Male";
            var namePool = partnerIsMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
            bool isRomantic = _rng.NextDouble() < 0.7;
            string label = isRomantic ? "romantic partner" : "platonic partner";

            var partner = CreateMinimalCharacter(
                partnerIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first: Pick(namePool),
                last: Pick(NamePool.LastNames),
                age: Math.Max(18, age + _rng.Next(-5, 6)),
                isAlive: true);

            character.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode = new ConnectionNode { Character = partner },
                Type = isRomantic ? ConnectionType.Romantic : ConnectionType.Friend,
                Label = label,
                Strength = 0.9f
            });
            partner.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = partner },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = isRomantic ? ConnectionType.Romantic : ConnectionType.Friend,
                Label = label,
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

        // YoungAdult, Adult, MiddleAged — always employed.
        return Pick(WorkPool.ByAgeGroup[category.Name]);
    }

    // ── Minimal character factory ─────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="Character"/> populated with the minimum fields
    /// needed to represent a social connection: gender, name, age, alive-status.
    ///
    /// Orientation, physique, birthday, features, clothing, additional
    /// connections, and occupation are intentionally left at their defaults;
    /// they will be filled in by <see cref="CompleteCharacter"/> if and when
    /// this character is dequeued.
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

    // ── RNG helpers ───────────────────────────────────────────────────────────

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