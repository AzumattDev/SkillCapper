using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SkillCapper
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ScPlugin : BaseUnityPlugin
    {
        public const string ModVersion = "2.0.0";
        public const string ModName = "Skill Capper";
        internal const string Author = "Azumatt";
        private const string ModGuid = "azumatt.skillCapper";
        private const string ConfigFileName = "azumatt_skillcapper_config.yaml";
        internal static string _skillConfigPath = null!;
        private static SortedDictionary<string, SkillConfig> skillConfigs = new();
        private static List<CodeInstruction> _codeInstructions = new();
        private static List<CodeInstruction> _codeInstructionsSkillDialog = new();
        public static MethodInfo TranspilerMethod;
        private static Dictionary<int, int> cappedvalues = new();

        private Harmony? _harmony;
        private static readonly ManualLogSource ScLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync? configSync = new(ModName)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private static readonly CustomSyncedValue<string> skillConfigData = new(configSync, "skillConfig", "");

        #region UnityEvents

        public static float AzuLimitSkill(Skills.Skill skill)
        {
            return cappedvalues.ContainsKey((int)skill.m_info.m_skill) ? cappedvalues[(int)skill.m_info.m_skill] : 100;
        }


        private void Awake()
        {
            /*ConfigInit();*/
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = configSync?.AddLockingConfigEntry(_serverConfigLocked);


            _skillConfigPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
            if (!File.Exists(Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName))
            {
                File.Create(Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName).Dispose();
                WriteDefaults.WriteDefaultValues();
            }

            _harmony = new Harmony(ModGuid);

            skillConfigData.ValueChanged += OnValChangedUpdate;
            TranspilerMethod = AccessTools.Method(typeof(ScPlugin), nameof(AzuLimitSkill),
                new Type[] { typeof(Skills.Skill) });
            _codeInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
            _codeInstructions.Add(new CodeInstruction(OpCodes.Call, TranspilerMethod));

            _codeInstructionsSkillDialog.Add(new CodeInstruction(OpCodes.Ldloc_S, 4));
            _codeInstructionsSkillDialog.Add(new CodeInstruction(OpCodes.Call, TranspilerMethod));


            ReadYamlConfigFile(null!, null!);
            SetupWatcher();

            _harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        #endregion

        #region HarmonyPatches

        [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
        private static class SKillCap
        {
            private static bool Prefix(Skills.Skill __instance)
            {
                ScLogger.LogDebug("Skill Being Raised-----------------: " +
                                  __instance.m_info.m_skill.ToString().ToUpper() + " a.k.a. " +
                                  Localization.instance.Localize("$skill_" + __instance.m_info.m_skill.GetHashCode()));
                foreach (KeyValuePair<string, SkillConfig> skillConfig in skillConfigs)
                {
                    /* This mainly works only for Vanilla skills, check where possible
                     */

                    if (string.Equals(__instance.m_info.m_skill.ToString(), skillConfig.Key,
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        ScLogger.LogDebug("Skill matched!");
                        ScLogger.LogDebug(skillConfig.Value.Level);
                        return __instance.m_level < skillConfig.Value.Level;
                    }

                    /* If it's a skill from SkillManager check for it using the localized name/hash.
                     Thanks Blaxxun for making it easy to provide compatibility, Jotunn will soon adopt this for compatibility 
                     */
                    if (string.Equals(skillConfig.Key,
                            Localization.instance.Localize("$skill_" + __instance.m_info.m_skill.GetHashCode()),
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        ScLogger.LogDebug("Skill hash matched! " +
                                          Localization.instance.Localize("$skill_" +
                                                                         __instance.m_info.m_skill.GetHashCode()));
                        ScLogger.LogDebug(skillConfig.Value.Level);
                        return __instance.m_level < skillConfig.Value.Level;
                    }
                }


                return true;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new(instructions);
                list.RemoveAt(2);
                list.InsertRange(2, _codeInstructions);
                list.RemoveAt(36);
                list.InsertRange(36, _codeInstructions);
                //list[2].operand = 100;
                //list[35].operand = 100;
                return list;
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
        static class Skills_GetSkillFactor_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new(instructions);
                list.RemoveAt(7);
                list.InsertRange(7, _codeInstructions);
                //list[7].operand = 100;
                return list;
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill))]
        static class Skills_CheatRaiseSkill_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new(instructions);
                list.RemoveAt(41);
                list.InsertRange(41, _codeInstructions);
                //list[41].operand = 100f;
                return list;
            }
        }

        [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.GetLevelPercentage))]
        static class Skills_SkillGetLevelPercentage_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new(instructions);
                list.RemoveAt(2);
                list.InsertRange(2, _codeInstructions);
                // list[2].operand = 100;
                return list;
            }
        }

        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
        static class SkillsDialog_SkillSetUp_Patch
        {
            static void Prefix(SkillsDialog __instance, ref Player player)
            {
                /* Make sure to sync the values each time a player opens the menu */
                cappedvalues.Clear();
                foreach (Skills.Skill? skill in player.GetSkills().GetSkillList())
                {
                    string[] split = skill.m_info.m_description.Split('_');

                    if (split.Length < 2) continue;

                    string name = split[1].ToLower();

                    if (!skillConfigs.ContainsKey(name)) continue;

                    cappedvalues.Add((int)skill.m_info.m_skill, (int)skillConfigs[name].Level);
                }
            }


            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new(instructions);
                list.RemoveAt(138);
                list.InsertRange(138, _codeInstructionsSkillDialog);
                list.RemoveAt(149);
                list.InsertRange(149, _codeInstructionsSkillDialog);
                /*list[138].operand = 100;
                list[148].operand = 100;*/
                return list;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        static class Player_SetLocalPlayer_Patch
        {
            static void Postfix(Player __instance)
            {
                /* Make sure to sync the values each time a player spawns */
                cappedvalues.Clear();
                foreach (Skills.Skill? skill in __instance.GetSkills().GetSkillList())
                {
                    string[] split = skill.m_info.m_description.Split('_');

                    if (split.Length < 2) continue;

                    string name = split[1].ToLower();

                    if (!skillConfigs.ContainsKey(name)) continue;

                    cappedvalues.Add((int)skill.m_info.m_skill, (int)skillConfigs[name].Level);
                    ScLogger.LogDebug(
                        $"------------------------------SKILL NAME VAR: {name}, SKILL {skill.m_info.m_skill}, SKILLCONFIG LEVEL {(int)skillConfigs[name].Level}");
                }
            }
        }

        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetCurrentDay))]
        static class EnvMan_GetCurrentDay_Patch
        {
            static void Postfix(EnvMan __instance)
            {
                /* For players that might not die as much or at all. Update the values in their list every morning in game */
                if (Player.m_localPlayer == null) return;
                cappedvalues.Clear();
                foreach (Skills.Skill? skill in Player.m_localPlayer.GetSkills().GetSkillList())
                {
                    string[] split = skill.m_info.m_description.Split('_');

                    if (split.Length < 2) continue;

                    string name = split[1].ToLower();

                    if (!skillConfigs.ContainsKey(name)) continue;

                    cappedvalues.Add((int)skill.m_info.m_skill, (int)skillConfigs[name].Level);
                }
            }
        }

        #endregion

        #region ConfigSetup

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion

        #region FileWatcher

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadYamlConfigFile;
            watcher.Created += ReadYamlConfigFile;
            watcher.Renamed += ReadYamlConfigFile;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadYamlConfigFile(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(_skillConfigPath)) return;
            try
            {
                ScLogger.LogDebug("ReadYamlConfigFile called");
                StreamReader file = File.OpenText(_skillConfigPath);
                IDeserializer deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                SortedDictionary<string, SkillConfig> tmp =
                    deserializer.Deserialize<SortedDictionary<string, SkillConfig>>(file);
                skillConfigs = tmp;
                file.Close();
                skillConfigData.AssignLocalValue(File.ReadAllText(_skillConfigPath));
            }
            catch
            {
                ScLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ScLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private static void OnValChangedUpdate()
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            ScLogger.LogDebug("OnValChanged called");
            try
            {
                skillConfigs = new SortedDictionary<string, SkillConfig>(
                    deserializer.Deserialize<Dictionary<string, SkillConfig>?>(skillConfigData.Value) ??
                    new Dictionary<string, SkillConfig>());
                foreach (SkillConfig skillConfig in skillConfigs.Values)
                {
                    SkillConfig skillConfig1 = skillConfig;
                    skillConfig1.Level ??= new int();
                }
            }
            catch (Exception e)
            {
                ScLogger.LogError($"Failed to deserialize skillConfig: {e}");
            }
        }

        #endregion

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
}