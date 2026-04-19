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

// make localization context dependent on localization provider (in russian animals are masc/fem while in english they are neutral)
// deprecate GetEnumValue(string typeName, string valueName in favor of GetEnumValue<T>(T value
// Fill in all TODO summaries for doc generator