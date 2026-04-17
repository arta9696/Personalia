using Personalia.CharGen;

namespace Personalia;

internal class Program
{
    static void Main(string[] args)
    {
        CharGenExample.Run(count: 50, outputFile: "characters.txt");
    }
}

// maybe store all connections in one unified graph of simulation instead of localized character connections?
// pull labels and text into separate file with ability to localize translation
// add and/or modify existing operations in LifeConnections to accomodate most of the uses where LifeConnections.All.Where(...) is used
// connect minimal character siblings to the same parents that generated character have
// write a graph to mermaid converter to visualise graph