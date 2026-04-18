namespace Personalia.Localization;

/// <summary>
/// Lk — centralised catalogue of all localisation keys used across the system.
///
/// Grouped by domain concern so that additions and audits are localised to a
/// single file. All keys follow the convention <c>"Domain.Term"</c>.
/// </summary>
public static class Lk
{
    // ── Character describer ───────────────────────────────────────────────────

    public static class Describer
    {
        // {0}=firstName, {1}=lastName, {2}=age, {3}=birthdayMonth, {4}=birthdayDay
        public const string Header = "Describer.Header";
        public const string SectionAppearance = "Describer.SectionAppearance";
        // {0}=ageGroup, {1}=gender, {2}=orientation, {3}=height, {4}=build
        public const string AppearanceLine1 = "Describer.AppearanceLine1";
        // {0}=skin, {1}=eyeColor, {2}=eyeShape, {3}=hairLength, {4}=hairColor
        public const string AppearanceLine2 = "Describer.AppearanceLine2";
        // {0}=feature phrase
        public const string DistinctiveFeature = "Describer.DistinctiveFeature";
        // {0}=comma-and-joined feature phrases
        public const string DistinctiveFeatures = "Describer.DistinctiveFeatures";
        public const string SectionClothing = "Describer.SectionClothing";
        // {0}=legs, {1}=top, {2}=feet
        public const string ClothingBase = "Describer.ClothingBase";
        // {0}=accessory list
        public const string ClothingAccessories = "Describer.ClothingAccessories";
        public const string SectionStatus = "Describer.SectionStatus";
        public const string FamilyPrefix = "Describer.FamilyPrefix";
        public const string AcquaintancePrefix = "Describer.AcquaintancePrefix";
        public const string PartnerPrefix = "Describer.PartnerPrefix";
        public const string None = "Describer.None";
        // {0}=role, {1}=displayName, {2}=age, {3}=alive/deceased
        public const string FamilyEntry = "Describer.FamilyEntry";
        // {0}=displayName, {1}=age
        public const string AcquaintanceEntry = "Describer.AcquaintanceEntry";
        // {0}=displayName, {1}=label, {2}=age
        public const string PartnerEntry = "Describer.PartnerEntry";
        public const string Alive = "Describer.Alive";
        public const string Deceased = "Describer.Deceased";
        public const string Unemployed = "Describer.Unemployed";
        public const string Retired = "Describer.Retired";
        // {0}=workplace
        public const string Working = "Describer.Working";
    }

    // ── Height ────────────────────────────────────────────────────────────────

    public static class Height
    {
        public const string VeryShort = "Height.VeryShort";
        public const string Short = "Height.Short";
        public const string SlightlyShort = "Height.SlightlyShort";
        public const string Average = "Height.Average";
        public const string SlightlyTall = "Height.SlightlyTall";
        public const string Tall = "Height.Tall";
        public const string VeryTall = "Height.VeryTall";
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    public static class Build
    {
        public const string Skinny = "Build.Skinny";
        public const string Thin = "Build.Thin";
        public const string Plump = "Build.Plump";
        public const string Lean = "Build.Lean";
        public const string Stocky = "Build.Stocky";
        public const string Ripped = "Build.Ripped";
        public const string Muscular = "Build.Muscular";
        public const string Brawny = "Build.Brawny";
        public const string Average = "Build.Average";
    }
}