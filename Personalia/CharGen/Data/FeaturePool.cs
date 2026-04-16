namespace Personalia.CharGen.Data;

/// <summary>
/// Adjective pools for distinctive facial features —
/// mirrors DISTINCTIVE_FEATURES from the Python prototype.
/// </summary>
internal static class FeaturePool
{
    /// <summary>
    /// Keys are feature-type labels (e.g. "nose"); values are describing adjectives.
    /// A random adjective is combined with its key to form a phrase like "crooked nose".
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DistinctiveFeatures =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Nose"] = ["button", "delicate", "flat", "sharp", "wide", "crooked"],
            ["Eyes"] = ["narrow", "squinty", "hooded", "wide-set", "prominent", "bright"],
            ["eyebrows"] = ["thin", "thick", "bushy", "expressive", "arched"],
            ["Lips"] = ["thin", "curved", "wide"],
            ["smile"] = ["captivating", "joyful", "nervous", "unsettling"],
            ["chin"] = ["pointed", "square", "dimpled", "sharp"],
            ["face"] = ["heart-shaped", "round", "soft-featured", "freckled", "dimpled"]
        };
}