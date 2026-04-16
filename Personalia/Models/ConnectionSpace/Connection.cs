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

    /// <summary>FromCharac — the origin character node ID.</summary>
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
/// Represents: a directed multigraph where nodes are characters
/// and edges are typed, weighted connections.
/// Multiple parallel edges between the same pair of nodes are allowed
/// (e.g. someone can be both a colleague AND a friend).
/// </summary>
public sealed class LifeConnections
{
    private readonly List<Connection> _connections = [];

    public IReadOnlyList<Connection> All => _connections;

    /// <summary>Add a new connection to the graph.</summary>
    public void Add(Connection connection) => _connections.Add(connection);

    /// <summary>Remove a connection by its ID.</summary>
    public bool Remove(Guid connectionId)
    {
        var target = _connections.FirstOrDefault(c => c.Id == connectionId);
        return target is not null && _connections.Remove(target);
    }

    /// <summary>All connections originating FROM a character.</summary>
    public IEnumerable<Connection> From(Guid characterId)
        => _connections.Where(c => c.FromCharacterNode.Character.Id == characterId);

    /// <summary>All connections pointing TO a character.</summary>
    public IEnumerable<Connection> To(Guid characterId)
        => _connections.Where(c => c.ToCharacterNode.Character.Id == characterId);

    /// <summary>All connections of a given type.</summary>
    public IEnumerable<Connection> OfType(ConnectionType type)
        => _connections.Where(c => c.Type == type);

    /// <summary>All connections between two specific characters (in either direction).</summary>
    public IEnumerable<Connection> Between(Guid characterA, Guid characterB)
        => _connections.Where(c =>
            (c.FromCharacterNode.Character.Id == characterA && c.ToCharacterNode.Character.Id == characterB) ||
            (c.FromCharacterNode.Character.Id == characterB && c.ToCharacterNode.Character.Id == characterA));
}