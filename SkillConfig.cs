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
            ListSkillsDefault.Add("swords", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("knives", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("club", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("polearms", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("spears", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("blocking", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("axes", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("bows", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("unarmed", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("pickaxes", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("woodcutting", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("sneak", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("swim", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("run", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("jump", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("elementalmagic", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("bloodmagic", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("tenacity", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("vitality", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("packhorse", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("evasion", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("building", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("cooking", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("cartography", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("fitness", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("athletics", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("gathering", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("sailing", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("discipline", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("abjuration", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("alteration", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("conjuration", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("evocation", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("illusion", new SkillConfig { Level = 100 });
            ListSkillsDefault.Add("jewelcrafting", new SkillConfig { Level = 100 });

            ISerializer serializer = new SerializerBuilder().DisableAliases()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string yaml = serializer.Serialize(ListSkillsDefault);
            File.WriteAllText(ScPlugin._skillConfigPath, yaml);
        }
    }
}