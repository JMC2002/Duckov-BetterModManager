using BetterModManager.Utils;
using Duckov.Modding.UI;
using Duckov.Utilities;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterModManager.UI
{
    public class ModEntryDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private CanvasGroup? canvasGroup;
        private Vector2 initialPosition;
        private Transform? initialParent;

        private float startY;
        private int initialSiblingIndex;

        private ModEntry modEntry;
        private RightClickableScrollRect? rightRect;
        private ModManagerUI master;
        private bool dragStarted = false;
        private float dragStartTime;
        private Vector2 dragStartPos;

        private const float DragingAlpha = 0.6f;
        private const float InitialAlpha = 1f; 
        private const float DragDelay = 0.25f;     // 鼠标按住多久才算拖拽（毫秒）
        private const float MoveThreshold = 6f;    // 移动多远才开始拖拽（像素）

        private float? deltaHeight;

        public void Setup(ModEntry modEntry, ModManagerUI master)
        {
            this.modEntry = modEntry;
            this.master = master;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            SetCanvasGroup(true);
        }

        private bool IsPermitted(PointerEventData eventData)
        {
            return eventData.button == PointerEventData.InputButton.Left;
        }

        // 拖拽开始时触发
        public void OnBeginDrag(PointerEventData eventData)
        {
            rightRect = master.GetComponentInChildren<ScrollRect>(true)?.GetComponent<RightClickableScrollRect>();
            if (rightRect == null)
            {
                ModLogger.Warn("未找到RightClickableScrollRect");
            }

            if (rightRect != null && !IsPermitted(eventData))
            {
                ModLogger.Debug("开始转发右键拖动事件");
                // 允许右键事件穿透到 ScrollRect
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                rightRect.OnBeginDrag(eventData);
                return;
            }

            // 延迟初始化
            deltaHeight = deltaHeight ?? GetDeltaHeight(modEntry);

            startY = GetLocalY(eventData);

            dragStarted = false;
            dragStartTime = Time.time;
            dragStartPos = eventData.position;

            ModLogger.Debug("OnBeginDrag()");
        }

        // 在拖拽过程中触发
        public void OnDrag(PointerEventData eventData)
        {
            if (rightRect != null && !IsPermitted(eventData))
            {
                rightRect.OnDrag(eventData);
                ModLogger.Debug("结束转发右键拖动事件");
                return;
            }
            ModLogger.Debug("进入 OnDrag()");

            // 只有超过时间或距离阈值才开始拖拽
            if (!dragStarted && (Time.time - dragStartTime > DragDelay || Vector2.Distance(eventData.position, dragStartPos) > MoveThreshold))
            {
                dragStarted = true;
                initialPosition = rectTransform.anchoredPosition;
                initialParent = transform.parent;
                initialSiblingIndex = transform.GetSiblingIndex();

                SetCanvasGroup(false);
                transform.SetAsLastSibling();

                ModLogger.Debug("拖拽正式开始");
            }

            if (dragStarted) 
            {
                rectTransform.anchoredPosition = new Vector2(initialPosition.x, GetLocalY(eventData));
                ModLogger.Debug("退出 OnDrag()");
            }
        }

        // 拖拽结束时触发
        public void OnEndDrag(PointerEventData eventData)
        {
            if (rightRect != null && !IsPermitted(eventData))
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = true;
                rightRect.OnEndDrag(eventData);
                return;
            }
            ModLogger.Debug("进入 OnEndDrag()");

            if (!dragStarted)
                return;

            dragStarted = false;
            SetCanvasGroup(true);

            rectTransform.anchoredPosition = initialPosition;
            transform.SetParent(this.initialParent);
            transform.SetSiblingIndex(this.initialSiblingIndex);

            ReorderHelper.AdjustIndex(modEntry, GetDeltaIdx(eventData));

            ModLogger.Debug("退出 OnEndDrag()");
        }

        private int GetDeltaIdx(PointerEventData eventData)
        {
            int result = 0;
            try
            {
                if (deltaHeight == null)
                    throw new InvalidOperationException("GetDeltaIdx 前未初始化 deltaHeight");
                var y = GetLocalY(eventData);
                ModLogger.Debug($"y = {y}, staY = {startY}, y - sY = {y - startY}");

                var r = (y - startY) / (float)deltaHeight;
                result = (int)(r <= 0 ? Math.Ceiling(r) : Math.Floor(r + 1));
                ModLogger.Debug($"r = {r}, ret = {result}");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"获取偏移时出错", ex);
            }
            return result;
        }

        private float GetLocalY(PointerEventData eventData)
        {
            RectTransform rect = (RectTransform)rectTransform.parent as RectTransform;
            Camera cam = eventData.pressEventCamera ?? Camera.main;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, cam, out Vector2 vector);
            return vector.y;
        }

        private float GetDeltaHeight(ModEntry modEntry)
        {
            var masterUI = ReflectionHelper.GetFieldValue<ModManagerUI>(modEntry, "master");
            var activeEntries = ReflectionHelper.GetFieldValue<PrefabPool<ModEntry>>(masterUI, "_pool").ActiveEntries;

            if (activeEntries.Count < 2)
            {
                ModLogger.Info("Mod数小于2，返回float.Max");
                return float.MaxValue;
            }

            RectTransform firstEntryRect = activeEntries[0].GetComponent<RectTransform>();
            RectTransform secondEntryRect = activeEntries[1].GetComponent<RectTransform>();

            var ret = secondEntryRect.anchoredPosition.y - firstEntryRect.anchoredPosition.y;
            ModLogger.Debug($"获取deltaHeight值为: {ret}");
            return ret;
        }

        private void SetCanvasGroup(bool vis)
        {
            if (canvasGroup == null)
            {
                ModLogger.Debug("创建canvasGroup");
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = vis ? InitialAlpha : DragingAlpha;
            canvasGroup.blocksRaycasts = vis;
        }
    }
}