using BetterModManager.UI;
using BetterModManager.Utils;
using Duckov.Modding;
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
        private static void PostSetup(ModEntry __instance, ModManagerUI master, ModInfo modInfo, int index)
        {
            UI.UpDownEntry.Setup(__instance);
            Utils.ComponentHelper.AddComponentIfNeeded<UI.ModEntryDragHandler>(__instance.gameObject, handler => handler.Setup(__instance), "ModEntryDragHandler 已添加并初始化");
            if (index == ModEntryKeyController.preIdx)
            {
                Utils.ComponentHelper.AddComponentAlways<UI.ModEntryKeyController>(__instance.gameObject, handler => handler.Setup(__instance, index), "ModEntryKeyController 已添加并初始化");
            }
            else
            {
                Utils.ComponentHelper.AddComponentIfNeeded<UI.ModEntryKeyController>(__instance.gameObject, handler => handler.Setup(__instance, index), "ModEntryKeyController 已添加并初始化");
            }
            
        }
    }
}
