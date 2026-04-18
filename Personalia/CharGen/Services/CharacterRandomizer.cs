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
/// CharacterRandomizer — generates <see cref="Character"/> instances and populates the
/// shared <see cref="ConnectionGraph"/> with all social edges.
///
/// Queue discipline
/// ────────────────
/// • When the queue is empty a brand-new <see cref="Character"/> is built from
///   scratch (identity, age, physique, clothing, connections, occupation).
/// • After completing any character all alive, not-yet-processed characters found
///   in their outbound connections are appended to the queue, sorted
///   oldest-to-youngest: family connections first, then non-family.
/// • Deceased minimal characters are never enqueued.
/// • Every <see cref="CharacterRandomizer"/> instance owns its own queue and
///   processed-ID set, so multiple generators run independently.
///
/// Sibling–parent wiring fix
/// ────────────────────────────────
/// Immediately after a sibling minimal character is created, <see cref="WireSiblingToParents"/>
/// copies the originating character's parent edges (mother/father) to the new sibling,
/// and writes the reciprocal child edges from the parents back to the sibling.
/// When the sibling is later dequeued, <c>filledRoles</c> already contains
/// <see cref="ConnectionLabel.Mother"/> and <see cref="ConnectionLabel.Father"/>,
/// so <see cref="GenerateFamily"/> skips generating new unrelated parents.
///
/// Queued-character completion
/// ───────────────────────────
/// 1. Co-parents are wired as romantic partners (<see cref="LinkCoParentsAsPartners"/>).
/// 2. Orientation is derived from existing partner connections
///    (<see cref="SetOrientationFromPartners"/>).
/// 3. If the character already has child connections, additional children are generated with very low chance.
/// </summary>
public sealed class CharacterRandomizer
{
    // ── Age distribution ──────────────────────────────────────────────────────

    private const double MeanAge = 35.0;
    private const double StdDevAge = 15.0;
    private const int MinAge = 1;
    private const int MaxAge = 100;

    // ── Height distribution ───────────────────────────────────────────────────

    private const double MaleMeanHeightCm = 175.0;
    private const double FemaleMeanHeightCm = 163.0;
    private const double HeightStdDevCm = 8.0;
    private const double MinHeightCm = 130.0;
    private const double MaxHeightCm = 220.0;

    // ── Family generation ─────────────────────────────────────────────────────

    /// <summary>Minimum age for a character to have child connections generated.</summary>
    private const int MinAgeForChildren = 20;

    /// <summary>Probability of adding one more optional family member per iteration.</summary>
    private const double AdditionalFamilyChance = 0.30;

    /// <summary>Base probability that a generated relative is alive.</summary>
    private const double RelativeAliveBaseChance = 0.90;

    /// <summary>
    /// Probability that an elderly parent is still alive when the character
    /// is older than <see cref="ElderlyParentAgeThreshold"/>.
    /// </summary>
    private const double ElderlyParentAliveChance = 0.30;

    /// <summary>Character age above which parent survival is further reduced.</summary>
    private const int ElderlyParentAgeThreshold = 70;

    /// <summary>Parent probability of having parallel children and thus family.</summary>
    private const double ParallelChildrenChance = 0.05;

    /// <summary>Age-delta table for family roles: (minDelta, maxDelta) relative to character age.</summary>
    private static readonly IReadOnlyDictionary<ConnectionLabel, (int Min, int Max)> FamilyRoleAgeDeltas =
        new Dictionary<ConnectionLabel, (int, int)>
        {
            [ConnectionLabel.Mother] = (20, 35),
            [ConnectionLabel.Father] = (20, 35),
            [ConnectionLabel.Brother] = (-5, 5),
            [ConnectionLabel.Sister] = (-5, 5),
            [ConnectionLabel.Son] = (-35, -20),
            [ConnectionLabel.Daughter] = (-35, -20)
        };

    private static readonly IReadOnlyList<ConnectionLabel> SiblingRoles =
        [ConnectionLabel.Brother, ConnectionLabel.Sister];

    private static readonly IReadOnlyList<ConnectionLabel> ChildRoles =
        [ConnectionLabel.Son, ConnectionLabel.Daughter];

    // ── Acquaintances ─────────────────────────────────────────────────────────

    /// <summary>Upper exclusive bound for random acquaintance count (0 to max-1).</summary>
    private const int MaxAcquaintanceCount = 6;
    private const int AcquaintanceAgeDeltaMin = -5;
    private const int AcquaintanceAgeDeltaMax = 5;

    // ── Partners ──────────────────────────────────────────────────────────────

    /// <summary>Minimum age for any partner generated from scratch.</summary>
    private const int MinPartnerAge = 18;

    /// <summary>Age below which a character may have at most one partner.</summary>
    private const int PartnerCountYoungAgeThreshold = 30;
    private const int MaxPartnersYoungAge = 1;
    private const int MaxPartnersAdultAge = 2;
    private const int PartnerAgeDeltaMin = -5;
    private const int PartnerAgeDeltaMax = 5;

    /// <summary>Probability that a newly generated partner is romantic (vs platonic).</summary>
    private const double RomanticPartnerChance = 0.70;

    /// <summary>
    /// Probability that an asexual character still gets a partner generated.
    /// </summary>
    private const double AsexualPartnerChance = 0.10;

    // ── Clothing ──────────────────────────────────────────────────────────────

    /// <summary>Probability that a character has an accessory item.</summary>
    private const double AccessoryChance = 0.30;

    // ── Work ──────────────────────────────────────────────────────────────────

    /// <summary>Probability that a Teen character is employed.</summary>
    private const double TeenWorkChance = 0.30;

    /// <summary>Probability that a Senior character is still working (vs retired).</summary>
    private const double SeniorWorkChance = 0.60;

    // ── Distinctive features ──────────────────────────────────────────────────

    /// <summary>
    /// Exclusive upper bound for the number of distinctive features per character.
    /// <c>_rng.Next(N)</c> yields 0 … N-1, so this gives 0, 1, or 2 features.
    /// </summary>
    private const int DistinctiveFeatureMaxCount = 3;

    // ── Connection strength ───────────────────────────────────────────────────

    private const float ConnectionStrengthFamily = 0.8f;
    private const float ConnectionStrengthAcquaintance = 0.3f;
    private const float ConnectionStrengthPartner = 0.9f;

    // ── Orientation derivation — no partners ──────────────────────────────────

    /// <summary>Asexual weight at age 0 when a character has no partners.</summary>
    private const double OrientationNoPartnerAsexualBaseWeight = 0.05;

    /// <summary>
    /// Additional asexual weight added at <see cref="MaxAge"/> when a character
    /// has no partners.  Final weight = Base + AgeWeight * (age / MaxAge).
    /// </summary>
    private const double OrientationNoPartnerAsexualAgeWeight = 0.45;

    // ── Orientation derivation — mixed partners ───────────────────────────────

    /// <summary>Base probability of Bisexual when a character has both-gender partners.</summary>
    private const double OrientationMixedBiBaseWeight = 0.60;

    /// <summary>
    /// Additional weight budget distributed proportionally to partner-gender fractions
    /// to produce a Hetero/Homo lean when partners are mixed.
    /// </summary>
    private const double OrientationMixedGenderLeanWeight = 0.40;

    // ── Instance state ────────────────────────────────────────────────────────

    private readonly Random _rng;
    private readonly ConnectionGraph _graph;

    /// <summary>
    /// Characters created as minimal references, waiting to be fully completed.
    /// Sorted oldest-to-youngest (family first, then non-family) when each batch
    /// is appended.
    /// </summary>
    private readonly Queue<Character> _queue = new();

    /// <summary>
    /// Every character ID that has already been enqueued or fully generated,
    /// preventing the same person from being processed twice.
    /// </summary>
    private readonly HashSet<Guid> _processed = [];

    // ── Construction ──────────────────────────────────────────────────────────

    /// <param name="graph">The shared connection graph for this session.</param>
    /// <param name="seed">Optional RNG seed for reproducible output.</param>
    public CharacterRandomizer(ConnectionGraph graph, int? seed = null)
    {
        _graph = graph;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the next fully populated <see cref="Character"/>.
    ///
    /// If the internal queue is non-empty the front character is dequeued and
    /// completed without overwriting already-set fields. Otherwise a brand-new
    /// character is created from scratch.
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
    /// has gender, name, age, and at least one connection set. The method:
    /// <list type="number">
    ///   <item>Links co-parents as romantic partners.</item>
    ///   <item>Derives orientation from existing partners.</item>
    ///   <item>Skips generating new children if the character already has them.</item>
    /// </list>
    /// Brand-new characters receive a randomly chosen orientation instead.
    /// </summary>
    private void CompleteCharacter(Character character, bool isFromQueue)
    {
        bool isMale;

        if (isFromQueue)
        {
            isMale = character.Appearance.BiologicalGender.Value == BiologicalGender.Male;
            LinkCoParentsAsPartners(character);
            SetOrientationFromPartners(character);
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
        var outbound = _graph.From(character.Id);

        var familyCandidates = outbound
            .Family()
            .Alive()
            .All
            .Select(c => c.ToCharacterNode.Character)
            .OrderByDescending(c => c.Appearance.Age.Value);

        var nonFamilyCandidates = outbound
            .NonFamily()
            .Alive()
            .All
            .Select(c => c.ToCharacterNode.Character)
            .OrderByDescending(c => c.Appearance.Age.Value);

        foreach (var candidate in familyCandidates.Concat(nonFamilyCandidates))
            if (_processed.Add(candidate.Id))
                _queue.Enqueue(candidate);
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

        SetOrientationRandom(character);

        var firstPool = isMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
        character.Appearance.FirstName = new HiddenValue<string>(Pick(firstPool));
        character.Appearance.LastName = new HiddenValue<string>(Pick(NamePool.LastNames));
    }

    /// <summary>
    /// Assigns a random <see cref="SexualOrientation"/> (hidden from observers).
    /// Used for brand-new characters who have no social connections yet.
    /// </summary>
    private void SetOrientationRandom(Character character)
    {
        var orientations = SexualOrientation.All.ToList();
        character.Appearance.SexualOrientation =
            HiddenValue<SexualOrientation>.Hidden(orientations[_rng.Next(orientations.Count)]);
    }

    /// <summary>
    /// Derives and assigns <see cref="SexualOrientation"/> from the character's
    /// existing partner connections (hidden from observers).
    ///
    /// Rules:
    /// <list type="bullet">
    ///   <item>All opposite-gender partners → <see cref="SexualOrientation.Heterosexual"/>.</item>
    ///   <item>All same-gender partners → <see cref="SexualOrientation.Homosexual"/>.</item>
    ///   <item>Mixed partners → weighted toward <see cref="SexualOrientation.Bisexual"/>,
    ///         with a lean toward Hetero/Homo proportional to which gender dominates.</item>
    ///   <item>No partners → weighted toward <see cref="SexualOrientation.Asexual"/>,
    ///         with weight rising linearly from
    ///         <see cref="OrientationNoPartnerAsexualBaseWeight"/> at age 0 to
    ///         Base + <see cref="OrientationNoPartnerAsexualAgeWeight"/> at max age.</item>
    /// </list>
    /// </summary>
    private void SetOrientationFromPartners(Character character)
    {
        var partnerConns = _graph.From(character.Id).Partners().All;

        SexualOrientation orientation;

        if (partnerConns.Count == 0)
        {
            orientation = DeriveOrientationNoPartners(character.Appearance.Age.Value);
        }
        else
        {
            string ownGender = character.Appearance.BiologicalGender.Value.Name;
            int sameCount = partnerConns.Count(c =>
                c.ToCharacterNode.Character.Appearance.BiologicalGender.Value.Name == ownGender);
            int oppositeCount = partnerConns.Count - sameCount;

            orientation = (sameCount, oppositeCount) switch
            {
                (0, _) => SexualOrientation.Heterosexual,
                (_, 0) => SexualOrientation.Homosexual,
                _ => DeriveOrientationMixedPartners(sameCount, oppositeCount)
            };
        }

        character.Appearance.SexualOrientation =
            HiddenValue<SexualOrientation>.Hidden(orientation);
    }

    /// <summary>
    /// Returns a random orientation weighted toward Asexual proportionally to age
    /// when a character has no partner history.
    /// </summary>
    private SexualOrientation DeriveOrientationNoPartners(int age)
    {
        double asexualWeight = OrientationNoPartnerAsexualBaseWeight
            + OrientationNoPartnerAsexualAgeWeight * ((double)age / MaxAge);

        double roll = _rng.NextDouble();
        if (roll < asexualWeight) return SexualOrientation.Asexual;

        // Remaining probability split equally among the other three orientations.
        double each = (1.0 - asexualWeight) / 3.0;
        double pos = roll - asexualWeight;
        if (pos < each) return SexualOrientation.Heterosexual;
        if (pos < each * 2.0) return SexualOrientation.Homosexual;
        return SexualOrientation.Bisexual;
    }

    /// <summary>
    /// Returns a random orientation for a character with mixed-gender partners.
    /// Bisexual receives a base weight; Hetero/Homo receive additional weight
    /// proportional to which partner gender dominates.
    /// </summary>
    private SexualOrientation DeriveOrientationMixedPartners(int sameCount, int oppositeCount)
    {
        double total = sameCount + oppositeCount;
        double biWeight = OrientationMixedBiBaseWeight;
        double heteroWeight = (oppositeCount / total) * OrientationMixedGenderLeanWeight;
        double homoWeight = (sameCount / total) * OrientationMixedGenderLeanWeight;
        double weightSum = biWeight + heteroWeight + homoWeight;

        double roll = _rng.NextDouble() * weightSum;
        if (roll < biWeight) return SexualOrientation.Bisexual;
        if (roll < biWeight + heteroWeight) return SexualOrientation.Heterosexual;
        return SexualOrientation.Homosexual;
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
            (float)Math.Clamp(NextGaussian(meanH, HeightStdDevCm), MinHeightCm, MaxHeightCm);

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
        int count = _rng.Next(DistinctiveFeatureMaxCount);
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

        // Accessory — placed on the wrist slot when selected
        if (_rng.NextDouble() < AccessoryChance)
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
    /// Wires a mutual romantic connection between <paramref name="character"/>
    /// and each of their co-parents (the other parent of each shared child).
    /// Already-partnered pairs are skipped to prevent duplicates.
    /// </summary>
    private void LinkCoParentsAsPartners(Character character)
    {
        var ownChildren = _graph.From(character.Id).Children()
            .All
            .Select(c => c.ToCharacterNode.Character)
            .ToList();

        foreach (var child in ownChildren)
        {
            var coParent = _graph.From(child.Id).Parents(excludeCharacterId: character.Id)
                .All
                .Select(c => c.ToCharacterNode.Character)
                .FirstOrDefault();

            if (coParent is null) continue;

            bool alreadyPartners = _graph.From(character.Id).Partners()
                .All
                .Any(c => c.ToCharacterNode.Character.Id == coParent.Id);

            if (alreadyPartners) continue;

            AddMutualPartnerConnection(character, coParent,
                label: ConnectionLabel.RomanticPartner, isRomantic: true);
        }
    }

    /// <summary>
    /// Generates family members and links them to <paramref name="character"/>.
    ///
    /// Singleton roles (mother, father) are skipped when already wired as outbound
    /// connections, preventing contradictions with reverse connections added during
    /// a child's own generation pass.
    ///
    /// After each sibling is created, <see cref="WireSiblingToParents"/> propagates
    /// the originating character's parent edges to that sibling so they share the
    /// same parents in the graph when later dequeued.
    /// </summary>
    private void GenerateFamily(Character character)
    {
        int age = character.Appearance.Age.Value;
        var gender = character.Appearance.BiologicalGender.Value;
        string lastName = character.Appearance.LastName.Value;

        // Collect family roles already wired as outbound connections.
        var filledRoles = new HashSet<ConnectionLabel>(
            _graph.From(character.Id).Family().WithLabel().All
                .Select(c => c.Label!));

        var roles = new List<ConnectionLabel>();
        if (!filledRoles.Contains(ConnectionLabel.Mother)) roles.Add(ConnectionLabel.Mother);
        if (!filledRoles.Contains(ConnectionLabel.Father)) roles.Add(ConnectionLabel.Father);

        var extras = new List<ConnectionLabel>(SiblingRoles);
        bool hasExistingChildren =
            filledRoles.Contains(ConnectionLabel.Son) ||
            filledRoles.Contains(ConnectionLabel.Daughter);

        if (age >= MinAgeForChildren && !hasExistingChildren)
        {
            extras.AddRange(ChildRoles);
            while (_rng.NextDouble() < AdditionalFamilyChance)
                roles.Add(extras[_rng.Next(extras.Count)]);
        }
        else if (age >= MinAgeForChildren && hasExistingChildren
                 && _rng.NextDouble() < ParallelChildrenChance)
        {
            while (_rng.NextDouble() < AdditionalFamilyChance)
                roles.Add(extras[_rng.Next(extras.Count)]);
            roles.Add(ChildRoles[_rng.Next(ChildRoles.Count)]);
        }

        foreach (var role in roles)
        {
            var (dMin, dMax) = FamilyRoleAgeDeltas[role];
            int relAge = Math.Max(0, age + _rng.Next(dMin, dMax + 1));

            bool alive = _rng.NextDouble() < RelativeAliveBaseChance;
            if (relAge > MaxAge) alive = false;
            else if (alive && role.IsParent && age > ElderlyParentAgeThreshold)
                alive = _rng.NextDouble() < ElderlyParentAliveChance;

            bool relIsMale = role == ConnectionLabel.Father
                          || role == ConnectionLabel.Brother
                          || role == ConnectionLabel.Son;

            var namePool = relIsMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;

            bool shareLastName =
                role == ConnectionLabel.Father ||
                role == ConnectionLabel.Brother ||
                (role == ConnectionLabel.Son && gender == BiologicalGender.Male) ||
                (role == ConnectionLabel.Daughter && gender == BiologicalGender.Female);

            var relative = CreateMinimalCharacter(
                relIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first: Pick(namePool),
                last: shareLastName ? lastName : Pick(NamePool.LastNames),
                age: relAge,
                isAlive: alive);

            ConnectionLabel reverseRole;
            if (role.IsParent)
                reverseRole = gender == BiologicalGender.Male ? ConnectionLabel.Son : ConnectionLabel.Daughter;
            else if (role.IsChild)
                reverseRole = gender == BiologicalGender.Male ? ConnectionLabel.Father : ConnectionLabel.Mother;
            else
                reverseRole = gender == BiologicalGender.Male ? ConnectionLabel.Brother : ConnectionLabel.Sister;

            AddFamilyConnection(character, relative, role, reverseRole);

            // ── Sibling–parent wiring fix ─────────────────────────────────────
            // Blood siblings share the same parents. Copy the originating
            // character's parent edges (mother/father) to the new sibling so
            // that when the sibling is later dequeued it does not generate a
            // second, unrelated set of parents.
            if (role.IsSibling)
                WireSiblingToParents(character, relative, relIsMale);
        }
    }

    /// <summary>
    /// Propagates <paramref name="originator"/>'s outbound parent edges
    /// (mother / father) to <paramref name="sibling"/> and writes the
    /// corresponding reciprocal child edges from each parent back to the sibling.
    ///
    /// This ensures blood siblings share exactly the same parent nodes in the graph.
    /// The sibling's gender is used to pick the correct reverse label (son/daughter).
    /// </summary>
    private void WireSiblingToParents(Character originator, Character sibling, bool siblingIsMale)
    {
        var parentConns = _graph.From(originator.Id).Parents().All;

        foreach (var parentConn in parentConns)
        {
            var parent = parentConn.ToCharacterNode.Character;

            // Skip if the sibling is already connected to this parent.
            bool alreadyLinked = _graph.From(sibling.Id).Parents().All
                .Any(c => c.ToCharacterNode.Character.Id == parent.Id);
            if (alreadyLinked) continue;

            ConnectionLabel parentRole = parentConn.Label!;  // Mother or Father
            ConnectionLabel siblingRole = siblingIsMale ? ConnectionLabel.Son : ConnectionLabel.Daughter;

            // sibling → parent
            _graph.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = sibling },
                ToCharacterNode = new ConnectionNode { Character = parent },
                Type = ConnectionType.CloseFamily,
                Label = parentRole,
                Strength = ConnectionStrengthFamily
            });

            // parent → sibling
            _graph.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = parent },
                ToCharacterNode = new ConnectionNode { Character = sibling },
                Type = ConnectionType.CloseFamily,
                Label = siblingRole,
                Strength = ConnectionStrengthFamily
            });
        }
    }

    /// <summary>
    /// Generates acquaintances as minimal characters and links them via
    /// <see cref="ConnectionType.Acquaintance"/> in both directions.
    /// </summary>
    private void GenerateAcquaintances(Character character)
    {
        int age = character.Appearance.Age.Value;
        int count = _rng.Next(MaxAcquaintanceCount);

        for (int i = 0; i < count; i++)
        {
            bool relIsMale = _rng.Next(2) == 0;
            var namePool = relIsMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
            int relAge = Math.Max(1,
                age + _rng.Next(AcquaintanceAgeDeltaMin, AcquaintanceAgeDeltaMax + 1));

            var acquaintance = CreateMinimalCharacter(
                relIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first: Pick(namePool),
                last: Pick(NamePool.LastNames),
                age: relAge,
                isAlive: true);

            _graph.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode = new ConnectionNode { Character = acquaintance },
                Type = ConnectionType.Acquaintance,
                Strength = ConnectionStrengthAcquaintance
            });
            _graph.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = acquaintance },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = ConnectionType.Acquaintance,
                Strength = ConnectionStrengthAcquaintance
            });
        }
    }

    /// <summary>
    /// Generates partners as minimal characters and links them via
    /// <see cref="ConnectionType.Romantic"/> or <see cref="ConnectionType.Friend"/>,
    /// labelled <see cref="ConnectionLabel.RomanticPartner"/> or
    /// <see cref="ConnectionLabel.PlatonicPartner"/>.
    ///
    /// Existing partners (e.g. a co-parent wired by <see cref="LinkCoParentsAsPartners"/>)
    /// count toward the age-appropriate maximum.
    /// </summary>
    private void GeneratePartners(Character character)
    {
        int age = character.Appearance.Age.Value;
        var gender = character.Appearance.BiologicalGender.Value;
        var orientation = character.Appearance.SexualOrientation.Value;

        if (AgeCategory.FromAge(age).IsMinor) return;
        if (orientation == SexualOrientation.Asexual
            && _rng.NextDouble() >= AsexualPartnerChance) return;

        int existingPartners = _graph.From(character.Id).Partners().Count;

        int maxCount = age < PartnerCountYoungAgeThreshold
            ? MaxPartnersYoungAge
            : MaxPartnersAdultAge;
        maxCount = Math.Max(0, maxCount - existingPartners);
        if (maxCount == 0) return;

        int count = _rng.Next(0, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            var partnerGender = orientation switch
            {
                _ when orientation == SexualOrientation.Heterosexual
                    => gender == BiologicalGender.Male ? BiologicalGender.Female : BiologicalGender.Male,
                _ when orientation == SexualOrientation.Homosexual
                    => gender,
                _ => _rng.Next(2) == 0 ? BiologicalGender.Male : BiologicalGender.Female
            };

            bool partnerIsMale = partnerGender == BiologicalGender.Male;
            var namePool = partnerIsMale ? NamePool.MaleFirstNames : NamePool.FemaleFirstNames;
            bool isRomantic = _rng.NextDouble() < RomanticPartnerChance;
            var label = isRomantic ? ConnectionLabel.RomanticPartner : ConnectionLabel.PlatonicPartner;

            var partner = CreateMinimalCharacter(
                partnerIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first: Pick(namePool),
                last: Pick(NamePool.LastNames),
                age: Math.Max(MinPartnerAge,
                                  age + _rng.Next(PartnerAgeDeltaMin, PartnerAgeDeltaMax + 1)),
                isAlive: true);

            AddMutualPartnerConnection(character, partner, label, isRomantic);
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

        if (category == AgeCategory.Child) return null;

        if (category == AgeCategory.Teen)
            return _rng.NextDouble() < TeenWorkChance
                ? Pick(WorkPool.ByAgeGroup[AgeCategory.Teen.Name])
                : null;

        if (category == AgeCategory.Senior)
            return _rng.NextDouble() < SeniorWorkChance
                ? Pick(WorkPool.ByAgeGroup[AgeCategory.Senior.Name])
                : "retired";

        return Pick(WorkPool.ByAgeGroup[category.Name]);
    }

    // ── Connection helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Adds a directed CloseFamily edge from <paramref name="character"/> to
    /// <paramref name="relative"/> and the reciprocal edge in the opposite direction.
    /// </summary>
    private void AddFamilyConnection(
        Character character, Character relative,
        ConnectionLabel role, ConnectionLabel reverseRole)
    {
        _graph.Add(new Connection
        {
            FromCharacterNode = new ConnectionNode { Character = character },
            ToCharacterNode = new ConnectionNode { Character = relative },
            Type = ConnectionType.CloseFamily,
            Label = role,
            Strength = ConnectionStrengthFamily
        });
        _graph.Add(new Connection
        {
            FromCharacterNode = new ConnectionNode { Character = relative },
            ToCharacterNode = new ConnectionNode { Character = character },
            Type = ConnectionType.CloseFamily,
            Label = reverseRole,
            Strength = ConnectionStrengthFamily
        });
    }

    /// <summary>
    /// Adds mutual partner edges between <paramref name="a"/> and <paramref name="b"/>.
    /// </summary>
    private void AddMutualPartnerConnection(
        Character a, Character b, ConnectionLabel label, bool isRomantic)
    {
        var type = isRomantic ? ConnectionType.Romantic : ConnectionType.Friend;

        _graph.Add(new Connection
        {
            FromCharacterNode = new ConnectionNode { Character = a },
            ToCharacterNode = new ConnectionNode { Character = b },
            Type = type,
            Label = label,
            Strength = ConnectionStrengthPartner
        });
        _graph.Add(new Connection
        {
            FromCharacterNode = new ConnectionNode { Character = b },
            ToCharacterNode = new ConnectionNode { Character = a },
            Type = type,
            Label = label,
            Strength = ConnectionStrengthPartner
        });
    }

    // ── Minimal character factory ─────────────────────────────────────────────

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