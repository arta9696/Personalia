namespace Personalia.CharGen.Data;

/// <summary>
/// Static clothing pools
/// </summary>
internal static class ClothingPool
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Legwear =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Male"] =
            [
                "jeans", "trousers", "sweatpants", "chinos", "shorts",
                "thermalwear", "cargo pants", "pleated pants", "sweat shorts",
                "joggers", "corduroy"
            ],
            ["Female"] =
            [
                "jeans", "leggings", "sweatpants", "chinos", "long skirt",
                "shorts", "capri pants", "culottes", "palazzo pants",
                "joggers", "jeggings", "bloomers"
            ]
        };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Topwear =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Male"] =
            [
                "t-shirt", "button-up shirt", "sweater", "hoodie", "jacket",
                "vest", "tank top", "blazer", "polo shirt", "cardigan",
                "bomber jacket", "leather jacket", "sports coat", "overcoat",
                "sweatshirt", "duster", "track jacket"
            ],
            ["Female"] =
            [
                "t-shirt", "button-up shirt", "blouse", "sweater", "hoodie",
                "jacket", "tank top", "cardigan", "bomber jacket", "denim jacket",
                "peacoat", "track jacket", "shrug dress", "blazer", "wrap top",
                "cape", "duster", "off-the-shoulder top", "crop top", "sweatshirt",
                "polo shirt", "sweater dress", "cropped sweater", "fitted turtleneck",
                "shell top"
            ]
        };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Footwear =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Male"] =
            [
                "sneakers", "boots", "sandals", "running shoes", "loafers",
                "oxfords", "espadrilles", "moccasins", "slides", "boat shoes",
                "flip-flops", "brogues", "clogs", "wellingtons", "slippers",
                "crocs", "combat boots", "ugg boots"
            ],
            ["Female"] =
            [
                "sneakers", "boots", "sandals", "loafers", "high heels",
                "flats", "running shoes", "mules", "clogs", "slippers",
                "espadrilles", "platforms", "stilettos", "brogues", "oxfords",
                "pumps", "over-the-knee boots", "flip-flops", "slides",
                "moccasins", "ugg boots", "booties"
            ]
        };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Accessories =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Male"] =
            [
                "wrist watch", "bracelet", "glasses", "tie", "cufflinks",
                "key chain", "lanyard", "bandana"
            ],
            ["Female"] =
            [
                "wrist watch", "bracelet", "glasses", "necklace", "earrings",
                "rings", "brooch", "choker", "hair clip", "purse",
                "key chain", "lanyard"
            ]
        };
}