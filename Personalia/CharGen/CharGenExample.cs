using Personalia.CharGen.Services;

namespace Personalia.CharGen;

/// <summary>
/// Generates <paramref name="count"/> characters, prints each to the console,
/// and writes all of them to <paramref name="outputFile"/>.
/// </summary>
public static class CharGenExample
{
    private const string Separator = "====================";

    /// <param name="count">Number of characters to generate (default: 50).</param>
    /// <param name="outputFile">Output file path (default: "characters.txt").</param>
    /// <param name="seed">Optional RNG seed for reproducible runs.</param>
    public static void Run(
        int count = 50,
        string outputFile = "characters.txt",
        int? seed = null)
    {
        var generator = new CharacterRandomizer(seed);
        var describer = new CharacterDescriber();

        using var writer = new StreamWriter(outputFile, append: false,
                                            encoding: System.Text.Encoding.UTF8);

        for (int i = 0; i < count; i++)
        {
            var character = generator.Generate();
            var description = describer.Describe(character);

            Console.WriteLine(Separator);
            Console.WriteLine(description);
            Console.WriteLine(Separator);

            writer.WriteLine(Separator);
            writer.WriteLine(description);
            writer.WriteLine(Separator);
        }

        Console.WriteLine($"Done. {count} characters written to '{outputFile}'.");
    }
}