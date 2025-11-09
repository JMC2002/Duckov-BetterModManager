using BetterModManager.Utils;
using Duckov.Modding.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterModManager.UI
{
    public class ModEntryKeyController : MonoBehaviour, IPointerClickHandler
    {
        private ModEntry modEntry;
        private CanvasGroup canvasGroup; // 用于控制透明度的 CanvasGroup

        private Color initialColor; // 记录初始颜色
        private const float SelectedAlpha = 0.5f; // 选中时的透明度
        private const float DeselectAlpha = 1f;  // 恢复原色时的透明度

        private static ModEntryKeyController? currentlySelectedController; // 记录当前选中的条目
        private static bool isListening = false; // 是否监听键盘或者鼠标事件

        private KeyCode upKey = KeyCode.W; // 上键（默认为W）
        private KeyCode downKey = KeyCode.S; // 下键（默认为S）
        private KeyCode enterKey = KeyCode.Return; // 回车键（默认为回车）


        public void Setup(ModEntry modEntry)
        {
            this.modEntry = modEntry;

            // 获取 ModEntry 上的 CanvasGroup
            canvasGroup = modEntry.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                // 如果没有 CanvasGroup，添加一个新的
                canvasGroup = modEntry.gameObject.AddComponent<CanvasGroup>();
            }

            // 设置初始透明度
            canvasGroup.alpha = DeselectAlpha; // 初始状态为完全可见
        }

        // 点击时，改变透明度并记录选中的条目
        public void OnPointerClick(PointerEventData eventData)
        {
            ModLogger.Debug("触发OnPointerClick");
            if (currentlySelectedController != null && currentlySelectedController == this)
            {
                ModLogger.Debug("重复点击，取消选中");

                // 置空，防止下一次点击不生效
                currentlySelectedController = null;
                return;
            }

            //// 如果之前有选中的条目，恢复其颜色
            //if (currentlySelectedController != null && currentlySelectedController == this)
            //{
            //    currentlySelectedController.ResetColor();
            //}

            //// 如果之前有选中的条目，恢复其颜色
            //if (currentlySelectedController != null && currentlySelectedController != this)
            //{
            //    currentlySelectedController.ResetColor();
            //}

            // 设置当前点击条目的透明度
            if (canvasGroup != null)
            {
                canvasGroup.alpha = SelectedAlpha;
            }

            // 记录当前选中的条目
            currentlySelectedController = this;

            StartListening();

            ModLogger.Info("ModEntry 被点击，蒙版效果激活");

        }

        // 恢复颜色
        public void ResetColor()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = DeselectAlpha;
            }
        }

        public void StartListening()
        {
            ModLogger.Debug("开始监听输入事件");
            isListening = true;

            // 启动监听键盘按键事件
            StartCoroutine(WaitForReturn());
            StartCoroutine(WaitForKeyPress());
        }

        // 停止监听
        public void StopListening()
        {
            ModLogger.Debug("停止监听输入事件");
            isListening = false;
        }

        // 监听鼠标点击事件
        private IEnumerator WaitForReturn()
        {
            // 等待直到鼠标按下事件触发
            yield return new WaitUntil(() => isListening && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(enterKey)));

            if (Input.GetMouseButtonDown(0))
            {
                ModLogger.Debug("检测到鼠标点击，取消选中");
            }

            if (Input.GetKeyDown(enterKey))
            {
                ModLogger.Debug("检测到按下回车，取消选中");

                // 相比于鼠标退出，这里多了置空的操作，以便下一次点击不会触发重复点击
                currentlySelectedController = null;
            }

            Reset();
        }

        // 键盘事件监听
        private IEnumerator WaitForKeyPress()
        {
            // 持续监听按键事件
            while (isListening)
            {
                // 检测 W 或 上键
                if (Input.GetKeyDown(upKey))
                {
                    ModLogger.Debug("按下了 W 或 上箭头键");
                    ReorderHelper.Inc(ReorderHelper.GetIndex(modEntry));
                }

                // 检测 S 或 下键
                if (Input.GetKeyDown(downKey))
                {
                    ModLogger.Debug("按下了 S 或 下箭头键");
                    ReorderHelper.Dec(ReorderHelper.GetIndex(modEntry));
                }

                yield return null; // 等待下一帧，持续监听按键
            }
        }

        private void Reset()
        {
            ResetColor();
            // currentlySelectedController = null;
            StopListening();
        }

        // 可以设置按键的替代方法
        public void SetKeys(KeyCode up, KeyCode down, KeyCode enter)
        {
            upKey = up;
            downKey = down;
            enterKey = enter;
        }
    }
}
