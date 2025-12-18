using BetterModManager.Utils;
using Duckov.Modding.UI;
using JmcModLib.Reflection;
using JmcModLib.Utils;
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

        // 缓存一个纯白纹理，避免重复创建
        private static Texture2D? _whiteTexture;

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
                // 注意：这里传入了 dir 枚举，以便后续判断是置顶还是置底
                AddButtonImpl(__instance, info.srcTag, info.Tag, info.dir, ref dict[dir].pos
                                    , GetOnClick(__instance, info.hintWords, dir == Dir.Up), dir);
            }
            else
            {
                ModLogger.Error($"{dir}未注册！");
            }
        }

        /// <summary>
        /// 根据原按钮添加置顶/置底按钮
        /// </summary>
        private static void AddButtonImpl(ModEntry __instance, string srcTag, string dstTag, Vector3 dir, ref Vector3? posDir, Action action, Dir directionType)
        {
            Button btnSrc;
            // 若不存在buttonName，此处会直接抛异常，因此不用单独判断
            btnSrc = MemberAccessor.Get(typeof(ModEntry), srcTag)
                                   .GetValue<ModEntry, Button>(__instance);

            ResizeButtonHeightAndMove(btnSrc, -dir, ref posDir);

            var btnNew = UnityEngine.Object.Instantiate(btnSrc, btnSrc.transform.parent);
            btnNew.name = $"{srcTag}";

            AdjustButtonPosition(btnNew.transform, btnSrc.transform, dir * btnSrc.GetComponent<RectTransform>().rect.height * 1.1f); // 向上偏移按钮
            ReplaceButtonEvent(btnNew, action);

            // === 新增：添加横线装饰 ===
            AddLineDecoration(btnNew, directionType);
        }

        /// <summary>
        /// 动态绘制一根横线来装饰按钮
        /// </summary>
        /// <summary>
        /// 动态绘制一根横线来装饰按钮（终极修正版）
        /// </summary>
        private static void AddLineDecoration(Button btn, Dir type)
        {
            // 1. 创建并设置父级
            GameObject lineObj = new("LimitLine");
            lineObj.transform.SetParent(btn.transform, false);
            lineObj.layer = btn.gameObject.layer;

            // 2. 强制重置变换
            lineObj.transform.localScale = Vector3.one;
            lineObj.transform.localRotation = Quaternion.identity;
            lineObj.transform.localPosition = Vector3.zero;

            // 3. 添加组件
            Image lineImg = lineObj.AddComponent<Image>();

            // 4. 设置纯白纹理
            if (_whiteTexture == null)
            {
                _whiteTexture = new Texture2D(1, 1);
                _whiteTexture.SetPixel(0, 0, Color.white);
                _whiteTexture.Apply();
            }
            lineImg.sprite = Sprite.Create(_whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

            Color targetColor = new(0.6f, 0.6f, 0.6f, 1.0f);

            var parentImg = btn.GetComponent<Image>();
            if (parentImg != null)
            {
                bool isColor = Mathf.Abs(parentImg.color.r - parentImg.color.b) > 0.1f;
                if (isColor)
                {
                    targetColor = parentImg.color;
                    targetColor.a = 1.0f; // 保持不透明
                }
            }
            lineImg.color = targetColor;

            // 6. 设置大小和位置
            RectTransform rt = lineObj.GetComponent<RectTransform>();
            RectTransform parentRt = btn.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float width = parentRt.rect.width * 0.5f;
            float height = 3f; // 高度保持 3 像素
            rt.sizeDelta = new Vector2(width, height);

            float offset = parentRt.rect.height * 0.32f;

            float yPos = (type == Dir.Up) ? offset : -offset;

            rt.anchoredPosition = new Vector2(0, yPos);
        }

        /// <summary>
        /// 打印调试信息，分析为什么看不见
        /// </summary>
        private static void DebugPrint(GameObject lineObj, Button parentBtn)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== [UI Debug Info] ==========");

            // 1. 检查父物体（按钮）状态
            sb.AppendLine($"[Parent Button] Name: {parentBtn.name}, Layer: {parentBtn.gameObject.layer}, Active: {parentBtn.gameObject.activeInHierarchy}");
            var pRect = parentBtn.GetComponent<RectTransform>();
            sb.AppendLine($"[Parent Rect] Size: {pRect.rect.size}, Pos: {pRect.anchoredPosition}, Scale: {pRect.localScale}");

            // 2. 检查子物体（横线）状态
            sb.AppendLine($"[Line Object] Name: {lineObj.name}, Layer: {lineObj.gameObject.layer}, Active: {lineObj.gameObject.activeInHierarchy}");

            // 3. 检查 RectTransform
            var lRect = lineObj.GetComponent<RectTransform>();
            sb.AppendLine($"[Line Rect] Size: {lRect.rect.size} (Width x Height)");
            sb.AppendLine($"[Line Rect] LocalPos: {lRect.localPosition}, AnchoredPos: {lRect.anchoredPosition}");
            sb.AppendLine($"[Line Rect] Scale: {lRect.localScale} (Expect 1,1,1)");
            sb.AppendLine($"[Line Rect] Pivot: {lRect.pivot}, AnchorMin/Max: {lRect.anchorMin}/{lRect.anchorMax}");

            // 4. 检查 Image 组件
            var img = lineObj.GetComponent<Image>();
            if (img == null)
            {
                sb.AppendLine("[Line Image] IS MISSING!");
            }
            else
            {
                sb.AppendLine($"[Line Image] Enabled: {img.enabled}");
                sb.AppendLine($"[Line Image] Color: {img.color} (Alpha: {img.color.a})");
                sb.AppendLine($"[Line Image] Sprite: {(img.sprite == null ? "NULL" : img.sprite.name)}");
                sb.AppendLine($"[Line Image] Material: {(img.material == null ? "NULL" : img.material.name)}");
            }

            // 5. 检查层级关系 (Hierarchy)
            sb.AppendLine($"[Hierarchy Check] Line Parent is: {lineObj.transform.parent?.name}");
            if (lineObj.transform.parent != parentBtn.transform)
            {
                sb.AppendLine("!!! WARNING: Parent is NOT the button! !!!");
            }

            // 6. 检查是否被 Mask 遮挡
            var masks = parentBtn.GetComponentsInParent<Mask>(true);
            var rectMasks = parentBtn.GetComponentsInParent<RectMask2D>(true);
            sb.AppendLine($"[Mask Check] Found {masks.Length} Masks, {rectMasks.Length} RectMask2Ds in parent chain.");

            sb.AppendLine("=====================================");
            ModLogger.Info(sb.ToString());
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
            _scaleHeight ??= (rectTransform.rect.height * scale);

            // 设置新高度
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)_scaleHeight);

            var offset = dir * ((1 / scale) - 1) * (float)_scaleHeight;
            pos ??= button.transform.localPosition;
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