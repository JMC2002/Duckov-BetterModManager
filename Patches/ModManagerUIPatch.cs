using Duckov.Modding.UI;
using HarmonyLib;
using UnityEngine.UI;

namespace BetterModManager.Patches
{
    [HarmonyPatch(typeof(ModManagerUI))]
    public static class ModManagerUI_Patch
    {
        // 在 ModManagerUI.OnEnable 执行后添加全选按钮
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        private static void PostOnEnable(ModManagerUI __instance)
        {
            UI.ToggleAllEntry.Setup(__instance);
        }
    }
}
