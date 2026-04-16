using Personalia.Models.AppearanceSpace.BodyParts;
using Personalia.Models.ClothingSpace.Components;

namespace Personalia.Models.ClothingSpace;

/// <summary>
/// ClothingItem — a single wearable piece.
///
/// Each item:
///   • Occupies one or more slots.
///   • Carries descriptive tags (e.g. "formal", "waterproof").
///   • Is composed of <see cref="IClothingComponent"/> characteristics.
///
/// If an item covers a parent slot, it implicitly covers all child slots
/// (e.g. a full-leg trouser covers Thigh → Shin → Foot).
/// </summary>
public sealed class ClothingItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    /// <summary>Body slots this item covers.</summary>
    public ISet<IBodyPart> OccupiedSlots { get; init; } = new HashSet<IBodyPart>();

    /// <summary>Теги — descriptive tags for filtering/querying.</summary>
    public IList<string> Tags { get; init; } = [];

    /// <summary>Компоненты-Характеристики — functional clothing properties.</summary>
    public IList<IClothingComponent> Components { get; init; } = [];

    // ── Convenience accessors ─────────────────────────────────────────────────

    public T? GetComponent<T>() where T : class, IClothingComponent
        => Components.OfType<T>().FirstOrDefault();

    public bool HasComponent<T>() where T : class, IClothingComponent
        => Components.OfType<T>().Any();

    /// <summary>
    /// Whether this item conceals its occupied slots from observers.
    /// </summary>
    public bool ConcealsSlots
        => GetComponent<SlotConcealment>()?.Conceals ?? false;
}

/// <summary>
/// Clothing — the outfit of a character.
/// Manages which items are currently worn and resolves slot coverage.
/// </summary>
public sealed class Clothing
{
    private readonly List<ClothingItem> _wornItems = [];

    public IReadOnlyList<ClothingItem> WornItems => _wornItems;

    /// <summary>Equip an item. Duplicate items are ignored.</summary>
    public void Wear(ClothingItem item)
    {
        if (!_wornItems.Contains(item))
            _wornItems.Add(item);
    }

    /// <summary>Remove an item.</summary>
    public void Remove(ClothingItem item) => _wornItems.Remove(item);

    /// <summary>
    /// Returns all slots currently concealed by worn clothing.
    /// </summary>
    public ISet<IBodyPart> GetConcealedSlots()
    {
        var concealed = new HashSet<IBodyPart>();
        foreach (var item in _wornItems.Where(i => i.ConcealsSlots))
            foreach (var slot in item.OccupiedSlots)
                concealed.Add(slot);
        return concealed;
    }

    /// <summary>Returns all items that cover a specific slot.</summary>
    public IEnumerable<ClothingItem> ItemsOnSlot(IBodyPart slot)
        => _wornItems.Where(i => i.OccupiedSlots.Contains(slot));
}