using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PlanetWormhole.Data;
using System.Collections.Generic;
using System.Threading;
using static PlanetWormhole.Constants;

namespace PlanetWormhole
{
    [BepInPlugin(package, plugin, version)]
    public class PlanetWormhole: BaseUnityPlugin
    {
        private const string package = "essium.DSP.PlanetWormhole";
        private const string plugin = "PlanetWormhole";
        private const string version = "2.0.3";

        private static List<LocalPlanet> planetWormhole;
        private static Cosmic globalWormhole;
        private static ManualLogSource logger;

        private static ConfigEntry<bool> enableInterstellar;
        /// <summary>MK3 缺货时是否回退使用 MK2/MK1 增产剂（默认关闭，保持原版行为）</summary>
        private static ConfigEntry<bool> enableLowerTierProliferator;
        private Harmony harmony;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.OnFactoryFrameEnd))]
        private static void _postfix_GameData_GameTick(GameLogic __instance)
        {
            if (GameMain.instance.isMenuDemo)
            {
                return;
            }
            DeepProfiler.BeginSample(DPEntry.Belt, -1, -1L);
            while (planetWormhole.Count < __instance.factoryCount)
            {
                planetWormhole.Add(new LocalPlanet());
            }
            globalWormhole.SetData(__instance.data);
            globalWormhole.BeforeLocal();
            for (int i = (int)(__instance.timei % PERIOD); i < __instance.factoryCount; i+=PERIOD)
            {
                planetWormhole[i].SetFactory(__instance.factories[i]);
                planetWormhole[i].SetCosmic(globalWormhole);
                ThreadPool.QueueUserWorkItem(planetWormhole[i].PatchPlanet);
            }
            for(int i = (int)(__instance.timei % PERIOD); i < __instance.factoryCount; i += PERIOD)
            {
                planetWormhole[i].completeSignal.WaitOne();
            }
            globalWormhole.AfterLocal();
            DeepProfiler.EndSample(-1, -2L);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick))]
        private static void _prefix_ProductionStatistics_GameTick(ProductionStatistics __instance)
        {
            for (int i = 0; i < __instance.gameData.factoryCount; i++)
            {
                if (i > planetWormhole.Count || planetWormhole[i].consumedProliferator <= 0) continue;
                __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK1] += planetWormhole[i].consumedProliferatorMk1;
                __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK2] += planetWormhole[i].consumedProliferatorMk2;
                __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK3] += planetWormhole[i].consumedProliferatorMk3;
                planetWormhole[i].consumedProliferator = 0;
                planetWormhole[i].consumedProliferatorMk1 = 0;
                planetWormhole[i].consumedProliferatorMk2 = 0;
                planetWormhole[i].consumedProliferatorMk3 = 0;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick_Parallel))]
        private static void _prefix_ProductionStatistics_GameTick_Parallel(ProductionStatistics __instance, int threadOrdinal, int threadCount)
        {
            for (int i = (threadOrdinal + 3) % threadCount; i < __instance.gameData.factoryCount; i += threadCount)
            {
                if (i > planetWormhole.Count || planetWormhole[i].consumedProliferator <= 0) continue;
                __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK1] += planetWormhole[i].consumedProliferatorMk1;
                __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK2] += planetWormhole[i].consumedProliferatorMk2;
                __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK3] += planetWormhole[i].consumedProliferatorMk3;
                planetWormhole[i].consumedProliferator = 0;
                planetWormhole[i].consumedProliferatorMk1 = 0;
                planetWormhole[i].consumedProliferatorMk2 = 0;
                planetWormhole[i].consumedProliferatorMk3 = 0;
            }
        }

        /*
        [HarmonyTranspiler, HarmonyPatch(typeof(UIBuildMenu), "SetCurrentCategory")]
        private static IEnumerable<CodeInstruction> _tranpiler_UIBuildMenu_SetCurrentCategory(IEnumerable<CodeInstruction> instructions)
        {
            return instructions;
        }
        */

        public void Start()
        {
            BindConfig();
            harmony = new Harmony(package + ":" + version);
            harmony.PatchAll(typeof(PlanetWormhole));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            BepInEx.Logging.Logger.Sources.Remove(logger);
        }

        private void BindConfig()
        {
            enableInterstellar = Config.Bind("Config", "EnableInterstellar", false, "enable auto interstellar transportation");
            enableLowerTierProliferator = Config.Bind("Config", "EnableLowerTierProliferator", false, "use MK2/MK1 proliferator when MK3 is out of stock");
        }

        /// <summary>获取是否启用低档位增产剂（MK2/MK1）回补</summary>
        public static bool EnableLowerTierProliferator()
        {
            return enableLowerTierProliferator.Value;
        }

        public static bool EnableInterstellar()
        {
            return enableInterstellar.Value;
        }

        static PlanetWormhole()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(plugin);
            planetWormhole = new List<LocalPlanet>();
            globalWormhole = new Cosmic();
        }

        public static void LogInfo(string msg)
        {
            logger.LogInfo(msg);
        }
    }
}
