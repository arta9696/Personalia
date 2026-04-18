using Personalia.CharGen.Services;
using Personalia.Localization;
using Personalia.Models;
using Personalia.Models.ConnectionSpace;

namespace Personalia.CharGen;

/// <summary>
/// Generates <paramref name="count"/> characters, prints each to the console,
/// and writes all of them to <paramref name="outputFile"/>.
///
/// A single <see cref="ConnectionGraph"/> is created here and shared between
/// <see cref="CharacterRandomizer"/> (which populates it) and
/// <see cref="CharacterDescriber"/> (which reads from it). This means the
/// describer always sees the fully up-to-date social graph for every character.
/// </summary>
public class CharGenExample
{
    private const string Separator = "====================";
    private const string OutputFile = "characters.txt";

    public CharGenExample()
    {
    }

    public CharGenExample(int seed)
    {
        Seed = seed;
    }

    public int? Seed { get; init; } = null;
    public ConnectionGraph Graph { get; } = new ConnectionGraph();
    public List<Character> Characters { get; } = [];



    /// <param name="count">Number of characters to generate (default: 50).</param>
    /// <param name="outputFile">Output file path (default: "characters.txt").</param>
    /// <param name="seed">Optional RNG seed for reproducible runs.</param>
    /// <param name="loc">
    /// Optional localisation provider. Defaults to English when <c>null</c>.
    /// Pass a <c>RussianLocalizationProvider</c> for Cyrillic output, or supply
    /// any custom <see cref="ILocalizationProvider"/> implementation.
    /// </param>
    public void Run(
        int count = 50,
        ILocalizationProvider? loc = null)
    {
        var generator = new CharacterRandomizer(Graph, Seed);
        var describer = new CharacterDescriber(loc);

        using var writer = new StreamWriter(
            OutputFile, append: false, encoding: System.Text.Encoding.UTF8);

        for (int i = 0; i < count; i++)
        {
            var character = generator.Generate();
            var description = describer.Describe(character, Graph);

            Console.WriteLine(Separator);
            Console.WriteLine(description);
            Console.WriteLine(Separator);

            writer.WriteLine(Separator);
            writer.WriteLine(description);
            writer.WriteLine(Separator);
        }

        Console.WriteLine($"Done. {count} characters written to '{OutputFile}'.");
    }
}