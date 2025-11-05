using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BetterModManager.Patches
{
    [HarmonyPatch(typeof(ModEntry))]
    public static class ModEntryButtonPatch
    {
        // 缓存缩放后的按钮模板
        private static float? _scaleHeight = null;
        private static Vector3? posUp   = null;
        private static Vector3? posDown = null;

        // 在 ModEntry.Setup() 执行后调用（Setup是生成UI时调用的）
        [HarmonyPostfix]
        [HarmonyPatch("Setup")]
        private static void PostSetup(ModEntry __instance)
        {
            try
            {
                // 获取原始的上移/下移按钮
                var btnUp = ReflectionHelper.GetFieldValue<Button>(__instance, "btnReorderUp");
                var btnDown = ReflectionHelper.GetFieldValue<Button>(__instance, "btnReorderDown");

                //var toggleButton = ReflectionHelper.GetFieldValue<Button>(__instance, "toggleButton");
                //RectTransform rectTransform = toggleButton.gameObject.GetComponent<RectTransform>();
                //ModLogger.Debug($"local: {rectTransform.localPosition}");
                //ModLogger.Debug($"pos: {rectTransform.position}");
                //ModLogger.Debug($"rect: {rectTransform.rect.width}");
                //ModLogger.Debug($"left: {Vector3.left}");
                if (btnUp == null || btnDown == null)
                {
                    ModLogger.Error("未能找到原始的上/下按钮，无法添加置顶/置底按钮。");
                    return;
                }

                // ===== 调整按钮高度，使其缩小一半 =====
                ResizeButtonHeight(btnUp, 0.5f, Vector3.down, ref posUp); // 缩小上移按钮
                ResizeButtonHeight(btnDown, 0.5f, Vector3.up, ref posDown); // 缩小下移按钮

                // ===== 创建置顶按钮 =====
                var btnTop = UnityEngine.Object.Instantiate(btnUp, btnUp.transform.parent);
                btnTop.name = "btnReorderTop";
                AdjustButtonPosition(btnTop.transform, btnUp.transform, Vector3.up * btnUp.GetComponent<RectTransform>().rect.height * 1.1f); // 向上偏移按钮
                ReplaceButtonEvent(btnTop, () =>
                {
                    ModLogger.Info("置顶按钮被点击");

                    // 使用反射获取当前 ModEntry 的 index 和 info 字段值
                    int currentIndex = ReflectionHelper.GetFieldValue<int>(__instance, "index");
                    ModInfo currentModInfo = ReflectionHelper.GetFieldValue<ModInfo>(__instance, "info");

                    // 调用 Reorder 方法将当前 Mod 移动到最前面
                    if (ModManager.Reorder(currentIndex, 0))  // 置顶
                    {
                        ModLogger.Info($"Mod '{currentModInfo.name}' 已被置顶");
                    }
                    else
                    {
                        ModLogger.Error("置顶操作失败");
                    }
                });

                // ===== 创建置底按钮 =====
                var btnBottom = UnityEngine.Object.Instantiate(btnDown, btnDown.transform.parent);
                btnBottom.name = "btnReorderBottom";
                AdjustButtonPosition(btnBottom.transform, btnDown.transform, Vector3.down * btnDown.GetComponent<RectTransform>().rect.height * 1.1f); // 向下偏移按钮
                ReplaceButtonEvent(btnBottom, () =>
                {
                    ModLogger.Info("置底按钮被点击");

                    // 使用反射获取当前 ModEntry 的 index 和 info 字段值
                    int currentIndex = BetterModManager.Utils.ReflectionHelper.GetFieldValue<int>(__instance, "index");
                    ModInfo currentModInfo = BetterModManager.Utils.ReflectionHelper.GetFieldValue<ModInfo>(__instance, "info");

                    // 调用 Reorder 方法将当前 Mod 移动到底部
                    if (ModManager.Reorder(currentIndex, ModManager.modInfos.Count - 1))  // 置底
                    {
                        ModLogger.Info($"Mod '{currentModInfo.name}' 已被置底");
                    }
                    else
                    {
                        ModLogger.Error("置底操作失败");
                    }
                });
                // ModLogger.Debug("已成功在 ModEntry 中添加置顶与置底按钮。");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"创建置顶/置底按钮时出错: {ex}");
            }
        }

        /// <summary>
        /// 调整按钮的高度
        /// </summary>
        private static void ResizeButtonHeight(Button button, float scale, Vector3 dir, ref Vector3? pos)
        {
            var rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                ModLogger.Error($"未能找到按钮的 RectTransform，无法调整按钮高度");
                return;
            }

            // 获取原始高度并计算缩放后的高度
            _scaleHeight = _scaleHeight ?? rectTransform.rect.height * scale;

            // 设置新高度
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)_scaleHeight);

            var offset = dir * (1 / scale - 1) * (float)_scaleHeight;
            pos = pos ?? button.transform.localPosition;
            button.transform.localPosition = (Vector3)(pos + offset);
        }

        /// <summary>
        /// 调整新按钮的位置
        /// </summary>
        private static void AdjustButtonPosition(Transform button, Transform reference, Vector3 offset)
        {
            button.SetSiblingIndex(reference.GetSiblingIndex());
            button.localPosition = reference.localPosition + offset;
        }

        /// <summary>
        /// 替换按钮点击事件
        /// </summary>
        private static void ReplaceButtonEvent(Button button, Action onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
