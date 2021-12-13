using System;
using System.Collections.Generic;
using System.IO;
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
        private const string ModGuid = "azumatt.skillcapper";
        private const string ConfigFileName = "azumatt_skillcapper_config.yaml";
        private static string _skillConfigPath = null!;
        private static SortedDictionary<string, SkillConfig> skillConfigs = new();


        /*private static IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();*/

        private Harmony _harmony;
        private static readonly ManualLogSource SCLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync? configSync = new(ModName)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private static readonly CustomSyncedValue<string> skillConfigData = new(configSync, "skillConfig", "");

        #region UnityEvents

        private void Awake()
        {
            /*ConfigInit();*/
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = configSync?.AddLockingConfigEntry(_serverConfigLocked);

            if (!File.Exists(Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName))
            {
                File.Create(Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName);
            }

            _skillConfigPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

            _harmony = new Harmony(ModGuid);

            _harmony.PatchAll();
            ReadYamlConfigFile(null!, null!);
            skillConfigData.ValueChanged += OnValChangedUpdate;

            SetupWatcher();
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }

        #endregion

        #region HarmonyPatches

        [HarmonyPatch]
        private static class SKillCap
        {
            [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
            [HarmonyPrefix]
            private static bool Prefix(Skills.Skill __instance)
            {
                SCLogger.LogDebug("Skill Being Raised-----------------:" +
                                  __instance.m_info.m_skill.ToString().ToUpper() + "a.k.a." +
                                  Localization.instance.Localize("$skill_" + __instance.m_info.m_skill.GetHashCode()));
                foreach (KeyValuePair<string, SkillConfig> skillConfig in skillConfigs)
                {
                    /* This mainly works only for Vanilla skills, check where possible
                     Jotunn mods that add skills add via hash...so this would never match unless you have the has of the skill as well.
                     */
                    if (string.Equals(__instance.m_info.m_skill.ToString(), skillConfig.Key,
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        SCLogger.LogDebug("Skill matched!");
                        SCLogger.LogDebug(skillConfig.Value.Level);
                        return __instance.m_level < skillConfig.Value.Level;
                    }

                    /* If it's a skill from SkillManager check for it using the localized name/hash.
                     Thanks Blaxxun for making it easy to provide compatibility 
                     */
                    if (string.Equals(skillConfig.Key,
                            Localization.instance.Localize("$skill_" + __instance.m_info.m_skill.GetHashCode()),
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        SCLogger.LogDebug("Skill hash matched! " +
                                          Localization.instance.Localize("$skill_" +
                                                                         __instance.m_info.m_skill.GetHashCode()));
                        SCLogger.LogDebug(skillConfig.Value.Level);
                        return __instance.m_level < skillConfig.Value.Level;
                    }
                }


                return true;
            }

            [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
            [HarmonyPostfix]
            private static void Postfix(Skills.Skill __instance)
            {
                SCLogger.LogDebug("Skillbeingraised-----------------:" +
                                  __instance.m_info.m_skill.ToString().ToUpper());
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
                SCLogger.LogWarning("ReadYamlConfigFile called");
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
                SCLogger.LogError($"There was an issue loading your {ConfigFileName}");
                SCLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private static void OnValChangedUpdate()
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            SCLogger.LogWarning("OnValChanged called");
            try
            {
                skillConfigs = new SortedDictionary<string, SkillConfig>(
                    deserializer.Deserialize<Dictionary<string, SkillConfig>?>(skillConfigData.Value) ??
                    new Dictionary<string, SkillConfig>());
                foreach (SkillConfig skillConfig in skillConfigs.Values)
                {
                    // yamldotnet helpfully nulls fields if empty
                    // ReSharper disable ConstantNullCoalescingCondition
                    SkillConfig skillConfig1 = skillConfig;
                    skillConfig1.Level ??= new float();
                    // ReSharper restore ConstantNullCoalescingCondition
                }
            }
            catch (Exception e)
            {
                SCLogger.LogError($"Failed to deserialize skillConfig: {e}");
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