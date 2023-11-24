using GrimBuilder2.Core.Models;
using System.Numerics;
using System.Text.RegularExpressions;

namespace GrimBuilder2.Core.Services;

public class GdService(ArzParserService arz)
{
    DbrData NavigateSkillToLeafSkill(DbrData dbr, out bool petSkill, out bool buffSkill)
    {
        bool changed;
        petSkill = buffSkill = false;

        do
        {
            changed = false;
            if (dbr.TryGetValue("buffSkillName", out var buffSkillValue)
                && arz.GetDbrData(buffSkillValue.StringValueUnsafe) is { } buffSkillDbr)
            {
                dbr = buffSkillDbr;
                buffSkill = true;
                changed = true;
            }
            else if (dbr.TryGetValue("petSkillName", out var petSkillValue)
                && arz.GetDbrData(petSkillValue.StringValueUnsafe) is { } petSkillDbr)
            {
                dbr = petSkillDbr;
                petSkill = true;
                changed = true;
            }
        } while (changed);
        return dbr;
    }

    [Flags]
    enum GdSkillConnectorTypes { None, Forward = 1, Up = 2, Down = 4 }

    public async Task<IEnumerable<GdClass>> GetClassesAsync()
    {
        await arz.EnsureLoadedAsync();

        List<GdClass> result = [];
        var classFiles = arz.GetDbrData(new Regex(@"records/ui/skills/class\d+"));

        foreach (var master in classFiles.Where(f => Path.GetFileName(f.Path).Equals("classtable.dbr", StringComparison.OrdinalIgnoreCase)))
            if (arz.GetTag(master["skillTabTitle"].StringValues![0]) is { } name)
            {
                var uiSkills = master["tabSkillButtons"].StringValues!.Select(arz.GetDbrData).ToList();
                var rawSkills = uiSkills.Select(uiSkill => arz.GetDbrData(uiSkill!["skillName"].StringValueUnsafe)).ToList();

                GdClass @class = new(name,
                    arz.GetDbrData(master["skillPaneMasteryBitmap"].StringValueUnsafe)!["bitmapName"].StringValueUnsafe);

                var skillConnectors = new Dictionary<GdSkill, List<GdSkillConnectorTypes>>();
                @class.Skills.AddRange(rawSkills.Zip(uiSkills, (rawSkill, uiSkill) => (rawSkill, uiSkill))
                    .Select(w =>
                    {
                        var leafSkill = NavigateSkillToLeafSkill(w.rawSkill!, out var isPetSkill, out var isBuffSkill);
                        return (rawSkill: leafSkill, isPetSkill, isBuffSkill, uiSkill: w.uiSkill!);
                    })
                    .Select(w =>
                    {
                        var skill = new GdSkill
                        {
                            Name = arz.GetTag(w.rawSkill["skillDisplayName"].StringValueUnsafe)!,
                            Description = arz.GetTag(w.rawSkill["skillBaseDescription"].StringValueUnsafe)!,
                            Tier = w.rawSkill.TryGetValue("skillTier", out var skillTierValue) ? skillTierValue.IntegerValueUnsafe : 0,
                            MaximumLevel = w.rawSkill["skillMaxLevel"].IntegerValueUnsafe,
                            UltimateLevel = w.rawSkill.TryGetValue("skillUltimateLevel", out var ultimateLevel) ? ultimateLevel.IntegerValueUnsafe : null,

                            BitmapUpPath = w.rawSkill["skillUpBitmapName"].StringValueUnsafe,
                            BitmapDownPath = w.rawSkill["skillDownBitmapName"].StringValueUnsafe,

                            BitmapFrameUpPath = w.uiSkill.TryGetValue("bitmapNameUp", out var bitmapNameUp) ? bitmapNameUp.StringValueUnsafe : null,
                            BitmapFrameDownPath = w.uiSkill.TryGetValue("bitmapNameDown", out var bitmapNameDown) ? bitmapNameDown.StringValueUnsafe : null,
                            BitmapFrameInFocusPath = w.uiSkill.TryGetValue("bitmapNameInFocus", out var bitmapNameInFocus) ? bitmapNameInFocus.StringValueUnsafe : null,

                            X = w.uiSkill["bitmapPositionX"].IntegerValueUnsafe,
                            Y = w.uiSkill["bitmapPositionY"].IntegerValueUnsafe,
                            Circular = w.uiSkill["isCircular"].BooleanValueUnsafe,
                        };

                        if (w.rawSkill.TryGetValue("skillConnectionOn", out var skillConnectionValue) && skillConnectionValue.Type is DbrValueType.String && skillConnectionValue.StringValues!.Count > 0)
                            skillConnectors.Add(skill, skillConnectionValue.StringValues.Select(w => w switch
                            {
                                "ui/skills/skillallocation/skills_connectoroncenter.tex" => GdSkillConnectorTypes.Forward,
                                "ui/skills/skillallocation/skills_connectoronbranchboth.tex" => GdSkillConnectorTypes.Forward | GdSkillConnectorTypes.Up | GdSkillConnectorTypes.Down,
                                "ui/skills/skillallocation/skills_connectoronbranchdown.tex" => GdSkillConnectorTypes.Forward | GdSkillConnectorTypes.Down,
                                "ui/skills/skillallocation/skills_connectoronbranchup.tex" => GdSkillConnectorTypes.Forward | GdSkillConnectorTypes.Up,
                                "ui/skills/skillallocation/skills_connectorontransmuterdown.tex" => GdSkillConnectorTypes.Down,
                                "ui/skills/skillallocation/skills_connectorontransmuterup.tex" => GdSkillConnectorTypes.Up,
                                _ => throw new NotImplementedException()
                            }).ToList());

                        return skill;
                    }));

                // link skill dependencies
                foreach (var skill in @class.Skills)
                    if (skillConnectors.TryGetValue(skill, out var dependencies))
                    {
                        var currentSkill = skill;
                        var currentTier = currentSkill.Tier;

                        foreach (var dependency in dependencies)
                        {
                            GdSkill findNextSkill(GdSkill origin, int dy) =>
                                @class.Skills.Where(skill => skill != currentSkill && Math.Sign(skill.Y - currentSkill!.Y) == dy && skill.Tier == currentTier + 1)
                                    .Select(s => (skill: s, dist: (new Vector2(s.X, s.Y) - new Vector2(currentSkill!.X, currentSkill.Y)).LengthSquared()))
                                    .DefaultIfEmpty()
                                    .MinBy(s => s.dist)
                                    .skill;

                            if ((dependency & GdSkillConnectorTypes.Up) != 0)
                                findNextSkill(currentSkill, -1).Dependency = currentSkill;
                            if ((dependency & GdSkillConnectorTypes.Down) != 0)
                                findNextSkill(currentSkill, 1).Dependency = currentSkill;
                            if ((dependency & GdSkillConnectorTypes.Forward) != 0)
                            {
                                if (findNextSkill(currentSkill, 0) is { } nextSkill)
                                {
                                    nextSkill.Dependency = currentSkill;
                                    currentSkill = nextSkill;
                                }
                                ++currentTier;
                            }
                        }
                    }

                result.Add(@class);
            }

        return result;
    }

    public async Task<(IEnumerable<GdAffinity> affinities, IEnumerable<GdConstellation> constellations, IEnumerable<GdNebula> nebulas)> GetDevotionsAsync()
    {
        await arz.EnsureLoadedAsync();

        var masterDevotions = arz.GetDbrData("records/ui/skills/devotion/devotion_mastertable.dbr")!;
        var affinities = masterDevotions.Keys.Where(key => Regex.IsMatch(key, @"affinity\d+Rollover"))
            .Select(key => arz.GetDbrData(masterDevotions[key].StringValueUnsafe))
            .Select(dbr => new GdAffinity
            {
                Name = arz.GetTag(dbr!["Line1Tag"].StringValueUnsafe)!,
                Description = arz.GetTag(dbr["Line2Tag"].StringValueUnsafe)!,
            })
            .ToList();

        var constellations = masterDevotions.Keys.Where(key => Regex.IsMatch(key, @"devotionConstellation\d+"))
            .Select(key => arz.GetDbrData(masterDevotions[key].StringValueUnsafe))
            .Select(constellation =>
            {
                var backgroundDbr = constellation!.TryGetValue("constellationBackground", out var backgroundValue)
                    ? arz.GetDbrData(backgroundValue.StringValueUnsafe) : null;
                var gdConstellation = new GdConstellation
                {
                    Name = arz.GetTag(constellation["constellationDisplayTag"].StringValueUnsafe)!,
                    Description = arz.GetTag(constellation["constellationInfoTag"].StringValueUnsafe)!,
                    BitmapPath = backgroundDbr?["bitmapName"].StringValueUnsafe,
                    X = backgroundDbr?["bitmapPositionX"].IntegerValueUnsafe ?? 0,
                    Y = backgroundDbr?["bitmapPositionY"].IntegerValueUnsafe ?? 0,
                    Skills = constellation.Keys.Where(key => Regex.IsMatch(key, @"devotionButton\d+"))
                        .Select(key => arz.GetDbrData(constellation[key].StringValueUnsafe)!)
                        .Select(uiSkill => (uiSkill: uiSkill,
                            rawSkill: NavigateSkillToLeafSkill(arz.GetDbrData(uiSkill["skillName"].StringValueUnsafe)!, out _, out _)))
                        .Select(w => new GdSkill
                        {
                            Name = arz.GetTag(w.rawSkill["skillDisplayName"].StringValueUnsafe)!,
                            X = w.uiSkill["bitmapPositionX"].IntegerValueUnsafe,
                            Y = w.uiSkill["bitmapPositionY"].IntegerValueUnsafe,
                            BitmapFrameDownPath = w.uiSkill["bitmapNameDown"].StringValueUnsafe,
                            BitmapFrameUpPath = w.uiSkill["bitmapNameUp"].StringValueUnsafe,
                            BitmapFrameInFocusPath = w.uiSkill["bitmapNameInFocus"].StringValueUnsafe,
                        })
                        .ToArray(),
                    RequiredAffinities = constellation.Keys.Where(key => Regex.IsMatch(key, @"affinityRequiredName\d+"))
                        .Select(key => (
                            affinities.First(a => a.Name == constellation[key].StringValueUnsafe),
                            constellation[$"{key[..^5]}{key[^1]}"].IntegerValueUnsafe))
                        .ToArray(),
                    GrantedAffinities = constellation.Keys.Where(key => Regex.IsMatch(key, @"affinityGivenName\d+"))
                        .Select(key => (
                            affinities.First(a => a.Name == constellation[key].StringValueUnsafe),
                            constellation[$"{key[..^5]}{key[^1]}"].IntegerValueUnsafe))
                        .ToArray(),
                };
                return gdConstellation;
            })
            .ToList();

        var nebulas = masterDevotions["nebulaSections"].StringValues!.Select(arz.GetDbrData)
            .Select(nebula => new GdNebula
            {
                BitmapPath = nebula!["bitmapName"].StringValueUnsafe,
                X = nebula["bitmapPositionX"].IntegerValueUnsafe,
                Y = nebula["bitmapPositionY"].IntegerValueUnsafe
            })
            .ToList();

        return (affinities, constellations, nebulas);
    }
}
