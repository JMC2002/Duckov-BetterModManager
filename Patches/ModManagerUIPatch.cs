using BetterModManager.UI;
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

            var scroll = __instance.GetComponentInChildren<ScrollRect>(true);
            Utils.ComponentHelper.AddComponentIfNeeded<ScrollRectRightDragAdapter>(scroll?.gameObject, null, $"已在 {scroll?.gameObject?.name} 挂载 ScrollRectRightDragAdapter");

        }
    }
}
