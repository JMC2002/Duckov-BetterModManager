using UnityEngine;
using UnityEngine.UI;
using Duckov.Modding.UI;
using Duckov.Modding;
using JmcModLib.Reflection;
using BetterModManager.Utils;

namespace BetterModManager.UI
{
    public class ModEntryPinHandler : MonoBehaviour
    {
        private ModEntry _modEntry;
        private ModInfo _modInfo;
        private Button _pinBtn;
        private bool _isPinned;

        // 图标缓存
        private static Sprite _spritePinned;
        private static Sprite _spriteUnpinned;

        public void Setup(ModEntry entry)
        {
            _modEntry = entry;
            try
            {
                _modInfo = MemberAccessor.Get(typeof(ModEntry), "info").GetValue<ModEntry, ModInfo>(_modEntry);
            }
            catch { return; }

            _isPinned = PinManager.IsPinned(_modInfo);

            if (_spritePinned == null) _spritePinned = GeneratePinSprite(true);
            if (_spriteUnpinned == null) _spriteUnpinned = GeneratePinSprite(false);

            CreatePinButton();
            RefreshState();
        }

        private void CreatePinButton()
        {
            // 偷懒：克隆锁的按钮，或者克隆 ReorderUp 按钮
            // 假设你的 LockHandler 已经生成了 btnLock，我们可以去抓它，或者抓 btnReorderUp
            // 这里为了稳妥，我们抓 btnReorderUp
            var anchorBtn = MemberAccessor.Get(typeof(ModEntry), "btnReorderUp").GetValue<ModEntry, Button>(_modEntry);
            if (anchorBtn == null) return;

            _pinBtn = Instantiate(anchorBtn, anchorBtn.transform.parent);
            _pinBtn.name = "btnPin";
            _pinBtn.onClick.RemoveAllListeners();
            foreach (Transform child in _pinBtn.transform) Destroy(child.gameObject);

            // 调整位置：
            // 锁的位置是 1.2 倍宽度。置顶按钮放在锁的左边。
            // 假设锁在 anchor - 1.2w，那置顶就在 anchor - 2.4w
            RectTransform rt = _pinBtn.GetComponent<RectTransform>();
            RectTransform anchorRt = anchorBtn.GetComponent<RectTransform>();
            rt.sizeDelta = anchorRt.sizeDelta;
            rt.anchoredPosition = anchorRt.anchoredPosition - new Vector2(anchorRt.rect.width * 2.4f, 0);

            _pinBtn.onClick.AddListener(OnBtnClicked);
        }

        private void OnBtnClicked()
        {
            // 1. 执行核心逻辑 (写入数据 + 移动位置 + 触发列表刷新)
            ReorderHelper.TogglePin(_modEntry);

            // 2. 【关键修正】立即手动更新本地状态
            // 不要等待 Setup 被调用，因为：
            // A. 如果置顶后位置没变（例如本来就在第一个），列表可能不会重绘，导致图标不变。
            //// B. 即使列表重绘，对象池复用时可能有短暂的时序问题。
            //// 直接取反当前状态，或者从 PinManager 重新读取
            //_isPinned = PinManager.IsPinned(_modInfo);

            //// 或者更稳妥地：
            //// _isPinned = PinManager.IsPinned(_modInfo);

            //// 3. 立即刷新视觉
            //RefreshState();
        }

        private void RefreshState()
        {
            if (_pinBtn == null) return;
            var img = _pinBtn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = _isPinned ? _spritePinned : _spriteUnpinned;
                // 置顶变红，未置顶变白
                img.color = _isPinned ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
            }
        }

        // --- 简单的图钉图标生成 (像素风) ---
        private static Sprite GeneratePinSprite(bool active)
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h);
            tex.filterMode = FilterMode.Point;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            Color c = Color.white;

            // 画一个简单的图钉形状
            // 钉帽
            for (int x = 4; x <= 11; x++) tex.SetPixel(x, 12, c);
            for (int x = 5; x <= 10; x++) tex.SetPixel(x, 13, c);

            // 钉身
            for (int y = 6; y <= 11; y++)
            {
                tex.SetPixel(7, y, c);
                tex.SetPixel(8, y, c);
            }

            // 针尖
            for (int y = 2; y <= 5; y++) tex.SetPixel(7, y, c);
            tex.SetPixel(7, 1, c); // 尖端

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }
    }
}