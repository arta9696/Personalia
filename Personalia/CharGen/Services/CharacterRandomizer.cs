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
/// <see cref="Connection"/>. Completion for queued characters:
///
/// 1. Co-parents are wired as romantic partners (<see cref="LinkCoParentsAsPartners"/>).
/// 2. Orientation is derived from existing partner connections rather than
///    chosen at random (<see cref="SetOrientationFromPartners"/>):
///    • All opposite-gender partners → Heterosexual.
///    • All same-gender partners → Homosexual.
///    • Mixed → weighted heavily toward Bisexual, leaning by majority gender.
///    • No partners → weighted toward Asexual proportionally to age.
/// 3. If the character was created as a parent (has existing son/daughter
///    connections), no additional child connections are generated — those
///    siblings were already wired when the originating child was processed.
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
    private static readonly IReadOnlyDictionary<string, (int Min, int Max)> FamilyRoleAgeDeltas =
        new Dictionary<string, (int, int)>
        {
            ["mother"] = (20, 35),
            ["father"] = (20, 35),
            ["brother"] = (-5, 5),
            ["sister"] = (-5, 5),
            ["son"] = (-35, -20),
            ["daughter"] = (-35, -20)
        };

    private static readonly IReadOnlyList<string> SiblingRoles = ["brother", "sister"];
    private static readonly IReadOnlyList<string> ChildRoles = ["son", "daughter"];

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
            // Gender, name, and age were set by CreateMinimalCharacter.
            // Wire co-parents first so orientation derivation sees those partners.
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
        var outbound = character.LifeConnections.From(character.Id).All;

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
        var partnerConns = character.LifeConnections.All
            .Where(c => c.FromCharacterNode.Character.Id == character.Id
                        && c.Label is not null
                        && c.Label.EndsWith("partner"))
            .ToList();

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

        if (roll < asexualWeight)
            return SexualOrientation.Asexual;

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
    /// and their co-parent(s) — the other parent of each shared child.
    ///
    /// Called early in queued-character completion so that orientation
    /// derivation can observe these partner connections.
    /// Already-partnered pairs are skipped to prevent duplicate connections.
    /// A character may be linked to multiple co-parents if they have children
    /// by different partners (implying half-siblings in the graph).
    /// </summary>
    private void LinkCoParentsAsPartners(Character character)
    {
        string ownGender = character.Appearance.BiologicalGender.Value.Name;

        // Find all children this character was created as a parent for.
        var ownChildren = character.LifeConnections.All
            .Where(c => c.FromCharacterNode.Character.Id == character.Id
                        && c.Type == ConnectionType.CloseFamily
                        && c.Label is ("son" or "daughter"))
            .Select(c => c.ToCharacterNode.Character)
            .ToList();

        foreach (var child in ownChildren)
        {
            // Find the co-parent: the OTHER parent of this child.
            var coParent = child.LifeConnections.All
                .Where(c => c.FromCharacterNode.Character.Id == child.Id
                            && c.Type == ConnectionType.CloseFamily
                            && c.Label is ("mother" or "father")
                            && c.ToCharacterNode.Character.Id != character.Id)
                .Select(c => c.ToCharacterNode.Character)
                .FirstOrDefault();

            if (coParent is null) continue;

            // Skip if already connected as partners.
            bool alreadyPartners = character.LifeConnections.All
                .Any(c => c.FromCharacterNode.Character.Id == character.Id
                          && c.ToCharacterNode.Character.Id == coParent.Id
                          && c.Label is not null && c.Label.EndsWith("partner"));

            if (alreadyPartners) continue;

            // Wire a mutual romantic connection between the co-parents.
            character.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode = new ConnectionNode { Character = coParent },
                Type = ConnectionType.Romantic,
                Label = "romantic partner",
                Strength = ConnectionStrengthPartner
            });
            coParent.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = coParent },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = ConnectionType.Romantic,
                Label = "romantic partner",
                Strength = ConnectionStrengthPartner
            });
        }
    }

    /// <summary>
    /// Generates family members and links them to <paramref name="character"/>.
    ///
    /// Singleton roles (mother, father) are skipped when already wired as
    /// outbound connections — this prevents duplicates and contradictions with
    /// reverse connections added during the child's own generation pass.
    ///
    /// Child roles (son, daughter) have a low probability to be added to the optional
    /// extras when the character already has existing child connections. If a parent 
    /// was created as part of someone else's family, their children are already 
    /// represented by that originating character and their siblings; additional 
    /// generated children are disconnected parallel families (half-siblings in the graph).
    /// </summary>
    private void GenerateFamily(Character character)
    {
        int age = character.Appearance.Age.Value;
        string gender = character.Appearance.BiologicalGender.Value.Name;
        string lastName = character.Appearance.LastName.Value;

        // Collect family roles already wired as outbound connections.
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

        // Child roles are probabalistic when the character already has children:
        // those were generated alongside the originating child character.
        var extras = new List<string>(SiblingRoles);
        bool hasExistingChildren = filledRoles.Contains("son") || filledRoles.Contains("daughter");
        if (age >= MinAgeForChildren && !hasExistingChildren)
        {
            extras.AddRange(ChildRoles);
            while (_rng.NextDouble() < AdditionalFamilyChance)
                roles.Add(extras[_rng.Next(extras.Count)]);
        }
        else if(age >= MinAgeForChildren && hasExistingChildren && _rng.NextDouble() < ParallelChildrenChance)
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
            else if (alive && role is "mother" or "father" && age > ElderlyParentAgeThreshold)
                alive = _rng.NextDouble() < ElderlyParentAliveChance;

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
                Strength = ConnectionStrengthFamily
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
                Strength = ConnectionStrengthFamily
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

            character.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode = new ConnectionNode { Character = acquaintance },
                Type = ConnectionType.Acquaintance,
                Strength = ConnectionStrengthAcquaintance
            });
            acquaintance.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = acquaintance },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = ConnectionType.Acquaintance,
                Strength = ConnectionStrengthAcquaintance
            });
        }
    }

    /// <summary>
    /// Generates partners as minimal <see cref="Character"/> objects and links
    /// them via <see cref="ConnectionType.Romantic"/> (romantic) or
    /// <see cref="ConnectionType.Friend"/> (platonic), labelled
    /// "romantic partner" or "platonic partner".
    ///
    /// Only applies from <see cref="AgeCategory.YoungAdult"/> upward.
    /// Existing partners (e.g. a co-parent wired by <see cref="LinkCoParentsAsPartners"/>)
    /// count toward the age-appropriate maximum, so the total never exceeds
    /// <see cref="MaxPartnersYoungAge"/> / <see cref="MaxPartnersAdultAge"/>.
    /// </summary>
    private void GeneratePartners(Character character)
    {
        int age = character.Appearance.Age.Value;
        string gender = character.Appearance.BiologicalGender.Value.Name;
        var orientation = character.Appearance.SexualOrientation.Value;

        if (AgeCategory.FromAge(age).IsMinor) return;
        if (orientation == SexualOrientation.Asexual && _rng.NextDouble() >= AsexualPartnerChance) return;

        // Respect existing partners created during co-parent linking.
        int existingPartners = character.LifeConnections.All
            .Count(c => c.FromCharacterNode.Character.Id == character.Id
                        && c.Label is not null && c.Label.EndsWith("partner"));

        int maxCount = age < PartnerCountYoungAgeThreshold
            ? MaxPartnersYoungAge
            : MaxPartnersAdultAge;
        maxCount = Math.Max(0, maxCount - existingPartners);

        if (maxCount == 0) return;

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
            bool isRomantic = _rng.NextDouble() < RomanticPartnerChance;
            string label = isRomantic ? "romantic partner" : "platonic partner";

            var partner = CreateMinimalCharacter(
                partnerIsMale ? BiologicalGender.Male : BiologicalGender.Female,
                first: Pick(namePool),
                last: Pick(NamePool.LastNames),
                age: Math.Max(MinPartnerAge, age + _rng.Next(PartnerAgeDeltaMin, PartnerAgeDeltaMax + 1)),
                isAlive: true);

            character.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = character },
                ToCharacterNode = new ConnectionNode { Character = partner },
                Type = isRomantic ? ConnectionType.Romantic : ConnectionType.Friend,
                Label = label,
                Strength = ConnectionStrengthPartner
            });
            partner.LifeConnections.Add(new Connection
            {
                FromCharacterNode = new ConnectionNode { Character = partner },
                ToCharacterNode = new ConnectionNode { Character = character },
                Type = isRomantic ? ConnectionType.Romantic : ConnectionType.Friend,
                Label = label,
                Strength = ConnectionStrengthPartner
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
            return _rng.NextDouble() < TeenWorkChance
                ? Pick(WorkPool.ByAgeGroup[AgeCategory.Teen.Name])
                : null;

        if (category == AgeCategory.Senior)
            return _rng.NextDouble() < SeniorWorkChance
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