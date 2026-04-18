using Personalia.CharGen;
using Personalia.CharGen.Services;
using Personalia.Localization.Ru;

namespace Personalia;

internal class Program
{
    static void Main(string[] args)
    {
        CharGenExample charGen = new CharGenExample(2106170426);
        charGen.Run(loc: new RussianLocalizationProvider());
    }
}

