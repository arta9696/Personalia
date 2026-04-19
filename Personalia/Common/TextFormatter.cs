using System.Text.RegularExpressions;

namespace Personalia.Common;

/// <summary>
/// TextFormatter — stateless text-formatting utilities shared across the Personalia system.
///
/// All methods are pure functions with no side-effects.
/// Centralises string manipulation that would otherwise be duplicated across
/// <c>CharacterDescriber</c> and similar services.
/// </summary>
public static partial class TextFormatter
{
    /// <summary>
    /// Joins <paramref name="items"/> with <c>", "</c> and replaces the final
    /// separator with <c>" and "</c> to form natural list prose.
    /// Returns the single element unchanged when the collection has exactly one item,
    /// and an empty string for an empty collection.
    /// </summary>
    /// <example><c>["red", "green", "blue"]</c> → <c>"red, green and blue"</c></example>
    public static string JoinWithAnd(IEnumerable<string> items)
    {
        var joined = string.Join(", ", items);
        int last = joined.LastIndexOf(", ", StringComparison.Ordinal);
        return last == -1 ? joined : joined.Remove(last, 2).Insert(last, " and ");
    }

    /// <summary>
    /// Escapes double-quote characters in <paramref name="text"/> using the
    /// Mermaid HTML entity <c>#quot;</c> so that quoted node and edge labels
    /// do not break Mermaid diagram syntax.
    /// </summary>
    public static string EscapeLabel(string text)
        => text.Replace("\"", "#quot;");

    /// <summary>
    /// TODO
    /// </summary>
    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
    public static partial Regex CamelCaseSplitter();
}