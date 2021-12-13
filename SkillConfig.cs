using System.Collections.Generic;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace SkillCapper
{
    public struct SkillConfig 
    {
        [YamlMember(Alias = "level", ApplyNamingConventions = false)]
        public float? Level { get; set; }
    }
}