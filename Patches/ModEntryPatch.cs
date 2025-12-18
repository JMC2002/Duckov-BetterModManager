using BetterModManager.UI;
using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using HarmonyLib;
using JmcModLib.Reflection;
using JmcModLib.Utils;

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
            ComponentHelper.AddComponentIfNeeded<UI.ModEntryDragHandler>(__instance.gameObject, handler => handler.Setup(__instance, master), "ModEntryDragHandler 已添加并初始化");


            ComponentHelper.AddComponentAlways<UI.ModEntryLockHandler>(
                __instance.gameObject,
                handler => handler.Setup(__instance, (locked) => {
                    ModLogger.Debug($"回调触发：锁定状态 = {locked}");
                }),
                "ModEntryLockHandler 已添加并初始化"
            );

            ComponentHelper.AddComponentAlways<UI.ModEntryPinHandler>(__instance.gameObject, initHandler => initHandler.Setup(__instance), "ModEntryPinHandler 已初始化");

            ComponentHelper.AddComponentAlways<UI.ModEntryKeyController>(__instance.gameObject, handler => handler.Setup(__instance, modInfo, index), "ModEntryKeyController 已添加并初始化");
        }

        // 拦截“上移”按钮
        [HarmonyPrefix]
        [HarmonyPatch("OnButtonReorderUpClicked")]
        private static bool PrefixOnUpClicked(ModEntry __instance)
        {
            // 获取当前的 index
            int index = MemberAccessor.Get(typeof(ModEntry), "index").GetValue<ModEntry, int>(__instance);
            ReorderHelper.Inc(index);

            // 返回 false 阻止原版方法运行
            return false;
        }

        // 拦截“下移”按钮
        [HarmonyPrefix]
        [HarmonyPatch("OnButtonReorderDownClicked")]
        private static bool PrefixOnDownClicked(ModEntry __instance)
        {
            int index = MemberAccessor.Get(typeof(ModEntry), "index").GetValue<ModEntry, int>(__instance);
            ReorderHelper.Dec(index);

            return false;
        }
    }
}
