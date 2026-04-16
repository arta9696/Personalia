namespace Personalia.Models.AppearanceSpace.BodyParts;

/// <summary>
/// Часть тела — интерфейс для Телосложения.
/// Defines a contract for every body part that makes up a character's physique.
/// </summary>
public interface IBodyPart
{
    /// <summary>Human-readable display name (localisation-ready).</summary>
    string DisplayName { get; }

    /// <summary>Child parts nested under this one, if any.</summary>
    IReadOnlyList<IBodyPart> SubParts { get; }
}