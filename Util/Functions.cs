using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace SkillCapper.Util;

public class Functions
{
    internal static IEnumerable<CodeInstruction> LimitSkillTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var limitMethod = AccessTools.Method(typeof(Functions), nameof(AzuLimitSkill));

        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 100f)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0); // Load 'this' onto the stack
                yield return new CodeInstruction(OpCodes.Call, limitMethod); // Call AzuLimitSkill
            }
            else
            {
                yield return instruction;
            }
        }
    }

    public static float AzuLimitSkill(Skills.Skill skill)
    {
        /*// Log the skill and teh capped value
        ScPlugin.ScLogger.LogWarning(ScPlugin.cappedvalues.ContainsKey((int)skill.m_info.m_skill) ? $"Skill: {skill.m_info.m_skill} Capped Value: {ScPlugin.cappedvalues[(int)skill.m_info.m_skill]}" : $"Logging 100 for skill {skill.m_info.m_skill}");
// Print the keys
        foreach (var key in ScPlugin.cappedvalues.Keys)
        {
            ScPlugin.ScLogger.LogWarning($"Key: {key}");
        }*/

        return ScPlugin.cappedvalues.ContainsKey((int)skill.m_info.m_skill)
            ? ScPlugin.cappedvalues[(int)skill.m_info.m_skill]
            : 100f;
    }

    private static Skills.Skill GetSkillToCap(Skills.SkillType skilltype)
    {
        Skills.Skill skill1;
        Skills? skilldata = null;
        if (skilldata != null && skilldata.m_skillData.TryGetValue(skilltype, out skill1))
            return skill1;
        Skills.Skill skill2 = new(skilldata?.GetSkillDef(skilltype));
        skilldata?.m_skillData.Add(skilltype, skill2);
        return skill2;
    }
}