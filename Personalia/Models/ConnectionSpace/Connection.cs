using Personalia.Models.Enums;

namespace Personalia.Models.ConnectionSpace;

/// <summary>
/// Connection — a directed edge in the life-connections graph.
///
/// Models an asymmetric relationship: FromChar -> ToChar, with its own
/// type, strength, and mutual knowledge levels.
/// </summary>
public sealed class Connection
{
    public Guid Id { get; } = Guid.NewGuid();

    // ── Endpoints ─────────────────────────────────────────────────────────────

    /// <summary>FromChar — the origin character node ID.</summary>
    public required ConnectionNode FromCharacterNode { get; init; }

    /// <summary>ToChar — the destination character node ID.</summary>
    public required ConnectionNode ToCharacterNode { get; init; }

    // ── Relationship ──────────────────────────────────────────────────────────

    /// <summary>Type — the broad category of this relationship.</summary>
    public ConnectionType Type { get; set; } = ConnectionType.Acquaintance;

    /// <summary>
    /// Optional typed label that refines the relationship role beyond what
    /// <see cref="ConnectionType"/> captures. Display text is resolved through
    /// <see cref="Localization.ILocalizationProvider"/> at the
    /// presentation layer; the label itself carries no locale-specific strings.
    /// Examples: <see cref="ConnectionLabel.Mother"/>,
    ///           <see cref="ConnectionLabel.RomanticPartner"/>.
    /// </summary>
    public ConnectionLabel? Label { get; set; }

    /// <summary>
    /// Strength of the connection (0.0 = non-existent, 1.0 = strongest).
    /// </summary>
    public float Strength { get; set; } = 0.5f;

    // ── Knowledge ─────────────────────────────────────────────────────────────

    /// <summary>
    /// How aware the FROM character is of this connection.
    /// (0.0 = unaware, 1.0 = fully aware)
    /// </summary>
    public float FromKnowledge { get; set; } = 1.0f;

    /// <summary>
    /// How aware the TO character is of this connection.
    /// (0.0 = unaware, 1.0 = fully aware)
    /// </summary>
    public float ToKnowledge { get; set; } = 1.0f;
}

/// <summary>
/// ConnectionNode — a node in the connections graph, wrapping a character reference.
/// </summary>
public sealed class ConnectionNode
{
    public required Character Character { get; init; }
}

/// <summary>
/// ConnectionGraph — the global directed multigraph of all character relationships.
///
/// Replaces the per-character <c>LifeConnections</c> with a single shared graph so
/// that the connection set is the single source of truth and cross-character queries
/// require no cross-referencing of local lists.
///
/// All filter methods return a new <see cref="ConnectionGraph"/> projected view so
/// results can be further narrowed or composed. The source graph is never mutated
/// by filtering.
/// </summary>
public sealed class ConnectionGraph
{
    public Guid Id { get; } = Guid.NewGuid();
    private readonly List<Connection> _connections = [];

    // ── Core ──────────────────────────────────────────────────────────────────

    /// <summary>All connections in the graph as an ordered read-only list.</summary>
    public IReadOnlyList<Connection> All => _connections;

    /// <summary>Total number of connections in the graph.</summary>
    public int Count => _connections.Count;

    /// <summary>Adds a connection. Returns <c>false</c> (no-op) if the ID already exists.</summary>
    public bool Add(Connection connection)
    {
        if (_connections.Any(c => c.Id == connection.Id)) return false;
        _connections.Add(connection);
        return true;
    }

    /// <summary>Removes a connection by ID. Returns <c>true</c> if found and removed.</summary>
    public bool Remove(Guid connectionId)
    {
        var target = _connections.FirstOrDefault(c => c.Id == connectionId);
        return target is not null && _connections.Remove(target);
    }

    // ── General filters ───────────────────────────────────────────────────────

    /// <summary>All connections originating from <paramref name="characterId"/>.</summary>
    public ConnectionGraph From(Guid characterId)
        => Filter(c => c.FromCharacterNode.Character.Id == characterId);

    /// <summary>All connections pointing to <paramref name="characterId"/>.</summary>
    public ConnectionGraph To(Guid characterId)
        => Filter(c => c.ToCharacterNode.Character.Id == characterId);

    /// <summary>All connections whose type matches any element of <paramref name="types"/>.</summary>
    public ConnectionGraph OfType(params ConnectionType[] types)
        => Filter(c => types.Contains(c.Type));

    /// <summary>
    /// All connections between <paramref name="a"/> and <paramref name="b"/>
    /// in either direction.
    /// </summary>
    public ConnectionGraph Between(Guid a, Guid b)
        => Filter(c =>
            (c.FromCharacterNode.Character.Id == a && c.ToCharacterNode.Character.Id == b) ||
            (c.FromCharacterNode.Character.Id == b && c.ToCharacterNode.Character.Id == a));

    // ── Semantic filters ──────────────────────────────────────────────────────

    /// <summary>
    /// All family connections
    /// (<see cref="ConnectionType.CloseFamily"/> or <see cref="ConnectionType.Family"/>).
    /// </summary>
    public ConnectionGraph Family()
        => Filter(c => c.Type.IsFamily);

    /// <summary>All non-family connections.</summary>
    public ConnectionGraph NonFamily()
        => Filter(c => !c.Type.IsFamily);

    /// <summary>
    /// All partner connections — those whose <see cref="Connection.Label"/> has
    /// <see cref="ConnectionLabel.IsPartner"/> set to <c>true</c>
    /// (e.g. <see cref="ConnectionLabel.RomanticPartner"/>,
    ///       <see cref="ConnectionLabel.PlatonicPartner"/>).
    /// </summary>
    public ConnectionGraph Partners()
        => Filter(c => c.Label?.IsPartner == true);

    /// <summary>
    /// <see cref="ConnectionType.CloseFamily"/> connections whose label has
    /// <see cref="ConnectionLabel.IsChild"/> set to <c>true</c>
    /// (<see cref="ConnectionLabel.Son"/> or <see cref="ConnectionLabel.Daughter"/>).
    /// Combine with <c>From(parentId)</c> to find a character's children.
    /// </summary>
    public ConnectionGraph Children()
        => Filter(c => c.Type == ConnectionType.CloseFamily && c.Label?.IsChild == true);

    /// <summary>
    /// <see cref="ConnectionType.CloseFamily"/> connections whose label has
    /// <see cref="ConnectionLabel.IsParent"/> set to <c>true</c>
    /// (<see cref="ConnectionLabel.Mother"/> or <see cref="ConnectionLabel.Father"/>),
    /// optionally excluding a specific destination.
    /// Combine with <c>From(childId)</c> to find a character's parents.
    /// </summary>
    /// <param name="excludeCharacterId">
    /// When provided, connections pointing to this character are omitted —
    /// useful for finding co-parents while excluding the querying character itself.
    /// </param>
    public ConnectionGraph Parents(Guid? excludeCharacterId = null)
        => Filter(c =>
            c.Type == ConnectionType.CloseFamily &&
            c.Label?.IsParent == true &&
            (excludeCharacterId is null || c.ToCharacterNode.Character.Id != excludeCharacterId));

    /// <summary>All connections whose destination character is currently alive.</summary>
    public ConnectionGraph Alive()
        => Filter(c => c.ToCharacterNode.Character.IsAlive);

    /// <summary>
    /// All connections whose <see cref="Connection.Label"/> is not null.
    /// </summary>
    public ConnectionGraph WithLabel()
        => Filter(c => c.Label is not null);

    /// <summary>
    /// All connections whose <see cref="Connection.Label"/> exactly matches any of
    /// the supplied <paramref name="labels"/> (by SmartEnum value equality).
    /// </summary>
    public ConnectionGraph WithLabel(params ConnectionLabel[] labels)
        => Filter(c => c.Label is not null && labels.Contains(c.Label));

    // ── Set operations ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new graph containing every connection from <paramref name="a"/>
    /// and <paramref name="b"/>, deduplicated by connection ID.
    /// </summary>
    public static ConnectionGraph Union(ConnectionGraph a, ConnectionGraph b)
    {
        var result = new ConnectionGraph();
        var seen = new HashSet<Guid>();
        foreach (var conn in a._connections.Concat(b._connections))
            if (seen.Add(conn.Id))
                result.Add(conn);
        return result;
    }

    /// <inheritdoc cref="Union(ConnectionGraph,ConnectionGraph)"/>
    public ConnectionGraph Union(ConnectionGraph other) => Union(this, other);

    // ── Private ───────────────────────────────────────────────────────────────

    private ConnectionGraph Filter(Func<Connection, bool> predicate)
    {
        var result = new ConnectionGraph();
        foreach (var c in _connections.Where(predicate))
            result.Add(c);
        return result;
    }
}