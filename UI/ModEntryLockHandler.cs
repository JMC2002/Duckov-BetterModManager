using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BetterModManager.UI
{
    /// <summary>
    /// 挂载在 ModEntry 物体上的锁控制器
    /// </summary>
    public class ModEntryLockHandler : MonoBehaviour
    {
        public bool IsLocked { get; private set; } = false;

        // 缓存两张图标：一张锁住（闭合），一张解锁（打开）
        private static Sprite? _spriteLocked;
        private static Sprite? _spriteUnlocked;

        private ModEntry? _modEntry;
        private ModInfo _modInfo;
        private Button? _lockBtn;
        private Action<bool>? _onLockStateChanged;

        public void Setup(ModEntry entry, Action<bool>? onLockChanged = null)
        {
            _modEntry = entry;
            _onLockStateChanged = onLockChanged;
            
            try
            {
                _modInfo = MemberAccessor.Get(typeof(ModEntry), "info").GetValue<ModEntry, ModInfo>(_modEntry);
            }
            catch { ModLogger.Error("无法获取 ModInfo"); }
            IsLocked = LockManager.IsLocked(_modInfo);

            // 预先生成两张图片
            if (_spriteLocked == null) _spriteLocked = GenerateLockSprite(true);
            if (_spriteUnlocked == null) _spriteUnlocked = GenerateLockSprite(false);

            CreateLockButton();
            RefreshState();
        }

        private void CreateLockButton()
        {
            if (_modEntry == null) return;

            var anchorBtn = MemberAccessor.Get(typeof(ModEntry), "btnReorderUp")
                                          .GetValue<ModEntry, Button>(_modEntry);

            if (anchorBtn == null)
            {
                ModLogger.Error("[LockHandler] 找不到 btnReorderUp");
                return;
            }

            _lockBtn = Instantiate(anchorBtn, anchorBtn.transform.parent);
            _lockBtn.name = "btnLock";

            _lockBtn.onClick.RemoveAllListeners();
            foreach (Transform child in _lockBtn.transform) Destroy(child.gameObject);

            // 调整位置：向左偏移
            RectTransform rt = _lockBtn.GetComponent<RectTransform>();
            RectTransform anchorRt = anchorBtn.GetComponent<RectTransform>();
            rt.sizeDelta = anchorRt.sizeDelta;

            // 使用 anchoredPosition 进行偏移，这里设为 1.2 倍宽度
            rt.anchoredPosition = anchorRt.anchoredPosition - new Vector2(anchorRt.rect.width * 1.2f, 0);

            _lockBtn.onClick.AddListener(OnBtnClicked);
        }

        private void OnBtnClicked()
        {
            IsLocked = !IsLocked;

            // 2. 更新状态到 LockManager (会自动触发 SetValue 保存)
            LockManager.SetLocked(_modInfo, IsLocked);

            RefreshState();
            _onLockStateChanged?.Invoke(IsLocked);

            // string name = GetModName();
            ModLogger.Info(IsLocked ? $"[Lock] {name} 已锁定" : $"[Lock] {name} 已解锁");
        }

        /// <summary>
        /// 刷新按钮的视觉状态（图标 + 颜色）
        /// </summary>
        private void RefreshState()
        {
            if (_lockBtn == null) return;

            var img = _lockBtn.GetComponent<Image>();
            if (img != null)
            {
                // 1. 切换图片：锁定用闭合图，解锁用开口图
                img.sprite = IsLocked ? _spriteLocked : _spriteUnlocked;

                // 2. 切换颜色：锁定变红，解锁变白
                img.color = IsLocked ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
            }
        }

        private string GetModName()
        {
            try
            {
                if (_modEntry == null) return "Unknown";
                var info = MemberAccessor.Get(typeof(ModEntry), "info").GetValue<ModEntry, ModInfo>(_modEntry);
                return info.name ?? "Unknown";
            }
            catch { return "Unknown"; }
        }

        /// <summary>
        /// 动态生成像素风锁图标
        /// </summary>
        /// <param name="isClosed">true=锁梁落下(闭合), false=锁梁抬起(打开)</param>
        private static Sprite GenerateLockSprite(bool isClosed)
        {
            int w = 16;
            int h = 16;
            Texture2D tex = new Texture2D(w, h);
            tex.filterMode = FilterMode.Point;

            // 初始化全透明
            Color[] colors = new Color[w * h];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;
            tex.SetPixels(colors);
            Color c = Color.white;

            // --- 1. 绘制锁身 (底部矩形) ---
            // 高度调整为 0~7 (8像素高)，给上面留出更多空间做动画
            for (int x = 3; x <= 12; x++)
            {
                for (int y = 0; y <= 7; y++)
                {
                    tex.SetPixel(x, y, c);
                }
            }

            // --- 2. 绘制锁梁 (U型) ---
            // 如果是打开状态，整个锁梁向上偏移 3 像素
            int yOffset = isClosed ? 0 : 3;

            // 锁梁的基础高度 (闭合时从 y=8 开始)
            int yStart = 8 + yOffset;
            int yEnd = 12 + yOffset;

            // 防止绘制超出图片边界 (16像素, max index 15)
            if (yEnd > 15) yEnd = 15;

            // 绘制左右两根柱子
            for (int y = yStart; y <= yEnd; y++)
            {
                // 左柱
                tex.SetPixel(4, y, c);
                tex.SetPixel(5, y, c);

                // 右柱
                tex.SetPixel(10, y, c);
                tex.SetPixel(11, y, c);
            }

            // 绘制顶部拱形 (连接处)
            int yTop = yEnd + 1;
            if (yTop <= 15)
            {
                for (int x = 4; x <= 11; x++)
                {
                    tex.SetPixel(x, yTop, c);
                    // 加粗拱形内部连接点
                    if (yTop - 1 >= 0) tex.SetPixel(x, yTop - 1, c);
                }
            }

            // --- 3. 绘制钥匙孔 (透明色挖空) ---
            // 保持在锁身中心
            tex.SetPixel(7, 4, Color.clear); tex.SetPixel(8, 4, Color.clear);
            tex.SetPixel(7, 5, Color.clear); tex.SetPixel(8, 5, Color.clear);
            tex.SetPixel(7, 3, Color.clear); tex.SetPixel(8, 3, Color.clear);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }
    }
}