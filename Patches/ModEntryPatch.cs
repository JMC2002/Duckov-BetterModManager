using Duckov.Modding.UI;
using HarmonyLib;

namespace BetterModManager.Patches
{
    [HarmonyPatch(typeof(ModEntry))]
    public static class ModEntryButtonPatch
    {
        // 在 ModEntry.Setup() 执行后调用
        [HarmonyPostfix]
        [HarmonyPatch("Setup")]
        private static void PostSetup(ModEntry __instance)
        {
            UI.UpDownEntry.Setup(__instance);
        }
    }
}
