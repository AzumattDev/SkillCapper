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
                    if (string.Equals(__instance.m_info.m_skill.ToString(), skillConfig.Key,
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        SCLogger.LogDebug("Skill matched!");
                        SCLogger.LogDebug(skillConfig.Value.Level);
                        return __instance.m_level < skillConfig.Value.Level;
                    }

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

                /*return __instance.m_info.m_skill.ToString() switch
                {
                    "Swords" => __instance.m_level < swords.Value,
                    "Knives" => __instance.m_level < knives.Value,
                    "Clubs" => __instance.m_level < clubs.Value,
                    "Polearms" => __instance.m_level < polearms.Value,
                    "Spears" => __instance.m_level < spears.Value,
                    "Blocking" => __instance.m_level < blocking.Value,
                    "Axes" => __instance.m_level < axes.Value,
                    "Bows" => __instance.m_level < bows.Value,
                    "Unarmed" => __instance.m_level < unarmed.Value,
                    "Pickaxes" => __instance.m_level < pickaxes.Value,
                    "WoodCutting" => __instance.m_level < woodCutting.Value,
                    "Jump" => __instance.m_level < jump.Value,
                    "Sneak" => __instance.m_level < sneak.Value,
                    "Run" => __instance.m_level < run.Value,
                    "Swim" => __instance.m_level < swim.Value,
                    "Cooking" => __instance.m_level < cooking.Value,
                    "Cartography" => __instance.m_level < cartography.Value,
                    "Fitness" => __instance.m_level < fitness.Value,
                    "Athletics" => __instance.m_level < athletics.Value,
                    "Gathering" => __instance.m_level < gathering.Value,
                    "Sailing" => __instance.m_level < sailing.Value,
                    "Discipline" => __instance.m_level < discipline.Value,
                    "Abjuration" => __instance.m_level < abjuration.Value,
                    "Alteration" => __instance.m_level < alteration.Value,
                    "Conjuration" => __instance.m_level < conjuration.Value,
                    "Evocation" => __instance.m_level < evocation.Value,
                    "Illusion" => __instance.m_level < illusion.Value,
                    _ => __instance.m_level < skillCap.Value
                };*/
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
        public static ConfigEntry<float> skillCap;
        public static ConfigEntry<float> swords;
        public static ConfigEntry<float> knives;
        public static ConfigEntry<float> clubs;
        public static ConfigEntry<float> polearms;
        public static ConfigEntry<float> spears;
        public static ConfigEntry<float> blocking;
        public static ConfigEntry<float> axes;
        public static ConfigEntry<float> bows;
        public static ConfigEntry<float> unarmed;
        public static ConfigEntry<float> pickaxes;
        public static ConfigEntry<float> woodCutting;
        public static ConfigEntry<float> jump;
        public static ConfigEntry<float> sneak;
        public static ConfigEntry<float> run;
        public static ConfigEntry<float> swim;
        public static ConfigEntry<float> cooking;
        public static ConfigEntry<float> cartography;
        public static ConfigEntry<float> fitness;
        public static ConfigEntry<float> athletics;
        public static ConfigEntry<float> gathering;
        public static ConfigEntry<float> sailing;
        public static ConfigEntry<float> discipline;
        public static ConfigEntry<float> abjuration;
        public static ConfigEntry<float> alteration;
        public static ConfigEntry<float> conjuration;
        public static ConfigEntry<float> evocation;
        public static ConfigEntry<float> illusion;

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


        private void ConfigInit()
        {
            /*_serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = configSync.AddLockingConfigEntry(_serverConfigLocked);*/

            /*skillCap = config("General", "Default Skill Cap Value", 100f,
                new ConfigDescription("Default skill cap for all skills not listed in this config file",
                    new AcceptableValueRange<float>(0.0f, 100f)));
            swords = config("General", "Swords Cap Value", 100f,
                new ConfigDescription("Sword Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            knives = config("General", "Knives Cap Value", 100f,
                new ConfigDescription("Knives Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            clubs = config("General", "Clubs Cap Value", 100f,
                new ConfigDescription("Clubs Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            polearms = config("General", "Polearms Cap Value", 100f,
                new ConfigDescription("Polearms Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            spears = config("General", "Spears Cap Value", 100f,
                new ConfigDescription("Spears Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            blocking = config("General", "Blocking Cap Value", 100f,
                new ConfigDescription("Blocking Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            axes = config("General", "Axes Cap Value", 100f,
                new ConfigDescription("Axes Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            bows = config("General", "Bows Cap Value", 100f,
                new ConfigDescription("Bows Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            unarmed = config("General", "Unarmed Cap Value", 100f,
                new ConfigDescription("Unarmed Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            pickaxes = config("General", "Pickaxes Cap Value", 100f,
                new ConfigDescription("Pickaxes Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            woodCutting = config("General", "WoodCutting Cap Value", 100f,
                new ConfigDescription("Wood Cutting Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            jump = config("General", "Jump Cap Value", 100f,
                new ConfigDescription("Jump Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            sneak = config("General", "Sneak Cap Value", 100f,
                new ConfigDescription("Sneak Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            run = config("General", "Run Cap Value", 100f,
                new ConfigDescription("Run Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            swim = config("General", "Swim Cap Value", 100f,
                new ConfigDescription("Swim Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            cooking = config("General", "Cooking Cap Value", 100f,
                new ConfigDescription("Cooking Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            cartography = config("General", "Cartography Cap Value", 100f,
                new ConfigDescription("Cartography Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            fitness = config("General", "Fitness Cap Value", 100f,
                new ConfigDescription("Fitness Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            athletics = config("General", "Athletics Cap Value", 100f,
                new ConfigDescription("Athletics Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            gathering = config("General", "Gathering Cap Value", 100f,
                new ConfigDescription("Gathering Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            sailing = config("General", "Sailing Cap Value", 100f,
                new ConfigDescription("Sailing Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            discipline = config("Valheim Legends", "Discipline Cap Value", 100f,
                new ConfigDescription("Discipline Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            abjuration = config("Valheim Legends", "Abjuration Cap Value", 100f,
                new ConfigDescription("Abjuration Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            alteration = config("Valheim Legends", "Alteration Cap Value", 100f,
                new ConfigDescription("Alteration Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            conjuration = config("Valheim Legends", "Conjuration Cap Value", 100f,
                new ConfigDescription("Conjuration Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            evocation = config("Valheim Legends", "Evocation Cap Value", 100f,
                new ConfigDescription("Evocation Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));
            illusion = config("Valheim Legends", "Illusion Cap Value", 100f,
                new ConfigDescription("Illusion Skill Cap", new AcceptableValueRange<float>(0.0f, 100f)));*/
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
                    SCLogger.LogError(skillConfigs.Values.ToString());
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