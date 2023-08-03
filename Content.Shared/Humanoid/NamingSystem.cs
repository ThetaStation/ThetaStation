using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Dataset;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;

namespace Content.Shared.Humanoid
{
    /// <summary>
    /// Figure out how to name a humanoid with these extensions.
    /// </summary>
    public sealed class NamingSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        public string GetName(string species, Gender? gender = null)
        {
            // if they have an old species or whatever just fall back to human I guess?
            // Some downstream is probably gonna have this eventually but then they can deal with fallbacks.
            if (!_prototypeManager.TryIndex(species, out SpeciesPrototype? speciesProto))
            {
                speciesProto = _prototypeManager.Index<SpeciesPrototype>("Human");
                Log.Warning($"Unable to find species {species} for name, falling back to Human");
            }

            switch (speciesProto.Naming)
            {
                case SpeciesNaming.TheFirstofLast:
                    return Loc.GetString("namepreset-thefirstoflast",
                        ("first", GetFirstName(speciesProto, gender)), ("last", GetLastName(speciesProto, gender))); // Corvax-LastnameGender
                case SpeciesNaming.FirstDashFirst:
                    return Loc.GetString("namepreset-firstdashfirst",
                        ("first1", GetFirstName(speciesProto, gender)), ("first2", GetFirstName(speciesProto, gender)));
                case SpeciesNaming.FirstLast:
                default:
                    return Loc.GetString("namepreset-firstlast",
                        ("first", GetFirstName(speciesProto, gender)), ("last", GetLastName(speciesProto, gender))); // Corvax-LastnameGender
            }
        }

        public string GetFirstName(SpeciesPrototype speciesProto, Gender? gender = null)
        {
            var localePreffix = _cfg.GetCVar(CCVars.CultureLocale) == "en-US" ? "_en" : "_ru";
            switch (gender)
            {
                case Gender.Male:
                    return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.MaleFirstNames + localePreffix).Values);
                case Gender.Female:
                    return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.FemaleFirstNames + localePreffix).Values);
                default:
                    if (_random.Prob(0.5f))
                        return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.MaleFirstNames + localePreffix).Values);
                    else
                        return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.FemaleFirstNames + localePreffix).Values);
            }
        }

        // Corvax-LastnameGender-Start: Added custom gender split logic
        public string GetLastName(SpeciesPrototype speciesProto, Gender? gender = null)
        {
            var localePreffix = _cfg.GetCVar(CCVars.CultureLocale) == "en-US" ? "_en" : "_ru";
            switch (gender)
            {
                case Gender.Male:
                    return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.MaleLastNames + localePreffix).Values);
                case Gender.Female:
                    return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.FemaleLastNames + localePreffix).Values);
                default:
                    if (_random.Prob(0.5f))
                        return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.MaleLastNames + localePreffix).Values);
                    else
                        return _random.Pick(_prototypeManager.Index<DatasetPrototype>(speciesProto.FemaleLastNames + localePreffix).Values);
            }
        }
        // Corvax-LastnameGender-End
    }
}
