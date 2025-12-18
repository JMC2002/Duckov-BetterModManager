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
            // 1. 如果字段里已经引用了，说明是同一个组件实例被复用，按钮还在，直接跳过
            if (_pinBtn != null)
            {
                // 必须重新绑定事件，因为 _modEntry 变了
                _pinBtn.onClick.RemoveAllListeners();
                _pinBtn.onClick.AddListener(OnBtnClicked);
                return;
            }

            // 获取定位锚点
            var anchorBtn = MemberAccessor.Get(typeof(ModEntry), "btnReorderUp").GetValue<ModEntry, Button>(_modEntry);
            if (anchorBtn == null) return;
            Transform parent = anchorBtn.transform.parent;

            // 检查物理上是否存在名为 "btnPin" 的子物体
            // 即使 _pinBtn 变量是 null，GameObject 可能已经挂在下面了（对象池复用导致的）
            Transform existing = parent.Find("btnPin");

            if (existing != null)
            {
                _pinBtn = existing.GetComponent<Button>();
            }
            else
            {
                // 只有真的找不到时，才实例化新的
                _pinBtn = Instantiate(anchorBtn, parent);
            _pinBtn.name = "btnPin";
            foreach (Transform child in _pinBtn.transform) Destroy(child.gameObject);

            // 调整位置：
            // 锁的位置是 1.2 倍宽度。置顶按钮放在锁的左边。
            // 假设锁在 anchor - 1.2w，那置顶就在 anchor - 2.4w
            RectTransform rt = _pinBtn.GetComponent<RectTransform>();
            RectTransform anchorRt = anchorBtn.GetComponent<RectTransform>();
            rt.sizeDelta = anchorRt.sizeDelta;
                // 向左偏移 2.4 倍宽度
            rt.anchoredPosition = anchorRt.anchoredPosition - new Vector2(anchorRt.rect.width * 2.4f, 0);
            }

            // 绑定事件
            _pinBtn.onClick.RemoveAllListeners();
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
            _isPinned = PinManager.IsPinned(_modInfo);

            //// 3. 立即刷新视觉
            RefreshState();
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