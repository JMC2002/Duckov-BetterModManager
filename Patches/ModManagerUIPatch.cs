using BetterModManager.UI;
using Duckov.Modding.UI;
using HarmonyLib;
using UnityEngine.UI;
using JmcModLib.Utils;

namespace BetterModManager.Patches
{
    [HarmonyPatch(typeof(ModManagerUI))]
    public static class ModManagerUI_Patch
    {
        // 在 ModManagerUI.OnEnable 执行后添加按钮
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        private static void PostOnEnable(ModManagerUI __instance)
        {
            // 1. 初始化全选按钮
            UI.ToggleAllEntry.Setup(__instance);

            // 2. 初始化重启按钮 (新增)
            UI.RestartEntry.Setup(__instance);

            // 3. 挂载滚动适配器
            var scroll = __instance.GetComponentInChildren<ScrollRect>(true);
            ComponentHelper.AddComponentIfNeeded<ScrollRectRightDragAdapter>(scroll?.gameObject, null, $"已在 {scroll?.gameObject?.name} 挂载 ScrollRectRightDragAdapter");
        }
    }
}