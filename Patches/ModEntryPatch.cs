using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using HarmonyLib;
using JmcModLib.Config;
using JmcModLib.Config.UI;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using System.Collections.Generic;
using TMPro;

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
                handler => handler.Setup(__instance, modInfo, (locked) => {
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

        [UIIntSlider(0, 2)]
        [Config("颜色方案")]
        private static int colorIdx = 0;

        [UIIntSlider(1, 200)]
        [Config("版本号字体大小 (%)")]
        private static int versionFontSize = 100;

        // Patch 目标：RefreshInfo 方法
        [HarmonyPatch("RefreshInfo")]
        [HarmonyPostfix]
        private static void Postfix(
            // Harmony 会自动注入名为 textName 的私有字段
            TextMeshProUGUI ___textName,
            // Harmony 会自动注入名为 info 的私有字段
            ModInfo ___info)
        {
            if (___textName == null) return;

            // 检查版本号是否存在且不为空
            if (!string.IsNullOrWhiteSpace(___info.version))
            {
                List<string> colorHex = new List<string>()
                {
                    "#2E5A88", // 方案 1: 深海蓝 (Steel Blue / Deep Blue) - 推荐
                    "#333333", // 方案 2: 深炭灰 (Dark Charcoal) - 如果你想要极致的清晰度
                    "#FFD700"  // 方案 3: 淡金色 (Gold) - 如果背景蓝比较深，这个会很好看；如果背景很亮，这个可能会看不清
                };

                ___textName.text += $" <size={versionFontSize}%><color={colorHex[colorIdx]}>v{___info.version}</color></size>";
            }
        }
    }
}
