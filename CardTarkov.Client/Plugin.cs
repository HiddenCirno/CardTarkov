using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CardTarkov.Client
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.hiddencirno.cardtarkov";
        internal const string PluginName = "CardTarkov";
        internal const string PluginVersion = "0.1.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            new Harmony(PluginGuid).PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} 已加载。");
        }
    }
}
