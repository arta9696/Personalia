using Personalia.Common;

namespace Personalia.Localization.Ru;

/// <summary>
/// RussianLocalizationProvider — Cyrillic locale for <c>CharacterDescriber</c> output.
///
/// Grammatical context (<see cref="LocalizationContext"/>) is used to resolve
/// gender-inflected variants of adjectives and nouns. When a
/// <see cref="LocalizationContext"/> is received, the provider first looks for
/// a context-specific dictionary entry using the key-suffix convention
/// <c>"{TypeName}.{ValueName}.{Context}"</c>
/// (e.g. <c>"AgeCategory.Adult.Female"</c>), then falls back to the base key.
///
/// Contexts are created exclusively through the provider's factory methods
/// , keeping callers locale-agnostic. The age-number rules (singular / dual / plural) are
/// encapsulated so they do not leak into presentation code.
/// </summary>
public sealed class RussianLocalizationProvider : ILocalizationProvider
{
    private static readonly IReadOnlyDictionary<string, string> _strings =
        new Dictionary<string, string>
        {
            // ── Describer ─────────────────────────────────────────────────────
            ["Describer.Header"]
                = "Ваше имя {0} {1}. Вам {2} лет. День рождения: {3} {4}.",
            ["Describer.Header.Singular"]
                = "Ваше имя {0} {1}. Вам {2} год. День рождения: {3} {4}.",
            ["Describer.Header.Dual"]
                = "Ваше имя {0} {1}. Вам {2} года. День рождения: {3} {4}.",
            ["Describer.SectionAppearance"] = "Внешность",
            ["Describer.AppearanceLine1"]
                = "Вы {0} {1}. Вы {2}. У вас {3} рост и {4} телосложение. ",
            ["Describer.AppearanceLine2"]
                = "У вас {0} кожа, {1} {2} глаза, а ваши {3} волосы имеют {4} цвет.",
            ["Describer.DistinctiveFeature"] = "Ваша наиболее выделяющаяся черта — {0}.",
            ["Describer.DistinctiveFeatures"] = "Ваши наиболее выделяющиеся черты — {0}.",
            ["Describer.SectionClothing"] = "Одежда",
            ["Describer.ClothingBase"] = "Вы одеты в {0}, {1} и {2}.",
            ["Describer.ClothingAccessories"] = " Также у вас есть {0}.",
            ["Describer.SectionStatus"] = "Статус",
            ["Describer.FamilyPrefix"] = "Ваша семья: ",
            ["Describer.AcquaintancePrefix"] = "Ваши знакомые: ",
            ["Describer.PartnerPrefix"] = "Ваши партнёры: ",
            ["Describer.None"] = "Нет",
            ["Describer.FamilyEntry"] = "{0} по имени {1} (возраст {2}, {3})",
            ["Describer.AcquaintanceEntry"] = "{0} (возраст {1})",
            ["Describer.PartnerEntry"] = "{0} ({1}, возраст {2})",
            ["Describer.Alive"] = "живой",
            ["Describer.Alive.Female"] = "живая",
            ["Describer.Deceased"] = "мертв",
            ["Describer.Deceased.Female"] = "мертва",
            ["Describer.Unemployed"] = "В данный момент у Вас нет работы.",
            ["Describer.Retired"] = "В данный момент вы на пенсии.",
            ["Describer.Working"] = "В данный момент вы работаете в {0}.",

            // ── Height (agrees with "рост", masculine noun — no gender context needed) ─
            ["Height.VeryShort"] = "очень низкий",
            ["Height.Short"] = "низкий",
            ["Height.SlightlyShort"] = "чуть ниже среднего",
            ["Height.Average"] = "средний",
            ["Height.SlightlyTall"] = "чуть выше среднего",
            ["Height.Tall"] = "высокий",
            ["Height.VeryTall"] = "очень высокий",

            // ── Build (agrees with "телосложение", neuter noun — no gender context needed) ─
            ["Build.Skinny"] = "худощавое",
            ["Build.Thin"] = "стройное",
            ["Build.Plump"] = "пухлое",
            ["Build.Lean"] = "поджарое",
            ["Build.Stocky"] = "коренастое",
            ["Build.Ripped"] = "рельефное",
            ["Build.Muscular"] = "мускулистое",
            ["Build.Brawny"] = "мощное",
            ["Build.Average"] = "среднее",
        };

    private static readonly IReadOnlyDictionary<string, string> _enumValues =
        new Dictionary<string, string>
        {
            // ── Age categories — base (masculine/neutral) + feminine overrides ─────────
            // Used in "Вы {ageGroup} {gender}." so the adjective must agree with gender.
            ["AgeCategory.Child"] = "ребёнок",                  // gender-neutral noun
            ["AgeCategory.Teen"] = "подросток",                 // gender-neutral noun
            ["AgeCategory.YoungAdult"] = "молодой",             // masculine
            ["AgeCategory.YoungAdult.Female"] = "молодая",      // feminine override
            ["AgeCategory.Adult"] = "взрослый",                 // masculine
            ["AgeCategory.Adult.Female"] = "взрослая",          // feminine override
            ["AgeCategory.MiddleAged"] = "среднего возраста",   // invariant (genitive phrase)
            ["AgeCategory.Senior"] = "пожилой",                 // masculine
            ["AgeCategory.Senior.Female"] = "пожилая",          // feminine override

            // ── Sexual orientation (short-form predicate, gender-invariant) ───────────
            ["SexualOrientation.Heterosexual"] = "гетеросексуальны",
            ["SexualOrientation.Homosexual"] = "гомосексуальны",
            ["SexualOrientation.Bisexual"] = "бисексуальны",
            ["SexualOrientation.Asexual"] = "асексуальны",

            // ── Gender ────────────────────────────────────────────────────────
            ["BiologicalGender.Male"] = "мужчина",
            ["BiologicalGender.Female"] = "женщина",

            // ── HairColor ─────────────────────────────────────────────────────
            ["HairColor.Black"] = "чёрный",
            ["HairColor.DarkBrown"] = "тёмно-каштановый",
            ["HairColor.Brown"] = "каштановый",
            ["HairColor.LightBrown"] = "светло-каштановый",
            ["HairColor.Blonde"] = "светлый",
            ["HairColor.Platinum"] = "платиновый",
            ["HairColor.Red"] = "рыжий",
            ["HairColor.Auburn"] = "медно-рыжий",
            ["HairColor.Grey"] = "серый",
            ["HairColor.White"] = "белый",
            ["HairColor.Dyed"] = "неестественный",

            // ── HairLength ────────────────────────────────────────────────────
            ["HairLength.Bald"] = "лысые",
            ["HairLength.CloseCropped"] = "очень короткие",
            ["HairLength.Short"] = "короткие",
            ["HairLength.EarLength"] = "до ушей",
            ["HairLength.Medium"] = "средней длины",
            ["HairLength.Long"] = "длинные",
            ["HairLength.WaistLength"] = "до талии",

            // ── HairShape ─────────────────────────────────────────────────────
            ["HairShape.Straight"] = "прямые",
            ["HairShape.Wavy"] = "волнистые",
            ["HairShape.Curly"] = "кудрявые",
            ["HairShape.Coily"] = "вьющиеся",

            // ── EyeColor ──────────────────────────────────────────────────────
            ["EyeColor.Brown"] = "карие",
            ["EyeColor.Blue"] = "голубые",
            ["EyeColor.Green"] = "зелёные",
            ["EyeColor.Grey"] = "серые",
            ["EyeColor.Hazel"] = "ореховые",
            ["EyeColor.Amber"] = "янтарные",

            // ── EyeShape ──────────────────────────────────────────────────────
            ["EyeShape.Almond"] = "миндалевидные",
            ["EyeShape.Round"] = "круглые",
            ["EyeShape.Hooded"] = "нависшие",
            ["EyeShape.Upturned"] = "приподнятые",
            ["EyeShape.Downturned"] = "опущенные",

            // ── FeatureShape ──────────────────────────────────────────────────
            ["FeatureShape.Small"] = "маленький",
            ["FeatureShape.Medium"] = "средний",
            ["FeatureShape.Large"] = "большой",
            ["FeatureShape.Narrow"] = "узкий",
            ["FeatureShape.Wide"] = "широкий",
            ["FeatureShape.Pointed"] = "заострённый",
            ["FeatureShape.Rounded"] = "округлый",

            // ── SkinColor ─────────────────────────────────────────────────────
            ["SkinColor.Pale"] = "бледная",
            ["SkinColor.Fair"] = "светлая",
            ["SkinColor.Light"] = "молочная",
            ["SkinColor.Olive"] = "оливковая",
            ["SkinColor.Beige"] = "бежевая",
            ["SkinColor.Tan"] = "загорелая",
            ["SkinColor.Brown"] = "коричневая",
            ["SkinColor.Dark"] = "тёмная",
            ["SkinColor.Ebony"] = "эбонитовая",

            // ── Month ─────────────────────────────────────────────────────────
            ["Month.January"] = "январь",
            ["Month.February"] = "февраль",
            ["Month.March"] = "март",
            ["Month.April"] = "апрель",
            ["Month.May"] = "май",
            ["Month.June"] = "июнь",
            ["Month.July"] = "июль",
            ["Month.August"] = "август",
            ["Month.September"] = "сентябрь",
            ["Month.October"] = "октябрь",
            ["Month.November"] = "ноябрь",
            ["Month.December"] = "декабрь",

            // ── ConnectionLabel ───────────────────────────────────────────────
            ["ConnectionLabel.Mother"] = "мать",
            ["ConnectionLabel.Father"] = "отец",
            ["ConnectionLabel.Son"] = "сын",
            ["ConnectionLabel.Daughter"] = "дочь",
            ["ConnectionLabel.Brother"] = "брат",
            ["ConnectionLabel.Sister"] = "сестра",
            ["ConnectionLabel.RomanticPartner"] = "любовник",
            ["ConnectionLabel.RomanticPartner.Female"] = "любовница",
            ["ConnectionLabel.PlatonicPartner"] = "близкий друг",
            ["ConnectionLabel.PlatonicPartner.Female"] = "близкая подруга",

            // ── ConnectionType ────────────────────────────────────────────────
            ["ConnectionType.CloseFamily"] = "близкородственная",
            ["ConnectionType.Family"] = "родственная",
            ["ConnectionType.Acquaintance"] = "знакомая",
            ["ConnectionType.Friend"] = "дружественная",
            ["ConnectionType.Colleague"] = "рабочая",
            ["ConnectionType.Romantic"] = "романтическая",
        };

    // ── ILocalizationProvider ─────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// When <paramref name="context"/> is a <see cref="LocalizationContext"/> with a
    /// non-empty <see cref="LocalizationContext.Context"/> string, the provider first
    /// tries the suffixed key <c>"{key}.{context}"</c> before falling back to
    /// <paramref name="key"/>. Null <see cref="LocalizationContext"/> are ignored and 
    /// the base key is used directly.
    /// </remarks>
    public string Get(string key, LocalizationContext? context = null)
    {
        if (context?.Context is { Length: > 0 } contextString)
        {
            var contextKey = $"{key}.{contextString}";
            if (_strings.TryGetValue(contextKey, out var contextVal))
                return contextVal;
        }

        return _strings.TryGetValue(key, out var val) ? val : key;
    }

    /// <inheritdoc/>
    public string Format(string key, LocalizationContext? context, params string[] args)
        => string.Format(Get(key, context), args);

    /// <inheritdoc/>
    public string Format(string key, params string[] args)
        => string.Format(Get(key), args);

    /// <inheritdoc/>
    /// <remarks>
    /// Lookup order when <paramref name="context"/> is a <see cref="LocalizationContext"/>:
    /// <list type="number">
    ///   <item><c>"{typeName}.{valueName}.{context.Context}"</c> — context-specific variant.</item>
    ///   <item><c>"{typeName}.{valueName}"</c> — base (gender-neutral or masculine default).</item>
    ///   <item><paramref name="value"/>.Name verbatim — ultimate fallback.</item>
    /// </list>
    /// Unknown <see cref="LocalizationContext"/> are ignored.
    /// </remarks>
    public string GetEnumValue<T>(T value, LocalizationContext? context = null) where T : SmartEnum<T>
    {
        if (context?.Context is { Length: > 0 } contextString)
        {
            var contextKey = $"{value.GetType().Name}.{value.Name}.{contextString}";
            if (_enumValues.TryGetValue(contextKey, out var contextVal))
                return contextVal;
        }

        return _enumValues.TryGetValue($"{value.GetType().Name}.{value.Name}", out var val) ? val : value.Name;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns a <see cref="LocalizationContext"/> wrapping the gender name so that
    /// Russian adjectives (e.g. age-category words) agree with the subject's gender
    /// or applies Russian number-agreement rules as follows:
    /// <list type="bullet">
    ///   <item>Units digit 1 (not teens) → <c>"Singular"</c> (год).</item>
    ///   <item>Units digit 2–4 (not teens) → <c>"Dual"</c> (года).</item>
    ///   <item>All other values → <c>null</c> (лет — uses the base template).</item>
    /// </list>
    /// </remarks>
    public LocalizationContext? Context(string contextType, params object[] contextItems)
    {
        if (contextType == "BiologicalGender") return new LocalizationContext((string)contextItems[0]);
        if (contextType == "Describer.Header.Age")
        {
            int age = (int)contextItems[0];
            int units = age % 10;
            int tens = age / 10 % 10;

            if (units == 1 && tens != 1) return new LocalizationContext("Singular");
            if (units is >= 2 and <= 4 && tens != 1) return new LocalizationContext("Dual");
        }
        return null;
    }
}