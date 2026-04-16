namespace Personalia.CharGen.Data;

/// <summary>
/// Workplace pools keyed by age-group string —
/// mirrors WORK_PLACES from the Python prototype.
/// </summary>
internal static class WorkPool
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByAgeGroup =
        new Dictionary<string, IReadOnlyList<string>>
        {
            // ── Teen (13–17) ──────────────────────────────────────────────────
            // Part-time, low-barrier roles accessible to teenagers.
            ["Teen"] =
            [
                "fast-food restaurant",
                "grocery store",
                "movie theater",
                "coffee shop",
                "retail store"
            ],

            // ── YoungAdult (18–25) ────────────────────────────────────────────
            // Entry-level careers, apprenticeships, and first professional steps.
            ["YoungAdult"] =
            [
                "coffee shop",
                "restaurant",
                "retail store",
                "warehouse",
                "call center",
                "grocery store",
                "sporting goods store",
                "pet store",
                "daycare center",
                "beauty salon",
                "print shop",
                "library",
                "furniture store",
                "campground",
                "technical support",
                "farm",
                "theater",
                "film studio",
                "automotive repair shop",
                "construction site",
                "lumberyard",
                "tech startup",
                "pottery studio",
                "university"
            ],

            // ── Adult (26–45) ─────────────────────────────────────────────────
            // Established mid-career professionals and skilled tradespeople.
            ["Adult"] =
            [
                "office",
                "hospital",
                "tech startup",
                "restaurant",
                "construction site",
                "automotive repair shop",
                "automotive dealership",
                "hotel",
                "library",
                "film studio",
                "farm",
                "school",
                "lumberyard",
                "pet store",
                "warehouse",
                "law firm",
                "advertising agency",
                "theater",
                "music school",
                "dance studio",
                "retail store",
                "art gallery",
                "beauty salon",
                "veterinary clinic",
                "textile factory",
                "tourism firm",
                "logistics company",
                "furniture store",
                "daycare center",
                "robotics lab",
                "insurance company",
                "postal service",
                "pottery studio",
                "small business",
                "call center",
                "social work agency",
                "government agency",
                "university",
                "technical support",
                "non-profit organization",
                "recycling plant",
                "observatory",
                "manufacturing plant",
                "pharmacy",
                "tattoo parlor",
                "trucking company",
                "fashion design studio",
                "real estate agency",
                "metal fabrication shop",
                "plumbing supply store"
            ],

            // ── MiddleAged (46–64) ────────────────────────────────────────────
            // Senior roles, specialised expertise, and management positions.
            ["MiddleAged"] =
            [
                "corporate office",
                "law firm",
                "school",
                "hospital",
                "library",
                "farm",
                "call center",
                "small business",
                "manufacturing plant",
                "government agency",
                "trucking company",
                "real estate agency",
                "university",
                "insurance company",
                "non-profit organization",
                "technical support",
                "freelance consulting",
                "art gallery",
                "property management",
                "social work agency",
                "architectural firm",
                "veterinary clinic",
                "tourism firm",
                "postal service",
                "interior design firm",
                "aerospace facility",
                "film production",
                "construction site",
                "pottery studio",
                "hotel",
                "theater",
                "pharmacy",
                "automotive dealership",
                "recycling plant",
                "metal fabrication shop",
                "fashion design studio",
                "tattoo parlor",
                "antique store",
                "furniture store",
                "logistics company",
                "garden center",
                "music school",
                "dance studio",
                "museum",
                "observatory"
            ],

            // ── Senior (65–100) ───────────────────────────────────────────────
            // Light roles, community involvement, and semi-retirement activities.
            ["Senior"] =
            [
                "volunteer center",
                "library",
                "community center",
                "consultant firm",
                "senior center",
                "assisted living facility",
                "adult daycare center",
                "retirement village"
            ],
        };
}