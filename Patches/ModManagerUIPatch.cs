using BetterModManager.Utils;
using Duckov.Modding.UI;
using Duckov.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterModManager.Patches
{
    [HarmonyPatch(typeof(ModManagerUI))]
    public static class ModManagerUI_Patch
    {
        // 缓存的全选按钮
        private static Button toggleAllButton;

        // 在 ModManagerUI.OnEnable 执行后添加全选按钮
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        private static void PostOnEnable(ModManagerUI __instance)
        {
            //try
            //{
            //    if (__instance == null) return;
                
            //    // 创建全选按钮，并设置样式
            //    if (toggleAllButton == null)
            //    {
            //        var pool = ReflectionHelper.GetFieldValue<PrefabPool<ModEntry>>(__instance, "_pool");
            //        var firstModEntry = pool.Find(m => true);   // 由于启用这个MOD至少有这个MOD，因此至少有一个MOD
            //        var toggleButton = ReflectionHelper.GetFieldValue<Button>(firstModEntry, "toggleButton");
            //        toggleAllButton = UnityEngine.Object.Instantiate(toggleButton, toggleButton.transform.parent);
            //        RectTransform rectTransform = toggleAllButton.GetComponent<RectTransform>();
            //        RectTransform firstModEntryRectTransform = firstModEntry.GetComponent<RectTransform>();
            //        ModLogger.Debug($"local: {rectTransform.localPosition}");
            //        ModLogger.Debug($"pos: {rectTransform.position}");
            //        ModLogger.Debug($"rect: {rectTransform.rect.width}");
            //        ModLogger.Debug($"left: {Vector3.left}");
            //        // rectTransform.localPosition = firstModEntryRectTransform.localPosition + Vector3.left * rectTransform.rect.width * 2;
            //        // toggleAllButton.gameObject.SetActive(true);  // 确保按钮是激活的

            //        toggleAllButton.transform.SetSiblingIndex(toggleButton.transform.GetSiblingIndex());
            //        toggleAllButton.transform.localPosition = toggleButton.transform.localPosition + Vector3.left * rectTransform.rect.width * 2;
            //       // toggleAllButton.transform.SetParent(__instance.transform);
                    
            //        ModLogger.Debug($"local: {rectTransform.localPosition}");
            //        ModLogger.Debug($"pos: {rectTransform.position}");
            //        ModLogger.Debug($"rect: {rectTransform.rect.width}");
            //        ModLogger.Debug($"left: {Vector3.left}");
            //        // 添加按钮的点击事件
            //        // toggleAllButton.onClick.AddListener(() => OnToggleAllButtonClicked(__instance));
            //        toggleAllButton.onClick.AddListener(() => {
            //            ModLogger.Info("全选按钮被点击");
            //            // 在这里添加全选的逻辑
            //        });

            //        ModLogger.Info("已成功创建全选按钮。");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    ModLogger.Error($"在 {nameof(PostOnEnable)} 中创建全选按钮时出错: {ex}");
            //}
        }

        // 点击全选按钮时的处理逻辑
        private static void OnToggleAllButtonClicked(ModManagerUI __instance)
        {
            try
            {
                ModLogger.Info("全选/全不选按钮被点击");

                bool newState = true; // 假设我们开始时是全选
                bool anySelected = false;

                // 遍历所有的 ModEntry 控件
                var pool = ReflectionHelper.GetFieldValue<PrefabPool<ModEntry>>(__instance, "_pool");
                if (pool == null)
                {
                    return;
                }

                var activeObjects = ReflectionHelper.GetFieldValue<List<ModEntry>>(pool, "activeObjects");
                if (activeObjects == null)
                {
                    return;
                }

                foreach (ModEntry modEntry in activeObjects)
                {
                    // 获取每个 ModEntry 的 toggleButton
                    var toggleButton = ReflectionHelper.GetFieldValue<Button>(modEntry, "toggleButton"); 
                    if (toggleButton != null)
                    {
                        bool isSelected = toggleButton.interactable; // 假设通过 interactable 判断是否勾选
                        if (isSelected)
                        {
                            anySelected = true; // 只要有一个已经选中了，就改变全选/全不选状态
                            break;
                        }
                    }
                }

                // 如果有选中的 ModEntry，就改为全不选，否则改为全选
                newState = !anySelected;

                // 遍历所有 ModEntry，设置它们的勾选状态
                foreach (ModEntry modEntry in activeObjects)
                {
                    var toggleButton = ReflectionHelper.GetFieldValue<Button>(modEntry, "toggleButton");
                    if (toggleButton != null)
                    {
                        toggleButton.interactable = newState; // 设置 toggleButton 的选中状态
                    }
                }

                ModLogger.Info($"全选状态：{newState}");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"处理全选按钮点击时出错: {ex}");
            }
        }
    }
}
