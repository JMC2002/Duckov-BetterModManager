using UnityEngine;
using UnityEngine.UI;
using Duckov.Modding.UI;
using Duckov.Modding;
using JmcModLib.Reflection;
using BetterModManager.Utils;
using JmcModLib.UI.Icon;

namespace BetterModManager.UI
{
    public class ModEntryPinHandler : MonoBehaviour
    {
        private ModEntry _modEntry = default!;
        private ModInfo _modInfo;
        private Button _pinBtn = default!;
        private bool _isPinned;

        public void Setup(ModEntry entry)
        {
            _modEntry = entry;
            try
            {
                _modInfo = MemberAccessor.Get(typeof(ModEntry), "info").GetValue<ModEntry, ModInfo>(_modEntry);
            }
            catch { return; }

            _isPinned = PinManager.IsPinned(_modInfo);

            CreatePinButton();
            RefreshState();
        }

        private void CreatePinButton()
        {
            // 如果字段里已经引用了，说明是同一个组件实例被复用，按钮还在，直接跳过
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
            // 写入数据 + 移动位置 + 触发列表刷新
            ReorderHelper.TogglePin(_modEntry);

            // 立即手动更新本地状态
            _isPinned = PinManager.IsPinned(_modInfo);

            // 立即刷新视觉
            RefreshState();
        }

        private void RefreshState()
        {
            if (_pinBtn == null) return;
            var img = _pinBtn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = IconGenerator.StickTop;
                // 置顶变红，未置顶变白
                img.color = _isPinned ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
            }
        }
    }
}