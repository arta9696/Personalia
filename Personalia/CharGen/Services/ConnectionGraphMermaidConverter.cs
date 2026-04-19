using Personalia.Common;
using Personalia.Localization;
using Personalia.Localization.En;
using Personalia.Models;
using Personalia.Models.ConnectionSpace;
using System.Text;

namespace Personalia.CharGen.Services;

/// <summary>
/// ConnectionGraphMermaidConverter — converts a <see cref="ConnectionGraph"/>
/// into a Mermaid <c>graph TD</c> definition for visual inspection of the
/// social network.
///
/// Node format:  <c>FirstName LastName Age</c>
/// Edge format:  directed arrow labelled with the localised display text for
///               <see cref="Connection.Label"/> when set, or the localised
///               <see cref="Models.Enums.ConnectionType"/> name as a fallback.
///               Both are resolved through <see cref="ILocalizationProvider"/>
///               so the diagram respects the active locale.
///
/// Label escaping is delegated to <see cref="TextFormatter.EscapeLabel"/>.
///
/// Usage:
/// <code>
///   var converter = new ConnectionGraphMermaidConverter();
///   string mermaid = converter.Convert(graph);
///   File.WriteAllText("graph.md", mermaid);
/// </code>
/// </summary>
public sealed class ConnectionGraphMermaidConverter
{
    private readonly ILocalizationProvider _loc;

    /// <param name="loc">
    /// Localisation provider (defaults to <see cref="EnglishLocalizationProvider"/>).
    /// </param>
    public ConnectionGraphMermaidConverter(ILocalizationProvider? loc = null)
    {
        _loc = loc ?? new EnglishLocalizationProvider();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts <paramref name="graph"/> into a Mermaid <c>graph TD</c> string.
    ///
    /// The output contains:
    /// <list type="number">
    ///   <item>One node declaration per unique character in the graph.</item>
    ///   <item>One directed edge per <see cref="Connection"/>, labelled with
    ///         the localised connection label or type name.</item>
    /// </list>
    /// </summary>
    /// <param name="graph">The shared connection graph to serialise.</param>
    /// <returns>A Mermaid <c>graph TD</c> block as a plain string.</returns>
    public string Convert(ConnectionGraph graph)
    {
        var characters = CollectUniqueCharacters(graph);
        var nodeIds = AssignNodeIds(characters);

        var sb = new StringBuilder();
        sb.AppendLine("graph TD");

        AppendNodeDeclarations(sb, characters, nodeIds);
        AppendEdges(sb, graph, nodeIds);

        return sb.ToString().TrimEnd();
    }

    // ── Node helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Collects every character that appears as either endpoint of any connection,
    /// deduplicated by <see cref="Character.Id"/>.
    /// </summary>
    private static List<Character> CollectUniqueCharacters(ConnectionGraph graph)
        => graph.All
            .SelectMany(c => new[] { c.FromCharacterNode.Character, c.ToCharacterNode.Character })
            .DistinctBy(c => c.Id)
            .ToList();

    /// <summary>
    /// Assigns a short, stable Mermaid node identifier to each character.
    /// IDs are sequential (<c>N0</c>, <c>N1</c>, …) to keep the diagram source readable.
    /// </summary>
    private static Dictionary<Guid, string> AssignNodeIds(List<Character> characters)
        => characters
            .Select((c, i) => (c.Id, NodeId: $"N{i}"))
            .ToDictionary(x => x.Id, x => x.NodeId);

    /// <summary>
    /// Writes one Mermaid node declaration per character using a quoted label
    /// so that spaces and special characters in names are handled correctly.
    /// </summary>
    private static void AppendNodeDeclarations(
        StringBuilder sb,
        List<Character> characters,
        Dictionary<Guid, string> nodeIds)
    {
        foreach (var character in characters)
        {
            string nodeId = nodeIds[character.Id];
            string label = TextFormatter.EscapeLabel(NodeLabel(character));
            sb.AppendLine($"    {nodeId}[\"{label}\"]");
        }
    }

    /// <summary>
    /// Writes one Mermaid directed edge per connection.
    /// The edge label is resolved through <see cref="ILocalizationProvider"/>:
    /// <see cref="Connection.Label"/> when available, or the
    /// <see cref="Models.Enums.ConnectionType"/> name otherwise.
    /// </summary>
    private void AppendEdges(
        StringBuilder sb,
        ConnectionGraph graph,
        Dictionary<Guid, string> nodeIds)
    {
        foreach (var conn in graph.All)
        {
            string from = nodeIds[conn.FromCharacterNode.Character.Id];
            string to = nodeIds[conn.ToCharacterNode.Character.Id];
            string label = TextFormatter.EscapeLabel(LabelOrType(conn));
            sb.AppendLine($"    {from} -->|\"{label}\"| {to}");
        }
    }

    // ── Formatting helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Builds the human-readable node label: <c>FirstName LastName Age</c>.
    /// </summary>
    private static string NodeLabel(Character character)
    {
        var app = character.Appearance;
        return $"{app.FirstName.Value} {app.LastName.Value} {app.Age.Value}";
    }

    /// <summary>
    /// Returns the localised display string for a connection's role.
    /// Uses the typed <see cref="Models.Enums.ConnectionLabel"/> when set;
    /// falls back to the localised <see cref="Models.Enums.ConnectionType"/> name.
    /// </summary>
    private string LabelOrType(Connection conn)
        => conn.Label is not null
            ? _loc.GetEnumValue(conn.Label)
            : _loc.GetEnumValue(conn.Type);
}