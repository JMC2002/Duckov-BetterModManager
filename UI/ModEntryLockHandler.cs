using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using JmcModLib.Reflection;
using JmcModLib.UI.Icon;
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

        private ModEntry? _modEntry;
        private ModInfo _modInfo;
        private Button? _lockBtn;
        private Action<bool>? _onLockStateChanged;

        public void Setup(ModEntry entry, ModInfo info, Action<bool>? onLockChanged = null)
        {
            _modEntry = entry;
            _onLockStateChanged = onLockChanged;
            _modInfo = info;
            
            IsLocked = LockManager.IsLocked(_modInfo);

            CreateOrFindLockButton();
            RefreshState();
        }

        private void CreateOrFindLockButton()
        {
            if (_lockBtn != null)
            {
                _lockBtn.onClick.RemoveAllListeners();
                _lockBtn.onClick.AddListener(OnBtnClicked);
                return;
            }

            var anchorBtn = MemberAccessor.Get(typeof(ModEntry), "btnReorderUp").GetValue<ModEntry, Button>(_modEntry);
            if (anchorBtn == null) return;
            Transform parent = anchorBtn.transform.parent;

            Transform existing = parent.Find("btnLock");

            if (existing != null)
            {
                _lockBtn = existing.GetComponent<Button>();
            }
            else
            {
                _lockBtn = Instantiate(anchorBtn, parent);
                _lockBtn.name = "btnLock"; // 名字必须固定，方便查找
                foreach (Transform child in _lockBtn.transform) Destroy(child.gameObject);

                RectTransform rt = _lockBtn.GetComponent<RectTransform>();
                RectTransform anchorRt = anchorBtn.GetComponent<RectTransform>();
                rt.sizeDelta = anchorRt.sizeDelta;
                // 向左偏移 1.2 倍
                rt.anchoredPosition = anchorRt.anchoredPosition - new Vector2(anchorRt.rect.width * 1.2f, 0);
            }

            _lockBtn.onClick.RemoveAllListeners();
            _lockBtn.onClick.AddListener(OnBtnClicked);
        }

        private void OnBtnClicked()
        {
            IsLocked = !IsLocked;

            // 更新状态到 LockManager (会自动触发 SetValue 保存)
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
                // 切换图片：锁定用闭合图，解锁用开口图
                img.sprite = IsLocked ? IconGenerator.LockClosed : IconGenerator.LockOpen;

                // 2. 切换颜色：锁定变红，解锁变白
                img.color = IsLocked ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
            }
        }
    }
}