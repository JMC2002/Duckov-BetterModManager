using BetterModManager.Utils;
using Duckov.Modding.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterModManager.UI
{
    /// <summary>
    /// 让 ScrollRect 支持右键拖动，而不破坏原 ScrollRect。
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectRightDragAdapter : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool allowRightButton = true;

        private ScrollRect scrollRect;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                ModLogger.Error($"{nameof(ScrollRectRightDragAdapter)}: 未找到 ScrollRect 组件。");
            }
            ModLogger.Debug("成功注册RightClickableScrollRect");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (scrollRect == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Right && allowRightButton)
            {
                ModLogger.Debug("检测到右键，开始拖动");
                // 模拟左键
                var fake = CloneEventAsLeftButton(eventData);
                scrollRect.OnBeginDrag(fake);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (scrollRect == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Right && allowRightButton)
            {
                var fake = CloneEventAsLeftButton(eventData);
                scrollRect.OnDrag(fake);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (scrollRect == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Right && allowRightButton)
            {
                ModLogger.Debug("停止右键拖动");
                var fake = CloneEventAsLeftButton(eventData);
                scrollRect.OnEndDrag(fake);
            }
        }

        private static PointerEventData CloneEventAsLeftButton(PointerEventData original)
        {
            // 复制 PointerEventData，只改按钮类型
            var copy = new PointerEventData(EventSystem.current)
            {
                position = original.position,
                delta = original.delta,
                button = PointerEventData.InputButton.Left,
                pressPosition = original.pressPosition,
                clickCount = original.clickCount,
                scrollDelta = original.scrollDelta,
                pointerDrag = original.pointerDrag,
                pointerPress = original.pointerPress,
                rawPointerPress = original.rawPointerPress
            };
            return copy;
        }
    }
}
