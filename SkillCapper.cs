using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using SkillCapper.Util;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SkillCapper
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ScPlugin : BaseUnityPlugin
    {
        public const string ModVersion = "3.0.4";
        public const string ModName = "SkillCapper";
        internal const string Author = "Azumatt";
        private const string ModGuid = $"{Author}.{ModName}";
        private const string ConfigFileName = $"{ModGuid}Config.yaml";
        private const string ReferenceFileName = $"{Author}.SkillReferenceFile.txt";
        internal static string ReferenceFilePath = Paths.ConfigPath + Path.DirectorySeparatorChar + ReferenceFileName;
        internal static string _skillConfigPath = null!;
        private static SortedDictionary<string, SkillConfig> skillConfigs = new();
        internal static Dictionary<int, int> cappedvalues = new();
        internal static string ConnectionError = "";

        private Harmony? _harmony;
        internal static readonly ManualLogSource ScLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private static readonly CustomSyncedValue<string> SkillConfigData = new(ConfigSync, "skillConfig", "");

        #region UnityEvents

        private void Awake()
        {
            ConfigSync.IsLocked = true;

            _skillConfigPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
            if (!File.Exists(_skillConfigPath))
            {
                File.Create(_skillConfigPath).Dispose();
                WriteDefaults.WriteDefaultValues();
            }

            if (!File.Exists(ReferenceFilePath))
            {
                File.Create(ReferenceFilePath).Dispose();
            }

            _harmony = new Harmony(ModGuid);

            SkillConfigData.ValueChanged += OnValChangedUpdate;

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
                ScLogger.LogDebug($"Skill Being Raised-----------------: {__instance.m_info.m_skill.ToString().ToUpper()} a.k.a. {Localization.instance.Localize("$skill_" + __instance.m_info.m_skill.GetHashCode())}");
                foreach (KeyValuePair<string, SkillConfig> skillConfig in skillConfigs)
                {
                    /* This mainly works only for Vanilla skills, check where possible */

                    if (string.Equals(__instance.m_info.m_skill.ToString(), skillConfig.Key,
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        ScLogger.LogDebug($"Skill {skillConfig.Key} matched!{Environment.NewLine}Level Cap: {skillConfig.Value.Level}");
                        return __instance.m_level < skillConfig.Value.Level;
                    }

                    /* If it's a skill from SkillManager check for it using the localized name/hash.
                     Thanks Blaxxun for making it easy to provide compatibility, Jotunn has adopted this for compatibility, but not sure if it works with it yet.
                    */
                    if (string.Equals(skillConfig.Key, Localization.instance.Localize($"$skill_{__instance.m_info.m_skill.GetHashCode()}"),
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        ScLogger.LogDebug($"Skill {skillConfig.Key}, hash matched!{Environment.NewLine}Level Cap: {skillConfig.Value.Level}");
                        return __instance.m_level < skillConfig.Value.Level;
                    }
                }


                return true;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return Functions.LimitSkillTranspiler(instructions);
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
        static class SkillsGetSkillFactorPatch
        {
            static void Prefix(Skills __instance, Skills.SkillType skillType, ref float __result)
            {
                if (skillType == Skills.SkillType.None)
                {
                    __result = 0f;
                }
                else
                {
                    float skillLevel = __instance.GetSkillLevel(skillType);

                    // skill factor increases linearly from 0 to 2 as skill level goes from 0 to 200, 
                    // but is never less than 0
                    __result = Mathf.Max(0, skillLevel / 100f);
                    // Log the result
                    ScLogger.LogDebug($"Skill Factor: {__result}");
                    ScLogger.LogDebug($"Normal Skill Factor: {Mathf.Clamp01(__instance.GetSkillLevel(skillType) / 100f)}");
                }
            }

            /*[HarmonyEmitIL("C:\\Users\\crypt\\AppData\\Roaming\\r2modmanPlus-local\\Valheim\\profiles\\TEST\\BepInEx\\plugins\\Azumatt-SkillCapper")]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return Functions.LimitSkillTranspiler(instructions);
            }*/
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill))]
        static class SkillsCheatRaiseSkillPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return Functions.LimitSkillTranspiler(instructions);
            }
        }

        [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.GetLevelPercentage))]
        static class SkillsSkillGetLevelPercentagePatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return Functions.LimitSkillTranspiler(instructions);
            }
        }

        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
        static class SkillsDialogSkillSetUpPatch
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

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return Functions.LimitSkillTranspiler(instructions);
            }
        }

        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
        static class SkillAwakeFileCreationPatch
        {
            static void Postfix(SkillsDialog __instance, ref Player player)
            {
#if DEBUG
                // TODO: Make this work with custom skills, not just those defined in the enum from vanilla.
                StringBuilder builder = new();
                List<string> list = ((IEnumerable<string>)Enum.GetNames(typeof(Skills.SkillType))).ToList<string>();
                list.Remove(Skills.SkillType.All.ToString());
                list.Remove(Skills.SkillType.None.ToString());

                foreach (string s in list)
                {
                    builder.AppendLine(s);
                }

                File.WriteAllText(ReferenceFilePath, builder.ToString());
#endif
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        static class PlayerOnSpawnedPlayerPatch
        {
            static void Postfix(Player __instance)
            {
                /* Make sure to sync the values each time a player spawns */
                cappedvalues.Clear();
                foreach (Skills.Skill? skill in __instance.GetSkills().GetSkillList())
                {
                    string[] split = skill.m_info.m_description.Split('_');

                    if (split.Length < 2)
                    {
                        ScLogger.LogDebug($"------------------------------SKILL NAME Description Not able to split: {skill.m_info.m_description}");
                        continue;
                    }

                    string name = split[1].ToLower();
                    if (!skillConfigs.ContainsKey(name)) continue;

                    cappedvalues.Add((int)skill.m_info.m_skill, (int)skillConfigs[name].Level);
                    ScLogger.LogDebug(
                        $"------------------------------SKILL NAME VAR: {name}, SKILL TYPE {skill.m_info.m_skill}, CONFIG CAP LEVEL {(int)skillConfigs[name].Level}");
                }
            }
        }

        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetCurrentDay))]
        static class EnvManGetCurrentDayPatch
        {
            static void Postfix(EnvMan __instance)
            {
                /* For players that might not die as much or at all. Update the values in their list every morning in game */
                if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead() || Player.m_localPlayer.IsTeleporting())
                    return;

                cappedvalues.Clear();

                if (skillConfigs == null) return;

                foreach (Skills.Skill? skill in Player.m_localPlayer.GetSkills().GetSkillList())
                {
                    string[] split = skill.m_info.m_description.Split('_');

                    if (split.Length < 2)
                        continue;

                    string name = split[1].ToLower();

                    if (!skillConfigs.ContainsKey(name))
                        continue;
                    
                    if (skillConfigs[name].Level == null) continue;
                    if (cappedvalues.ContainsKey((int)skill.m_info.m_skill)) continue;
                    int? level = skillConfigs[name].Level;
                    if (level != null)
                        cappedvalues.Add((int)skill.m_info.m_skill, (int)level);
                }
            }
        }

        #endregion

        #region ConfigSetup

        /*private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }*/

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
                SkillConfigData.AssignLocalValue(File.ReadAllText(_skillConfigPath));
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
                    deserializer.Deserialize<Dictionary<string, SkillConfig>?>(SkillConfigData.Value) ??
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
    }
}