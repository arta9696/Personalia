using Personalia.CharGen;

namespace Personalia;

internal class Program
{
    static void Main(string[] args)
    {
        CharGenExample.Run(count: 50, outputFile: "characters.txt");
    }
}