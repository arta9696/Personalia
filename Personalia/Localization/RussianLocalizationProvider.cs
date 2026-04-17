namespace Personalia.Localization.Ru;

/// <summary>
/// RussianLocalizationProvider — Cyrillic locale for <c>CharacterDescriber</c> output.
///
/// All SmartEnum display names mirror the <c>DisplayName</c> values defined in the
/// domain-model enum classes, centralised here so the domain model remains independent
/// of the localisation layer.
/// </summary>
public sealed class RussianLocalizationProvider : ILocalizationProvider
{
    private static readonly IReadOnlyDictionary<string, string> _strings =
        new Dictionary<string, string>
        {
            // ── Describer ─────────────────────────────────────────────────────
            [Lk.Describer.Header]
                = "Ваше имя {0} {1}. Вам {2} лет. День рождения: {3} {4}.",
            [Lk.Describer.SectionAppearance] = "Внешность",
            [Lk.Describer.AppearanceLine1]
                = "Вы {0} {1}. Вы {2}. У вас {3} рост, а телосложение лучше всего можно описать как {4}. ",
            [Lk.Describer.AppearanceLine2]
                = "У вас {0} кожа, {1} {2} глаза, а ваши {3} волосы {4}.",
            [Lk.Describer.DistinctiveFeature] = "Ваша наиболее выделяющаяся черта — {0}.",
            [Lk.Describer.DistinctiveFeatures] = "Ваши наиболее выделяющиеся черты — {0}.",
            [Lk.Describer.SectionClothing] = "Одежда",
            [Lk.Describer.ClothingBase] = "Вы одеты в {0}, {1} и {2}.",
            [Lk.Describer.ClothingAccessories] = " Также у вас есть {0}.",
            [Lk.Describer.SectionStatus] = "Статус",
            [Lk.Describer.FamilyPrefix] = "Ваша семья: ",
            [Lk.Describer.AcquaintancePrefix] = "Ваши знакомые: ",
            [Lk.Describer.PartnerPrefix] = "Ваши партнёры: ",
            [Lk.Describer.None] = "Нет",
            [Lk.Describer.FamilyEntry] = "{0} по имени {1} (возраст {2}, {3})",
            [Lk.Describer.AcquaintanceEntry] = "{0} (возраст {1})",
            [Lk.Describer.PartnerEntry] = "{0} ({1}, возраст {2})",
            [Lk.Describer.Alive] = "живой",
            [Lk.Describer.Deceased] = "умерший",
            [Lk.Describer.Unemployed] = "В данный момент вы без работы.",
            [Lk.Describer.Retired] = "В данный момент вы на пенсии.",
            [Lk.Describer.Working] = "В данный момент вы работаете в {0}.",

            // ── Age groups ────────────────────────────────────────────────────
            [Lk.AgeGroup.Child] = "ребёнок",
            [Lk.AgeGroup.Teen] = "подросток",
            [Lk.AgeGroup.YoungAdult] = "молодой человек",
            [Lk.AgeGroup.Adult] = "взрослый",
            [Lk.AgeGroup.MiddleAged] = "среднего возраста",
            [Lk.AgeGroup.Senior] = "пожилой человек",

            // ── Sexual orientation ────────────────────────────────────────────
            [Lk.Orientation.Heterosexual] = "гетеросексуальный",
            [Lk.Orientation.Homosexual] = "гомосексуальный",
            [Lk.Orientation.Bisexual] = "бисексуальный",
            [Lk.Orientation.Asexual] = "асексуальный",
            [Lk.Orientation.Unknown] = "неизвестно",

            // ── Height ────────────────────────────────────────────────────────
            [Lk.Height.VeryShort] = "очень низкий",
            [Lk.Height.Short] = "низкий",
            [Lk.Height.SlightlyShort] = "чуть ниже среднего",
            [Lk.Height.Average] = "средний",
            [Lk.Height.SlightlyTall] = "чуть выше среднего",
            [Lk.Height.Tall] = "высокий",
            [Lk.Height.VeryTall] = "очень высокий",

            // ── Build ─────────────────────────────────────────────────────────
            [Lk.Build.Skinny] = "худощавый",
            [Lk.Build.Thin] = "стройный",
            [Lk.Build.Plump] = "пухлый",
            [Lk.Build.Lean] = "поджарый",
            [Lk.Build.Stocky] = "коренастый",
            [Lk.Build.Ripped] = "рельефный",
            [Lk.Build.Muscular] = "мускулистый",
            [Lk.Build.Brawny] = "мощный",
            [Lk.Build.Average] = "среднего телосложения",

            // ── Gender ────────────────────────────────────────────────────────
            [Lk.Gender.Male] = "мужчина",
            [Lk.Gender.Female] = "женщина",
        };

    private static readonly IReadOnlyDictionary<string, string> _enumValues =
        new Dictionary<string, string>
        {
            // ── HairColor ─────────────────────────────────────────────────────
            ["HairColor.Black"] = "чёрные",
            ["HairColor.DarkBrown"] = "тёмно-каштановые",
            ["HairColor.Brown"] = "каштановые",
            ["HairColor.LightBrown"] = "светло-каштановые",
            ["HairColor.Blonde"] = "светлые",
            ["HairColor.Platinum"] = "платиновые",
            ["HairColor.Red"] = "рыжие",
            ["HairColor.Auburn"] = "медно-рыжие",
            ["HairColor.Grey"] = "серые",
            ["HairColor.White"] = "белые",
            ["HairColor.Dyed"] = "крашеные",

            // ── HairLength ────────────────────────────────────────────────────
            ["HairLength.Bald"] = "лысые",
            ["HairLength.CloseCropped"] = "очень короткие",
            ["HairLength.Short"] = "короткие",
            ["HairLength.EarLength"] = "до ушей",
            ["HairLength.Medium"] = "средние",
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
            ["Month.March"] = "марть",
            ["Month.April"] = "апрель",
            ["Month.May"] = "май",
            ["Month.June"] = "июнь",
            ["Month.July"] = "июль",
            ["Month.August"] = "август",
            ["Month.September"] = "сентябрь",
            ["Month.October"] = "октябрь",
            ["Month.November"] = "ноябрь",
            ["Month.December"] = "декабрь",
        };

    // ── ILocalizationProvider ─────────────────────────────────────────────────

    public string Get(string key)
        => _strings.TryGetValue(key, out var val) ? val : key;

    public string Format(string key, params object?[] args)
        => string.Format(Get(key), args);

    public string GetEnumValue(string typeName, string valueName)
        => _enumValues.TryGetValue($"{typeName}.{valueName}", out var val) ? val : valueName;
}