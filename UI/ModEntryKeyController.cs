using BetterModManager.Utils;
using Duckov.Modding.UI;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BetterModManager.UI
{
    public class ModEntryKeyController : MonoBehaviour, IPointerClickHandler
    {
        private CanvasGroup canvasGroup; // 用于控制透明度的 CanvasGroup

        private const float SelectedAlpha = 0.5f; // 选中时的透明度
        private const float DeselectAlpha = 1f;  // 恢复原色时的透明度

        private static ModEntryKeyController? currentlySelectedController; // 记录当前选中的条目
        private static bool isListening = false; // 是否监听键盘或者鼠标事件

        private int nowIdx;
        public static int? preIdx = null;
        private static bool waitingKeyLock = true;

        private static KeyCode[] upKeys = { KeyCode.W, KeyCode.UpArrow }; // 上键（默认为W和方向上）
        private static KeyCode[] downKeys = { KeyCode.S, KeyCode.DownArrow }; // 下键（默认为S和方向下）
        private static KeyCode[] enterKeys = { KeyCode.Return, KeyCode.Escape }; // 退出键（默认为回车和ESC）

        private static bool GetKeys(KeyCode[] keys) => keys.Any(key => Input.GetKeyDown(key));
        private static bool GetReturn() => GetKeys(enterKeys);
        private static bool GetUp() => GetKeys(upKeys);
        private static bool GetDown() => GetKeys(downKeys);


        // 在对象激活时启动监听
        private void OnEnable()
        {
        }

        // 在对象禁用时停止监听
        private void OnDisable()
        {
            ModLogger.Debug($"{ReorderHelper.GetName(nowIdx)}: OnDisable");
            Reset();
        }

        public void Setup(ModEntry modEntry, int index)
        {
            ModLogger.Debug($"{ReorderHelper.GetName(nowIdx)}: 进入Setup");
            this.nowIdx = index;
            // 获取 ModEntry 上的 CanvasGroup
            canvasGroup = modEntry.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                // 如果没有 CanvasGroup，添加一个新的
                canvasGroup = modEntry.gameObject.AddComponent<CanvasGroup>();
            }

            // 设置初始透明度
            canvasGroup.alpha = DeselectAlpha; // 初始状态为完全可见

            ModLogger.Debug($"preIdx: {(preIdx != null ? preIdx : "空")}, index: {index}");
            // 衔接上次按键移动排序的状态
            if (preIdx != null && preIdx == index)
            {
                ModLogger.Debug("测试");
                try
                {
                    PointerClick();
                }
                catch (Exception ex)
                {
                    ModLogger.Error("Test出现问题", ex);
                }
            }
            else
            {
                ModLogger.Debug("走else");
            }
        }

        private void PointerClick()
        {
            // 设置当前点击条目的透明度
            if (canvasGroup != null)
            {
                ModLogger.Debug("设置alpha");
                canvasGroup.alpha = SelectedAlpha;
                Canvas.ForceUpdateCanvases();
            }
            else
            {
                ModLogger.Warn("当前canvasGroup为空!");
            }

                // 记录当前选中的条目
                currentlySelectedController = this;

            // 只有在当前对象是激活状态下才开始监听
            if (gameObject.activeInHierarchy)
            {
                ModLogger.Debug("当前激活");
                StartListening();
            }
            else
            {
                ModLogger.Debug("当前未激活");
                // 如果当前GameObject非激活状态，延迟启动
                StartCoroutine(WaitForActivationAndStartListening());
            }
        }

        private IEnumerator WaitForActivationAndStartListening()
        {
            // 在激活状态改变时进行循环检查，确保对象激活
            while (!gameObject.activeInHierarchy)
            {
                yield return null;  // 等待下一帧继续检查
            }

            // 激活后再启动监听
            StartListening();
        }

        // 点击时，改变透明度并记录选中的条目
        public void OnPointerClick(PointerEventData eventData)
        {
            ModLogger.Debug("触发OnPointerClick");
            ModLogger.Debug($"当前idx为，{nowIdx}");
            if (currentlySelectedController != null && currentlySelectedController == this)
            {
                ModLogger.Debug("重复点击，取消选中");

                // 置空，防止下一次点击不生效
                currentlySelectedController = null;
                return;
            }

            PointerClick();

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
            if (isListening)
            {
                ModLogger.Debug("已经在监听，跳过启动");
                return;
            }

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
            StopAllCoroutines();
        }

        // 监听鼠标点击事件
        private IEnumerator WaitForReturn()
        {
            // 等待直到鼠标按下事件触发
            yield return new WaitUntil(() => isListening && (Input.GetMouseButtonDown(0) || GetReturn()));

            if (canvasGroup != null)
            {
                ModLogger.Debug($"当前canvasGroup.alpha: {canvasGroup.alpha}");
            }

            if (Input.GetMouseButtonDown(0))
            {
                ModLogger.Debug("检测到鼠标点击，取消选中");
            }
            else
            {
                ModLogger.Debug("检测到按下回车或ESC，取消选中");
            }

            Reset();
            preIdx = null;
            ModLogger.Debug("处理点击事件完毕");
        }

        // 键盘事件监听
        private IEnumerator WaitForKeyPress()
        {
            yield return new WaitUntil(() => isListening && waitingKeyLock && (GetUp() || GetDown()));
            ModLogger.Debug("WaitForKeyPress被触发");
            waitingKeyLock = false;
            var name = ReorderHelper.GetName(nowIdx);
            ModLogger.Debug($"{name}: ");
            // 检测 W 或 上键
            if (GetUp())
            {
                preIdx = ReorderHelper.Clamp(nowIdx - 1);
                
                ModLogger.Debug($"按下了 W 或 上箭头键，目标为{preIdx}");
                ReorderHelper.Inc(nowIdx);
                ModLogger.Debug("继续");
            }
            else
            {
                preIdx = ReorderHelper.Clamp(nowIdx + 1);
                ModLogger.Debug($"按下了 S 或 下箭头键，目标为{preIdx}");
                ReorderHelper.Dec(nowIdx);
                ModLogger.Debug("继续");
            }

            waitingKeyLock = true;
        }

        private void Reset()
        {
            ResetColor();
            currentlySelectedController = null;
            if (isListening)
                StopListening();
        }
    }
}
