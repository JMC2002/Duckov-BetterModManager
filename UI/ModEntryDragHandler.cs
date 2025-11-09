using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using Duckov.Utilities;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

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
        private bool isDragging = false;

        private const float DragingAlpha = 0.6f;
        private const float InitialAlpha = 1f;

        private float? deltaHeight;

        public void Setup(ModEntry modEntry)
        {
            this.modEntry = modEntry;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            SetCanvasGroup(true);
        }

        // 拖拽开始时触发
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 延迟初始化
            deltaHeight = deltaHeight ?? GetDeltaHeight(modEntry);

            startY = GetLocalY(eventData);
            isDragging = false;

            ModLogger.Debug("OnBeginDrag()");
        }

        // 在拖拽过程中触发
        public void OnDrag(PointerEventData eventData)
        {
            ModLogger.Debug("进入 OnDrag()");
            if (modEntry == null)
            {
                throw new InvalidOperationException("OnBeginDrag 前未初始化 modEntry");
            }

            if (!isDragging)
            {
                isDragging = true;
                initialPosition = rectTransform.anchoredPosition;
                initialParent = transform.parent;
                initialSiblingIndex = transform.GetSiblingIndex();

                SetCanvasGroup(false);
                transform.SetAsLastSibling();
            }

            rectTransform.anchoredPosition = new Vector2(initialPosition.x, GetLocalY(eventData));
            ModLogger.Debug("退出 OnDrag()");
        }

        // 拖拽结束时触发
        public void OnEndDrag(PointerEventData eventData)
        {
            ModLogger.Debug("进入 OnEndDrag()");
            if (modEntry == null)
                throw new InvalidOperationException("OnEndDrag 前未初始化 modEntry");
            if (!isDragging)
                return;

            isDragging = false;
            SetCanvasGroup(true);

            rectTransform.anchoredPosition = initialPosition;
            transform.SetParent(this.initialParent);
            transform.SetSiblingIndex(this.initialSiblingIndex);

            int deltaIdx = this.GetDeltaIdx(eventData);
            int nowIdx = ReorderHelper.GetIndex(modEntry);
            ReorderHelper.Reorder(nowIdx, nowIdx + deltaIdx);

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