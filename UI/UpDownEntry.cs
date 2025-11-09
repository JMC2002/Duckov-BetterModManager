using BetterModManager.Utils;
using Duckov.Modding.UI;
using System;
using System.Collections.Generic;
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
        public enum Dir
        {
            Up,
            Down,
        }

        public class BtnInfo
        {
            public string Tag = "default";
            public string srcTag = "default";
            public string hintWords = "default";
            public Vector3 dir;      // 按钮相对于参考按钮的方向
            public Vector3? pos;     // 按钮原始的偏移
        }

        public static Dictionary<Dir, BtnInfo> dict = new()
        {
            { Dir.Up, new BtnInfo { Tag = "btnReorderTop", srcTag = "btnReorderUp", hintWords = "置顶", dir = Vector3.up, pos = null } },
            { Dir.Down, new BtnInfo { Tag = "btnReorderBottom", srcTag = "btnReorderDown", hintWords = "置底", dir = Vector3.down, pos = null } }
        };
        public static void Setup(ModEntry __instance)
        {
            try
            {
                foreach (Dir direction in Enum.GetValues(typeof(Dir)))
                {
                    AddButton(__instance, direction);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"创建置顶/置底按钮时出错", ex);
            }
        }

        private static void AddButton(ModEntry __instance, Dir dir)
        {
            if (dict.TryGetValue(dir, out var info))
            {
                AddButtonImpl(__instance, info.srcTag, info.Tag, info.dir, ref dict[dir].pos
                                    , GetOnClick(__instance, info.hintWords, dir == Dir.Up));
            }
            else
            {
                ModLogger.Error($"{dir}未注册！");
            }

        }
        /// <summary>
        /// 根据原按钮添加置顶/置底按钮
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="srcTag">参考按钮</param>
        /// <param name="dir">参考按钮的移动方向</param>
        /// <param name="posDir">缓存的偏移向量</param>
        private static void AddButtonImpl(ModEntry __instance, string srcTag, string dstTag, Vector3 dir, ref Vector3? posDir, Action action)
        {
            Button btnSrc;
            // 若不存在buttonName，此处会直接抛异常，因此不用单独判断
            btnSrc = ReflectionHelper.GetFieldValue<Button>(__instance, srcTag);
            ResizeButtonHeightAndMove(btnSrc, -dir, ref posDir);

            var btnTop = UnityEngine.Object.Instantiate(btnSrc, btnSrc.transform.parent);
            btnTop.name = $"{srcTag}";
            AdjustButtonPosition(btnTop.transform, btnSrc.transform, dir * btnSrc.GetComponent<RectTransform>().rect.height * 1.1f); // 向上偏移按钮
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

        private static Action GetOnClick(ModEntry __instance, string dirName, bool IsToUp)
        {
            return () =>
            {
                ModLogger.Info($"{dirName}按钮被点击");
                ReorderHelper.ToTopOrBottom(__instance, IsToUp);
            };
        }
    }
}
