using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Models.SavedFile;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace GrimBuilder2.Services;

public partial class GdService(ArzParserService arz)
{
    DbrData NavigateSkillToLeafSkill(DbrData dbr, out bool petSkill, out bool buffSkill)
    {
        bool changed;
        petSkill = buffSkill = false;

        do
        {
            changed = false;
            if (dbr.TryGetStringValue("buffSkillName", out var buffSkillValue)
                && arz.GetDbrData(buffSkillValue) is { } buffSkillDbr)
            {
                dbr = buffSkillDbr;
                buffSkill = true;
                changed = true;
            }
            else if (dbr.TryGetStringValue("petSkillName", out var petSkillValue)
                && arz.GetDbrData(petSkillValue) is { } petSkillDbr)
            {
                dbr = petSkillDbr;
                petSkill = true;
                changed = true;
            }
        } while (changed);
        return dbr;
    }

    void ReadStats(GdStats stats, DbrData dbr)
    {
        stats.Health = dbr.GetFloatValueOrDefault("characterLife");
        stats.HealthModifier = dbr.GetFloatValueOrDefault("characterLifeModifier");
    }

    [Flags]
    enum GdSkillConnectorTypes { None, Forward = 1, Up = 2, Down = 4 }

    public async Task<(IList<GdClass> Classes, IDictionary<(GdClass? Class1, GdClass? Class2), string?> ClassCombinations)> GetClassesAsync()
    {
        await arz.EnsureLoadedAsync();

        List<GdClass> result = [];
        var classFiles = arz.GetDbrData(new Regex(@"records/ui/skills/class\d+"));

        foreach (var master in classFiles.Where(f => Path.GetFileName(f.Path).Equals("classtable.dbr", StringComparison.OrdinalIgnoreCase)))
            if (arz.GetTag(master.GetStringValue("skillTabTitle")) is { } name)
            {
                var uiSkills = master.GetStringValues("tabSkillButtons").Select(arz.GetDbrData).ToList();
                var rawSkills = uiSkills.Select(uiSkill => arz.GetDbrData(uiSkill!.GetStringValue("skillName"))).ToList();

                GdClass @class = new()
                {
                    Name = name,
                    Index = int.Parse(Regex.Match(rawSkills[0]!.Path, @"\d+").Value, CultureInfo.InvariantCulture),
                    BitmapPath = arz.GetDbrData(master.GetStringValue("skillPaneMasteryBitmap"))!
                        .GetStringValue("bitmapName")
                        .Replace("ui/", "/")
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
                            Name = arz.GetTag(w.rawSkill.GetStringValue("skillDisplayName"))!,
                            InternalName = w.originalRawSkill.Path,
                            Description = arz.GetTag(w.rawSkill.GetStringValue("skillBaseDescription"))!,
                            Tier = w.rawSkill.TryGetIntegerValue("skillTier", out var skillTierValue) ? skillTierValue : 0,
                            MaximumLevel = w.rawSkill.GetIntegerValue("skillMaxLevel"),
                            UltimateLevel = w.rawSkill.TryGetIntegerValue("skillUltimateLevel", out var ultimateLevel) ? ultimateLevel : null,

                            BitmapUpPath = w.rawSkill.GetStringValue("skillUpBitmapName"),
                            BitmapDownPath = w.rawSkill.GetStringValue("skillDownBitmapName"),

                            BitmapFrameUpPath = w.uiSkill.TryGetStringValue("bitmapNameUp", out var bitmapNameUp) ? bitmapNameUp : null,
                            BitmapFrameDownPath = w.uiSkill.TryGetStringValue("bitmapNameDown", out var bitmapNameDown) ? bitmapNameDown : null,
                            BitmapFrameInFocusPath = w.uiSkill.TryGetStringValue("bitmapNameInFocus", out var bitmapNameInFocus) ? bitmapNameInFocus : null,

                            X = w.uiSkill.GetIntegerValue("bitmapPositionX"),
                            Y = w.uiSkill.GetIntegerValue("bitmapPositionY"),
                            Circular = w.uiSkill.GetBooleanValue("isCircular"),
                        };

                        if (w.rawSkill.TryGetStringValues("skillConnectionOn", out var skillConnectionValues) && skillConnectionValues.Length > 0)
                            skillConnectors.Add(skill, skillConnectionValues.Select(w => w switch
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

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return (result,
            (from c1 in result.Append(null)
             from c2 in result.Append(null)
             select (key: (c1, c2), value:
                c1 == c2 ? c1?.Name
                : c1 is null ? c2.Name : c2 is null ? c1.Name
                : arz.GetTag($"tagSkillClassName{Math.Min(c1.Index, c2.Index):00}{Math.Max(c1.Index, c2.Index):00}")))
            .ToDictionary(w => w.key, w => w.value));
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }

    public async Task<(IList<GdAffinity> affinities, IList<GdConstellation> constellations, IList<GdNebula> nebulas)> GetDevotionsAsync()
    {
        await arz.EnsureLoadedAsync();

        var masterDevotions = arz.GetDbrData("records/ui/skills/devotion/devotion_mastertable.dbr")!;
        var affinities = masterDevotions.Keys.Where(key => Regex.IsMatch(key, @"affinity\d+Rollover"))
            .Select(key => arz.GetDbrData(masterDevotions.GetStringValue(key)))
            .Select(dbr => new GdAffinity
            {
                Name = arz.GetTag(dbr!.GetStringValue("Line1Tag"))!,
                Description = arz.GetTag(dbr.GetStringValue("Line2Tag"))!,
            })
            .ToList();

        var constellations = masterDevotions.Keys.Where(key => Regex.IsMatch(key, @"devotionConstellation\d+"))
            .Select(key => arz.GetDbrData(masterDevotions.GetStringValue(key)))
            .Select(constellation =>
            {
                var backgroundDbr = constellation!.TryGetStringValue("constellationBackground", out var backgroundValue)
                    ? arz.GetDbrData(backgroundValue) : null;
                var skillDependencyIndex = new Dictionary<GdSkill, int>();
                var gdConstellation = new GdConstellation
                {
                    Name = arz.GetTag(constellation.GetStringValue("constellationDisplayTag"))!,
                    Description = arz.GetTag(constellation.GetStringValue("constellationInfoTag"))!,
                    BitmapPath = backgroundDbr?.GetStringValue("bitmapName"),
                    X = backgroundDbr?.GetIntegerValue("bitmapPositionX") ?? 0,
                    Y = backgroundDbr?.GetIntegerValue("bitmapPositionY") ?? 0,
                    Skills = constellation.Keys.Where(key => Regex.IsMatch(key, @"devotionButton\d+"))
                        .Select(key => (uiSkill: arz.GetDbrData(constellation.GetStringValue(key))!, index: int.Parse(Regex.Match(key, @"\d+$").ValueSpan, CultureInfo.InvariantCulture)))
                        .Select(w =>
                        {
                            var rawSkill = arz.GetDbrData(w.uiSkill.GetStringValue("skillName"))!;
                            return (w.uiSkill, originalRawSkill: rawSkill,
                                linkIndex: constellation.TryGetIntegerValue($"devotionLinks{w.index}", out var linkIndex) ? linkIndex - 1 : -1,
                                rawSkill: NavigateSkillToLeafSkill(rawSkill, out _, out _));
                        })
                        .Select(w =>
                        {
                            var skill = new GdSkill
                            {
                                Name = arz.GetTag(w.rawSkill.GetStringValue("skillDisplayName"))!,
                                InternalName = w.originalRawSkill.Path,
                                X = w.uiSkill.GetIntegerValue("bitmapPositionX"),
                                Y = w.uiSkill.GetIntegerValue("bitmapPositionY"),
                                BitmapFrameDownPath = w.uiSkill.GetStringValue("bitmapNameDown"),
                                BitmapFrameUpPath = w.uiSkill.GetStringValue("bitmapNameUp"),
                                BitmapFrameInFocusPath = w.uiSkill.GetStringValue("bitmapNameInFocus"),
                            };
                            if (w.linkIndex >= 0)
                                skillDependencyIndex.Add(skill, w.linkIndex);
                            return skill;
                        })
                        .ToArray(),
                    RequiredAffinities = constellation.Keys.Where(key => Regex.IsMatch(key, @"affinityRequiredName\d+"))
                        .Select(key => (
                            affinities.First(a => a.Name == constellation.GetStringValue(key)),
                            constellation.GetIntegerValue($"{key[..^5]}{key[^1]}")))
                        .ToArray(),
                    GrantedAffinities = constellation.Keys.Where(key => Regex.IsMatch(key, @"affinityGivenName\d+"))
                        .Select(key => (
                            affinities.First(a => a.Name == constellation.GetStringValue(key)),
                            constellation.GetIntegerValue($"{key[..^5]}{key[^1]}")))
                        .ToArray(),
                };
                foreach (var (skill, linkIndex) in skillDependencyIndex)
                    skill.Dependency = gdConstellation.Skills[linkIndex];
                return gdConstellation;
            })
            .ToList();

        var nebulas = masterDevotions.GetStringValues("nebulaSections").Select(arz.GetDbrData)
            .Select(nebula => new GdNebula
            {
                BitmapPath = nebula!.GetStringValue("bitmapName"),
                X = nebula.GetIntegerValue("bitmapPositionX"),
                Y = nebula.GetIntegerValue("bitmapPositionY")
            })
            .ToList();

        return (affinities, constellations, nebulas);
    }

    static readonly Dictionary<string, GdItemType> itemTypeMapping = new()
    {
        ["ItemArtifact"] = GdItemType.Relic,
        ["ArmorProtective_Feet"] = GdItemType.Feet,
        ["ArmorProtective_Hands"] = GdItemType.Hands,
        ["ArmorProtective_Head"] = GdItemType.Head,
        ["ArmorProtective_Chest"] = GdItemType.Chest,
        ["ArmorProtective_Shoulders"] = GdItemType.Shoulders,
        ["ArmorProtective_Waist"] = GdItemType.Belt,
        ["ArmorProtective_Legs"] = GdItemType.Legs,
        ["ArmorJewelry_Medal"] = GdItemType.Medal,
        ["ArmorJewelry_Amulet"] = GdItemType.Amulet,
        ["ArmorJewelry_Ring"] = GdItemType.Ring,
        ["WeaponArmor_Offhand"] = GdItemType.OffhandFocus,
        ["WeaponArmor_Shield"] = GdItemType.Shield,
        ["WeaponMelee_Sword"] = GdItemType.WeaponOneHandedSword,
        ["WeaponMelee_Mace"] = GdItemType.WeaponOneHandedMace,
        ["WeaponMelee_Axe"] = GdItemType.WeaponOneHandedAxe,
        ["WeaponHunting_Ranged1h"] = GdItemType.WeaponOneHandedGun,
        ["WeaponMelee_Scepter"] = GdItemType.WeaponScepter,
        ["WeaponMelee_Dagger"] = GdItemType.WeaponDagger,
        ["WeaponMelee_Sword2h"] = GdItemType.WeaponTwoHandedSword,
        ["WeaponMelee_Mace2h"] = GdItemType.WeaponTwoHandedMace,
        ["WeaponMelee_Axe2h"] = GdItemType.WeaponTwoHandedAxe,
        ["WeaponHunting_Ranged2h"] = GdItemType.WeaponTwoHandedGun,
    };

    public async Task<(IList<GdItem> items, IList<GdItem> prefixes, IList<GdItem> suffixes)> GetItemsAsync()
    {
        await arz.EnsureLoadedAsync();

        var items = new ConcurrentBag<GdItem>();
        var prefixes = new ConcurrentBag<GdItem>();
        var suffixes = new ConcurrentBag<GdItem>();
        await Parallel.ForEachAsync(arz.GetDbrData(DbrFileRecordsItemsRegex()), async (dbr, ct) =>
        {
            if (!dbr.TryGetStringValue("Class", out var @class)) return;

            // affixes
            if (@class == "LootRandomizer" && dbr.TryGetStringValue("lootRandomizerName", out var affixNameTag))
            {
                var affixType = PrefixTagRegex().IsMatch(affixNameTag) ? GdItemAffixType.Prefix
                    : SuffixTagRegex().IsMatch(affixNameTag) ? GdItemAffixType.Suffix
                    : throw new InvalidOperationException();
                var affix = new GdItem
                {
                    Name = arz.GetTag(affixNameTag)!,
                    DbrPath = dbr.Path,
                    Rarity = dbr.TryGetStringValue("itemClassification", out var affixItemClassification)
                        && Enum.TryParse<GdItemRarity>(affixItemClassification, out var affixRarity) ? affixRarity : GdItemRarity.Broken,
                };
                ReadStats(affix.Stats, dbr);
                (affixType is GdItemAffixType.Prefix ? prefixes : suffixes).Add(affix);
                return;
            }

            // base items
            if (!itemTypeMapping.TryGetValue(@class, out var itemType))
                return;

            if (!dbr.TryGetStringValue("itemNameTag", out var itemTagNameDbr) || arz.GetTag(itemTagNameDbr) is not { } name)
                if (dbr.TryGetStringValue("itemSkillName", out var itemSkillNameDbr))
                {
                    var itemDbr = NavigateSkillToLeafSkill(arz.GetDbrData(itemSkillNameDbr)!, out _, out _);
                    name = arz.GetTag(itemDbr!.GetStringValue("skillDisplayName"))!;
                }
                else
                    return;

            var item = new GdItem
            {
                Name = name,
                Description = dbr.TryGetStringValue("itemText", out var tagItemText) && arz.GetTag(tagItemText) is { } description
                    ? description : null,
                DbrPath = dbr.Path,
                Type = itemType,
                Rarity = dbr.TryGetStringValue("itemClassification", out var itemClassification)
                    && Enum.TryParse<GdItemRarity>(itemClassification, out var rarity) ? rarity : GdItemRarity.Broken,
                BitmapPath = dbr.TryGetStringValue("bitmap", out var bitmap) ? bitmap : dbr.GetStringValue("artifactBitmap"),
            };
            ReadStats(item.Stats, dbr);
            items.Add(item);
        });

        return (items.ToArray(), prefixes.ToArray(), suffixes.ToArray());
    }

    public async Task<IList<GdEquipSlot>> GetEquipSlotsAsync()
    {
        await arz.EnsureLoadedAsync();

        var master = arz.GetDbrData("records/ui/character/character_mastertable.dbr")!;
        var equipSlots = Enum.GetValues<EquipSlotType>().Select(type =>
        {
            var parser = arz.GetDbrData(master.GetStringValue($"equip{type}"))!;
            return new GdEquipSlot
            {
                Type = type,
                X = parser.GetIntegerValue("itemX"),
                Y = parser.GetIntegerValue("itemY"),
                Width = parser.GetIntegerValue("itemXSize"),
                Height = parser.GetIntegerValue("itemYSize"),
                SilhouetteBitmapPath = parser.GetStringValue("silhouette"),
            };
        }).ToArray();

        return equipSlots;
    }

    public IList<GdsCharacter> GetCharacterList(string gdPath, bool headerOnly = false) =>
        Directory.EnumerateFiles(gdPath, "player.gdc", SearchOption.AllDirectories)
            .Select(path => ParseSaveFile(Path.GetDirectoryName(path)!, headerOnly))
            .OrderBy(x => x.Name)
            .ToList();

    public GdsCharacter ParseSaveFile(string baseFolderPath, bool headerOnly = false)
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

        GdsSkill[]? skills = default;
        var items = new GdsItem[16];
        if (!headerOnly)
        {
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
            items = readBlock(3, _ =>
            {
                var items = new GdsItem[16];

                magic = reader.ReadEncInt32(encState);
                Debug.Assert(magic == 4);

                var flag = reader.ReadEncUInt8(encState);
                if (flag != 0)
                {
                    var sackCount = reader.ReadEncInt32(encState);
                    var focused = reader.ReadEncInt32(encState);
                    var selected = reader.ReadEncInt32(encState);

                    while (sackCount-- > 0)
                        // sack data
                        skipNextBlock();

                    var useAlternate = reader.ReadEncUInt8(encState);

                    // equipment
                    for (var i = 0; i < 12; ++i)
                        items[i] = reader.ReadItem(true, encState);

                    var alternate0 = reader.ReadEncUInt8(encState);
                    items[12] = reader.ReadItem(true, encState);
                    items[13] = reader.ReadItem(true, encState);

                    var alternate1 = reader.ReadEncUInt8(encState);
                    items[14] = reader.ReadItem(true, encState);
                    items[15] = reader.ReadItem(true, encState);
                }

                return items;
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
            skills = readBlock(8, _ =>
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
                    var itemId = reader.ReadEncString(encState);

                    return (name, autoCastSkill, autoCastController, itemSlot, itemId);
                });

                return skills;
            });

            // don't care about the rest
        }

        // build result
        return new(charName, classIndex1, classIndex2, level, skills ?? [], items ?? []);
    }

    [GeneratedRegex("^records/items/(?!enemygear|enchants|lootchests|lootaffixes|loreobjects|materia|questitems|transmutes)|^records/items/lootaffixes/(?:prefix|suffix)")]
    private static partial Regex DbrFileRecordsItemsRegex();

    [GeneratedRegex("^tag.*Prefix.")]
    private static partial Regex PrefixTagRegex();

    [GeneratedRegex("^tag.*Suffix.")]
    private static partial Regex SuffixTagRegex();
}
