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
    /// Optional human-readable label that refines the relationship type
    /// beyond what <see cref="ConnectionType"/> captures.
    /// Examples: "mother", "father", "romantic partner", "platonic partner".
    /// </summary>
    public string? Label { get; set; }

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
/// Жизненные Связи — the directed multigraph of a character's relationships.
///
/// Represents: a directed multigraph where nodes are characters and edges are
/// typed, weighted connections. Multiple parallel edges between the same pair
/// of nodes are allowed (e.g. someone can be both a colleague AND a friend).
///
/// All filter methods (<see cref="From"/>, <see cref="To"/>, <see cref="OfType"/>,
/// <see cref="Between"/>) return a new <see cref="LifeConnections"/> so that
/// results can be further filtered or passed as a unit. Use the <see cref="All"/>
/// property when raw list access is required. Use <see cref="Union(LifeConnections,LifeConnections)"/>
/// to merge two graphs into a single deduplicated graph.
/// </summary>
public sealed class LifeConnections
{
    private readonly List<Connection> _connections = [];

    // ── Core ──────────────────────────────────────────────────────────────────

    /// <summary>All connections in this graph as an ordered read-only list.</summary>
    public IReadOnlyList<Connection> All => _connections;

    /// <summary>Number of connections currently in this graph.</summary>
    public int Count => _connections.Count;

    /// <summary>Add a new connection to the graph.</summary>
    public bool Add(Connection connection)
    {
        if (_connections.Any(c => c.Id == connection.Id))
            return false;
        _connections.Add(connection);
        return true;
    }

    /// <summary>Remove a connection by its ID. Returns <c>true</c> if found and removed.</summary>
    public bool Remove(Guid connectionId)
    {
        var target = _connections.FirstOrDefault(c => c.Id == connectionId);
        return target is not null && _connections.Remove(target);
    }

    // ── Composable filters — each returns a new LifeConnections ──────────────

    /// <summary>
    /// Returns a new graph containing only connections where the FROM node
    /// belongs to <paramref name="characterId"/>.
    /// </summary>
    public LifeConnections From(Guid characterId) =>
        Filter(c => c.FromCharacterNode.Character.Id == characterId);

    /// <summary>
    /// Returns a new graph containing only connections where the TO node
    /// belongs to <paramref name="characterId"/>.
    /// </summary>
    public LifeConnections To(Guid characterId) =>
        Filter(c => c.ToCharacterNode.Character.Id == characterId);

    /// <summary>
    /// Returns a new graph containing only connections of the given
    /// <paramref name="type"/>.
    /// </summary>
    public LifeConnections OfType(params ConnectionType[] types) =>
        Filter(c => types.Contains(c.Type));

    /// <summary>
    /// Returns a new graph containing all connections between
    /// <paramref name="characterA"/> and <paramref name="characterB"/>
    /// in either direction.
    /// </summary>
    public LifeConnections Between(Guid characterA, Guid characterB) =>
        Filter(c =>
            (c.FromCharacterNode.Character.Id == characterA &&
             c.ToCharacterNode.Character.Id == characterB) ||
            (c.FromCharacterNode.Character.Id == characterB &&
             c.ToCharacterNode.Character.Id == characterA));

    // ── Set operations ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="LifeConnections"/> containing every connection
    /// from <paramref name="a"/> and <paramref name="b"/>, deduplicated by
    /// connection <see cref="Connection.Id"/>.
    /// </summary>
    public static LifeConnections Union(LifeConnections a, LifeConnections b)
    {
        var result = new LifeConnections();
        var seen = new HashSet<Guid>();
        foreach (var conn in a._connections.Concat(b._connections))
            if (seen.Add(conn.Id))
                result.Add(conn);
        return result;
    }

    /// <summary>
    /// Returns a new <see cref="LifeConnections"/> containing every connection
    /// from this graph and <paramref name="other"/>, deduplicated by
    /// connection <see cref="Connection.Id"/>.
    /// </summary>
    public LifeConnections Union(LifeConnections other) => Union(this, other);

    // ── Private ───────────────────────────────────────────────────────────────

    private LifeConnections Filter(Func<Connection, bool> predicate)
    {
        var result = new LifeConnections();
        foreach (var c in _connections)
            if (predicate(c))
                result.Add(c);
        return result;
    }
}