using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BetterModManager.UI
{
    public static class UpDownEntry
    {
        // 原本按钮的默认缩放比例
        private static readonly float scale = 0.5f;
        // 缓存缩放高度
        private static float? _scaleHeight = null;
        private static Vector3? posUp = null;
        private static Vector3? posDown = null;

        public static void Setup(ModEntry __instance)
        {
            try
            {
                // ===== 创建置顶按钮 =====
                AddButton(__instance, "btnReorderUp", Vector3.down, ref posUp, GetOnClick(__instance, "置顶", 0));
                
                // ===== 创建置底按钮 =====
                AddButton(__instance, "btnReorderDown", Vector3.up, ref posDown, GetOnClick(__instance, "置底", ModManager.modInfos.Count - 1));
                
                // ModLogger.Debug("已成功在 ModEntry 中添加置顶与置底按钮。");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"创建置顶/置底按钮时出错", ex);
            }
        }

        /// <summary>
        /// 根据原按钮添加置顶/置底按钮
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="buttonName">参考按钮</param>
        /// <param name="dir">参考按钮的移动方向</param>
        /// <param name="posDir">缓存的偏移向量</param>
        private static void AddButton(ModEntry __instance, string buttonName, Vector3 dir, ref Vector3? posDir, Action action)
        {
            Button btnSrc;
            // 若不存在buttonName，此处会直接抛异常，因此不用单独判断
            btnSrc = ReflectionHelper.GetFieldValue<Button>(__instance, buttonName);
            ResizeButtonHeightAndMove(btnSrc, dir, ref posDir);

            // ===== 创建置顶按钮 =====
            var btnTop = UnityEngine.Object.Instantiate(btnSrc, btnSrc.transform.parent);
            btnTop.name = $"btnFrom_{buttonName}";
            AdjustButtonPosition(btnTop.transform, btnSrc.transform, -dir * btnSrc.GetComponent<RectTransform>().rect.height * 1.1f); // 向上偏移按钮
            ReplaceButtonEvent(btnTop, action);
        }

        /// <summary>
        /// 调整按钮的高度并调整按钮位置
        /// </summary>
        private static void ResizeButtonHeightAndMove(Button button, Vector3 dir, ref Vector3? pos)
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

        private static Action GetOnClick(ModEntry __instance, string dirName, int dstIdx)
        {
            return () =>
            {
                ModLogger.Info($"{dirName}按钮被点击");

                // 使用反射获取当前 ModEntry 的 index 和 info 字段值
                int currentIndex = ReflectionHelper.GetFieldValue<int>(__instance, "index");
                ModInfo currentModInfo = ReflectionHelper.GetFieldValue<ModInfo>(__instance, "info");

                // 调用 Reorder 方法移动当前 Mod
                if (ModManager.Reorder(currentIndex, dstIdx))
                {
                    ModLogger.Info($"Mod '{currentModInfo.name}' 已被{dirName}");
                }
                else
                {
                    ModLogger.Error("置底操作失败");
                }
            };
        }
    }
}
