using Personalia.CharGen;

namespace Personalia;

internal class Program
{
    static void Main(string[] args)
    {
        CharGenExample.Run(count: 50, outputFile: "characters.txt");
    }
}

// for queued characters
//  generate orientation for queued character based on what love partners it had in life (opposite - getero, same - homo, both - bi, non - random weighthed to asexuial the more the age is)
//  if queued character generated as someone parent it must ensure that new children connections are eather not created (as they already have a kid they were generated from with his siblings) or created from already existing another love partner of theirs (implying half-brother and half-sisters)
// Refactor LifeConnection to return not IEnumerable<Connection> but LifeConnections (except in public IReadOnlyList<Connection> All => _connections;). Add union function that unites LifeConnections graphs into a new LifeConnections graph.
// Pull constants, probabilities and 'magic numbers/strings' up to constatnt vars in generator