using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.UI;
using HarmonyLib;

namespace CardTarkov.Client.UI
{
    [HarmonyPatch]
    internal static class InventoryScreenShowPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(InventoryScreen))
                .Where(method => method.Name == "Show");

        private static void Prefix(InventoryScreen __instance)
        {
            try
            {
                CardTarkovTab.TryAttach(__instance);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogWarning($"[CardTarkov] 注入 Tab 失败：{ex}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryScreen), nameof(InventoryScreen.Close))]
    internal static class InventoryScreenClosePatch
    {
        private static void Postfix(InventoryScreen __instance)
        {
            try
            {
                CardTarkovTab.ShutdownForScreen(__instance);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogWarning($"[CardTarkov] 关闭时清理牌局失败：{ex}");
            }
        }
    }
}
