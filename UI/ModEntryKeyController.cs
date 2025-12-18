using Duckov.Modding.UI;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Duckov.Modding;
using JmcModLib.Utils;
using BetterModManager.Utils;

namespace BetterModManager.UI
{
    public class ModEntryKeyController : MonoBehaviour, IPointerClickHandler
    {
        private CanvasGroup canvasGroup = default!; // 用于控制透明度的 CanvasGroup
        private ModEntry _entry = default!;
        private ModInfo _info; // 缓存 ModInfo

        private const float SelectedAlpha = 0.5f; // 选中时的透明度
        private const float DeselectAlpha = 1f;  // 恢复原色时的透明度

        // 全局状态
        public static string? LastSelectedId = null;
        public static bool IsGlobalListening = false;

        private int nowIdx;
        private bool waitingKeyLock = true;  // 用于防止重复触发按键

        private bool _pendingResumeSelection = false;

        private static readonly KeyCode[] upKeys = [KeyCode.W, KeyCode.UpArrow]; // 上键（默认为W和方向上）
        private static readonly KeyCode[] downKeys = [KeyCode.S, KeyCode.DownArrow]; // 下键（默认为S和方向下）
        private static readonly KeyCode[] enterKeys = [KeyCode.Return, KeyCode.Escape]; // 退出键（默认为回车和ESC）

        private static bool GetKeys(KeyCode[] keys) => keys.Any(key => Input.GetKeyDown(key));
        private static bool GetReturn() => GetKeys(enterKeys);
        private static bool GetUp() => GetKeys(upKeys);
        private static bool GetDown() => GetKeys(downKeys);

        // 当物体被激活时（SetActive(true)），检查是否有待处理任务
        private void OnEnable()
        {
            if (_pendingResumeSelection)
            {
                _pendingResumeSelection = false;
                StartCoroutine(ResumeSelectionRoutine());
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ResetColor();
            // 清除挂起的任务，防止逻辑错乱
            _pendingResumeSelection = false;
        }

        public void Setup(ModEntry modEntry, ModInfo info, int index)
        {
            _entry = modEntry;
            _info = info; // 缓存起来
            this.nowIdx = index;
          
            canvasGroup = modEntry.GetComponent<CanvasGroup>() ?? modEntry.gameObject.AddComponent<CanvasGroup>();

            // 设置初始透明度
            canvasGroup.alpha = DeselectAlpha; // 初始状态为完全可见

            // 重置标记，防止复用脏数据
            _pendingResumeSelection = false;

            // 如果全局都没在监听，直接返回，不要做任何字符串 ID 生成。
            if (!IsGlobalListening) return;

            // 只有在监听模式下，才去算自己的 ID
            string myId = _info.GetUniqueId();

            // 只有 ID 匹配，才启动协程
            if (LastSelectedId == myId)
            {
                // 检查物体是否激活
                if (gameObject.activeInHierarchy)
                {
                    // 如果已激活，直接启动
                    StartCoroutine(ResumeSelectionRoutine());
                }
                else
                {
                    // 如果未激活（Setup 在 SetActive 之前执行），标记为待处理
                    // 等待 OnEnable 自动触发
                    _pendingResumeSelection = true;
                }
            }
        }

        private IEnumerator ResumeSelectionRoutine()
        {
            // 等待一帧让 UI 布局稳定
            yield return null;

            if (gameObject.activeInHierarchy)
            {
                // 恢复视觉高亮
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = SelectedAlpha;
                    canvasGroup.blocksRaycasts = false;
                }
                // 恢复监听逻辑
                StartListening();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 点击时才生成 ID，这是低频操作，不影响性能
            string myId = _info.GetUniqueId();

            if (IsGlobalListening && LastSelectedId == myId) return;

            EnterSelection(myId);
        }

        private void EnterSelection(string id)
        {
            LastSelectedId = id;
            IsGlobalListening = true;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = SelectedAlpha;
                canvasGroup.blocksRaycasts = false;
            }

            StartListening();
            ModLogger.Info($"选中 Mod: {_info.name}");
        }

        public void StartListening()
        {
            StopAllCoroutines();
            StartCoroutine(WaitForInput());
        }

        private IEnumerator WaitForInput()
        {
            // 缓存当前 ID，避免循环里反复 Generate string
            string cachedId = _info.GetUniqueId();

            while (IsGlobalListening && LastSelectedId == cachedId)
            {
                if (GetReturn() || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    ExitSelection();
                    yield break;
                }

                if (waitingKeyLock && (GetUp() || GetDown()))
                {
                    waitingKeyLock = false;
                    var name = ReorderHelper.GetName(nowIdx);
                    ModLogger.Debug($"{name}: ");
                    if (GetUp())
                    {
                        ModLogger.Debug($"按下了 W 或 上箭头键");
                        ReorderHelper.Inc(nowIdx);
                    }
                    else
                    {
                        ModLogger.Debug($"按下了 S 或 下箭头键");
                        ReorderHelper.Dec(nowIdx);
                    }

                    // 移动后 UI 会重建，本脚本会被 Disable，循环自然终止
                }

                if (!waitingKeyLock && !GetUp() && !GetDown())
                {
                    waitingKeyLock = true;
                }

                yield return null;
            }
        }

        private void ExitSelection()
        {
            IsGlobalListening = false;
            LastSelectedId = null;
            ResetColor();
            canvasGroup?.blocksRaycasts = true;
        }

        public void ResetColor()
        {
            canvasGroup?.alpha = DeselectAlpha;
            canvasGroup?.blocksRaycasts = true;     // 防止其他条目复用时无法点击
        }
    }
}