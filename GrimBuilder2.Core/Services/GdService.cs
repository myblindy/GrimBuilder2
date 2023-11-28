using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Models.SavedFile;
using System.Diagnostics;
using System.Globalization;
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

    public async Task<IList<GdClass>> GetClassesAsync()
    {
        await arz.EnsureLoadedAsync();

        List<GdClass> result = [];
        var classFiles = arz.GetDbrData(new Regex(@"records/ui/skills/class\d+"));

        foreach (var master in classFiles.Where(f => Path.GetFileName(f.Path).Equals("classtable.dbr", StringComparison.OrdinalIgnoreCase)))
            if (arz.GetTag(master["skillTabTitle"].StringValues![0]) is { } name)
            {
                var uiSkills = master["tabSkillButtons"].StringValues!.Select(arz.GetDbrData).ToList();
                var rawSkills = uiSkills.Select(uiSkill => arz.GetDbrData(uiSkill!["skillName"].StringValueUnsafe)).ToList();

                GdClass @class = new()
                {
                    Name = name,
                    Index = int.Parse(Regex.Match(rawSkills[0]!.Path, @"\d+").Value, CultureInfo.InvariantCulture),
                    BitmapPath = arz.GetDbrData(master["skillPaneMasteryBitmap"].StringValueUnsafe)!["bitmapName"].StringValueUnsafe.Replace("ui/", "/")
                };

                var skillConnectors = new Dictionary<GdSkill, List<GdSkillConnectorTypes>>();
                @class.Skills.AddRange(rawSkills.Zip(uiSkills, (rawSkill, uiSkill) => (rawSkill, uiSkill))
                    .Select(w =>
                    {
                        var leafSkill = NavigateSkillToLeafSkill(w.rawSkill!, out var isPetSkill, out var isBuffSkill);
                        return (originalRawSkill: w.rawSkill!, rawSkill: leafSkill, isPetSkill, isBuffSkill, uiSkill: w.uiSkill!);
                    })
                    .Select((w, index) =>
                    {
                        var skill = new GdSkill
                        {
                            Name = arz.GetTag(w.rawSkill["skillDisplayName"].StringValueUnsafe)!,
                            InternalName = w.originalRawSkill.Path,
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

    public async Task<(IList<GdAffinity> affinities, IList<GdConstellation> constellations, IList<GdNebula> nebulas)> GetDevotionsAsync()
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
                var skillDependencyIndex = new Dictionary<GdSkill, int>();
                var gdConstellation = new GdConstellation
                {
                    Name = arz.GetTag(constellation["constellationDisplayTag"].StringValueUnsafe)!,
                    Description = arz.GetTag(constellation["constellationInfoTag"].StringValueUnsafe)!,
                    BitmapPath = backgroundDbr?["bitmapName"].StringValueUnsafe,
                    X = backgroundDbr?["bitmapPositionX"].IntegerValueUnsafe ?? 0,
                    Y = backgroundDbr?["bitmapPositionY"].IntegerValueUnsafe ?? 0,
                    Skills = constellation.Keys.Where(key => Regex.IsMatch(key, @"devotionButton\d+"))
                        .Select(key => (uiSkill: arz.GetDbrData(constellation[key].StringValueUnsafe)!, index: int.Parse(Regex.Match(key, @"\d+$").ValueSpan, CultureInfo.InvariantCulture)))
                        .Select(w =>
                        {
                            var rawSkill = arz.GetDbrData(w.uiSkill["skillName"].StringValueUnsafe)!;
                            return (w.uiSkill, originalRawSkill: rawSkill,
                                linkIndex: constellation.TryGetValue($"devotionLinks{w.index}", out var linkIndex) ? linkIndex.IntegerValueUnsafe - 1 : -1,
                                rawSkill: NavigateSkillToLeafSkill(rawSkill, out _, out _));
                        })
                        .Select(w =>
                        {
                            var skill = new GdSkill
                            {
                                Name = arz.GetTag(w.rawSkill["skillDisplayName"].StringValueUnsafe)!,
                                InternalName = w.originalRawSkill.Path,
                                X = w.uiSkill["bitmapPositionX"].IntegerValueUnsafe,
                                Y = w.uiSkill["bitmapPositionY"].IntegerValueUnsafe,
                                BitmapFrameDownPath = w.uiSkill["bitmapNameDown"].StringValueUnsafe,
                                BitmapFrameUpPath = w.uiSkill["bitmapNameUp"].StringValueUnsafe,
                                BitmapFrameInFocusPath = w.uiSkill["bitmapNameInFocus"].StringValueUnsafe,
                            };
                            if (w.linkIndex >= 0)
                                skillDependencyIndex.Add(skill, w.linkIndex);
                            return skill;
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
                foreach (var (skill, linkIndex) in skillDependencyIndex)
                    skill.Dependency = gdConstellation.Skills[linkIndex];
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

    public async Task<IList<GdEquipSlot>> GetEquipSlotsAsync()
    {
        await arz.EnsureLoadedAsync();

        var master = arz.GetDbrData("records/ui/character/character_mastertable.dbr")!;
        var equipSlots = Enum.GetValues<EquipSlotType>().Select(type =>
        {
            var parser = arz.GetDbrData(master[$"equip{type}"].StringValueUnsafe)!;
            return new GdEquipSlot
            {
                Type = type,
                X = parser["itemX"].IntegerValueUnsafe,
                Y = parser["itemY"].IntegerValueUnsafe,
                Width = parser["itemXSize"].IntegerValueUnsafe,
                Height = parser["itemYSize"].IntegerValueUnsafe,
                SilhouetteBitmapPath = parser["silhouette"].StringValueUnsafe,
            };
        }).ToArray();

        return equipSlots;
    }

    public GdsCharacter ParseSaveFile(string baseFolderPath)
    {
        using var reader = new BinaryReader(File.OpenRead(Path.Combine(baseFolderPath, "player.gdc")));

        // encryption data
        GdEncryptionState encState = new();
        reader.ReadEncryptionKey(encState);

        // block helpers
        T readBlock<T>(int expectedId, Func<uint, T> action)
        {
            if (reader!.ReadEncUInt32(encState) != expectedId && expectedId != -1)
                throw new InvalidOperationException();
            var size = reader!.ReadEncUInt32(encState, false);
            var end = reader!.BaseStream.Position + size;

            var result = action(size);

            if (reader!.BaseStream.Position != end) throw new InvalidOperationException();
            if (reader!.ReadEncUInt32(encState, false) != 0) throw new InvalidOperationException();

            return result;
        }

        void skipNextBlock(int expectedId = -1) => readBlock<byte>(expectedId, size =>
        {
            Span<byte> buffer = stackalloc byte[(int)size];
            reader!.ReadEnc(encState, buffer);
            return 0;
        });

        // header
        var magic = reader.ReadEncInt32(encState);
        Debug.Assert(magic == 0x58434447);

        var version = reader.ReadEncInt32(encState);
        Debug.Assert(version is 1 or 2);

        var charName = reader.ReadEncWideString(encState);
        var sex = reader.ReadEncUInt8(encState);
        var classId = reader.ReadEncString(encState);
        var (classIndex1, classIndex2) = Regex.Match(classId, @"(\d\d)(\d\d)$") is { Success: true } m2
            ? (int.Parse(m2.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(m2.Groups[2].Value, CultureInfo.InvariantCulture))
            : (int.Parse(Regex.Match(classId, @"\d+$").Value, CultureInfo.InvariantCulture), -1);
        var level = reader.ReadEncInt32(encState);
        var hc = reader.ReadEncUInt8(encState);
        var expansion = version >= 2 ? reader.ReadEncUInt8(encState) : 0;

        // more magic stuff
        magic = reader.ReadEncInt32(encState, false);
        Debug.Assert(magic == 0);

        magic = reader.ReadEncInt32(encState);
        Debug.Assert(magic is 6 or 7 or 8);

        // uid
        Span<byte> uid = stackalloc byte[16];
        reader.ReadEnc(encState, uid);

        // info
        skipNextBlock(1);

        // bio
        skipNextBlock(2);

        // inventory
        var equippedItems = readBlock(3, _ =>
        {
            magic = reader.ReadEncInt32(encState);
            Debug.Assert(magic == 4);

            var flag = reader.ReadEncUInt8(encState);
            if (flag != 0)
            {
                var sackCount = reader.ReadEncInt32(encState);
                var focused = reader.ReadEncInt32(encState);
                var selected = reader.ReadEncInt32(encState);

                while (sackCount-- > 0)
                {
                    // sack data
                    skipNextBlock();
                }

                var useAlternate = reader.ReadEncUInt8(encState);

                // equipment
                var items = new GdsItem[16];
                for (int i = 0; i < 12; ++i)
                    items[i] = reader.ReadItem(true, encState);

                var alternate0 = reader.ReadEncUInt8(encState);
                items[12] = reader.ReadItem(true, encState);
                items[13] = reader.ReadItem(true, encState);

                var alternate1 = reader.ReadEncUInt8(encState);
                items[14] = reader.ReadItem(true, encState);
                items[15] = reader.ReadItem(true, encState);

                return items;
            }

            return default;
        });

        // stash
        _ = readBlock(4, _ =>
        {
            var version = reader.ReadEncInt32(encState);
            Debug.Assert(version is 5 or 6);

            var pageCount = version >= 6 ? reader.ReadEncInt32(encState) : 1;

            while (pageCount-- > 0)
                skipNextBlock();

            return 0;
        });

        // respawns
        skipNextBlock(5);

        // teleports
        skipNextBlock(6);

        // markers
        skipNextBlock(7);

        // shrines
        skipNextBlock(17);

        // skills
        var skills = readBlock(8, _ =>
        {
            version = reader.ReadEncInt32(encState);
            Debug.Assert(version == 5);

            var skills = reader.ReadEncArray(encState, () =>
            {
                var name = reader.ReadEncString(encState);
                var level = reader.ReadEncInt32(encState);
                var enabled = reader.ReadEncUInt8(encState);
                var devotionLevel = reader.ReadEncInt32(encState);
                var experience = reader.ReadEncInt32(encState);
                var active = reader.ReadEncUInt32(encState);
                reader.ReadEncUInt8(encState);
                reader.ReadEncUInt8(encState);
                var autoCastSkill = reader.ReadEncString(encState);
                var autoCastController = reader.ReadEncString(encState);

                return new GdsSkill(name, level, enabled != 0, devotionLevel);
            });
            var masteriesAllowed = reader.ReadEncInt32(encState);
            var skillReclamationPointsUsed = reader.ReadEncInt32(encState);
            var devotionReclamationPointsUsed = reader.ReadEncInt32(encState);
            var charItemSkills = reader.ReadEncArray(encState, () =>
            {
                var name = reader.ReadEncString(encState);
                var autoCastSkill = reader.ReadEncString(encState);
                var autoCastController = reader.ReadEncString(encState);
                var itemSlot = reader.ReadEncInt32(encState);
                var itemId = reader.ReadEncInt32(encState);

                return (name, autoCastSkill, autoCastController, itemSlot, itemId);
            });

            return skills;
        });

        // don't care about the rest

        // build result
        return new(charName, classIndex1, classIndex2, skills);
    }
}
