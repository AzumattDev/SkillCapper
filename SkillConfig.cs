using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using JetBrains.Annotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SkillCapper
{
    public struct SkillConfig
    {
        [YamlMember(Alias = "level", ApplyNamingConventions = false)]
        public int? Level { get; set; }
    }

    internal static class WriteDefaults
    {
        private static readonly Dictionary<string, SkillConfig> ListSkillsDefault = new();

        internal static void WriteDefaultValues()
        {
            ListSkillsDefault.Add("Swords", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Knives", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Club", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Polearms", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Spears", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Blocking", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Axes", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Bows", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Unarmed", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Pickaxes", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Woodcutting", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Sneak", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Swim", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Run", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Jump", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("ElementalMagic", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("BloodMagic", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Tenacity", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Vitality", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Packhorse", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Evasion", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Building", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Cooking", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Cartography", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Fitness", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Athletics", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Gathering", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Sailing", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Discipline", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Abjuration", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Alteration", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Conjuration", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Evocation", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Illusion", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("Jewelcrafting", new SkillConfig { Level = 100 });


            ISerializer serializer = new SerializerBuilder().DisableAliases()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string yaml = serializer.Serialize(ListSkillsDefault);
            File.WriteAllText(ScPlugin._skillConfigPath, yaml);
        }
    }
}