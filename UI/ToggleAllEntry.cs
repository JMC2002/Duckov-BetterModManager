using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using Duckov.Utilities;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterModManager.UI
{
    public static class ToggleAllEntry
    {
        // 缓存的全选按钮
        private static Button? toggleAllButton;

        public static void Setup(ModManagerUI __instance)
        {
            try
            {
                if (__instance == null) return;

                // 创建全选按钮，并设置样式
                // if (toggleAllButton == null)
                {
                    var pool = MemberAccessor.Get(typeof(ModManagerUI), "_pool")
                                             .GetValue<ModManagerUI, PrefabPool<ModEntry>>(__instance);
                    var firstModEntry = pool.Find(m => true);   // 由于启用这个MOD至少有这个MOD，因此至少有一个MOD

                    var toggleButton = MemberAccessor.Get(typeof(ModEntry), "toggleButton")
                                                     .GetValue<ModEntry, Button>(firstModEntry);

                    var activeIndicatorTmp = MemberAccessor.Get(typeof(ModEntry), "activeIndicator")
                                                           .GetValue<ModEntry, GameObject>(firstModEntry);

                    var tmp = activeIndicatorTmp.activeSelf;
                    activeIndicatorTmp.SetActive(false);
                    //ModLogger.Info($"active: {activeIndicatorTmp.activeSelf}");
                    toggleAllButton = UnityEngine.Object.Instantiate(toggleButton, __instance.transform);
                    var activeAllIndicator = UnityEngine.Object.Instantiate(activeIndicatorTmp, toggleAllButton.transform);
                    activeIndicatorTmp.SetActive(tmp);

                    RectTransform rectTransform = toggleAllButton.GetComponent<RectTransform>();
                    RectTransform rectTransformActive = activeAllIndicator.GetComponent<RectTransform>();
                    toggleAllButton.transform.position = toggleButton.transform.position + Vector3.left * rectTransform.rect.width * 3;

                    // 添加按钮的点击事件
                    toggleAllButton.onClick.AddListener(() => OnToggleAllButtonClicked(__instance, ref activeAllIndicator));
                    ModLogger.Info("已成功创建全选按钮。");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"创建全选按钮时出错", ex);
            }
        }

        // 点击全选按钮时的处理逻辑
        private static void OnToggleAllButtonClicked(ModManagerUI __instance, ref GameObject activeAllIndicator)
        {
            try
            {
                ModLogger.Info("全选/全不选按钮被点击");
                ModLogger.Info($"点击前 interactable: {toggleAllButton?.interactable}, active: {activeAllIndicator.activeSelf}");
                activeAllIndicator.SetActive(!activeAllIndicator.activeSelf);
                ModLogger.Info($"点击后 interactable: {toggleAllButton?.interactable}, active: {activeAllIndicator.activeSelf}");

                bool now = activeAllIndicator.activeSelf;

                // 遍历所有的 ModEntry 控件
                var pool = MemberAccessor.Get(typeof(ModManagerUI), "_pool")
                                         .GetValue<ModManagerUI, PrefabPool<ModEntry>>(__instance);
                if (pool == null)
                {
                    return;
                }

                var activeObjects = MemberAccessor.Get(typeof(PrefabPool<ModEntry>), "activeObjects")
                                                  .GetValue<PrefabPool<ModEntry>, List<ModEntry>>(pool);
                if (activeObjects == null)
                {
                    ModLogger.Info("activeObjects不存在");
                    return;
                }
                else
                {
                    ModLogger.Info($"activeObjects.size(): {activeObjects.Count}");
                }

                // 预获取访问器
                var infoAccessor = MemberAccessor.Get(typeof(ModEntry), "info");
                var activeIndicatorAccessor = MemberAccessor.Get(typeof(ModEntry), "activeIndicator");
                var toggleButtonMethod = MethodAccessor.Get(typeof(ModEntry), "OnToggleButtonClicked", Type.EmptyTypes);

                foreach (ModEntry modEntry in now ? activeObjects : activeObjects.AsEnumerable().Reverse())
                {
                    var info = MemberAccessor.Get(typeof(ModEntry), "info")
                                             .GetValue<ModEntry, ModInfo>(modEntry);

                    if (LockManager.IsLocked(info))
                    {
                        ModLogger.Debug($"模组 {info.name} 已被锁定，跳过全选/反选操作。");
                        continue;
                    }

                    var toggleButton = MemberAccessor.Get(typeof(ModEntry), "toggleButton")
                                                     .GetValue<ModEntry, Button>(modEntry);
                    var activeIndicator = MemberAccessor.Get(typeof(ModEntry), "activeIndicator")
                                                        .GetValue<ModEntry, GameObject>(modEntry);

                    bool b = ModManager.IsModActive(info, out Duckov.Modding.ModBehaviour context);

                    ModLogger.Debug($"遍历到{info.name}, 当前active为: {activeIndicator.activeSelf}，modActive为：{b}");

                    if (activeIndicator.activeSelf != now && b != now)
                    {
                        MethodAccessor.Get(typeof(ModEntry), "OnToggleButtonClicked", Type.EmptyTypes)
                                      .InvokeVoid<ModEntry>(modEntry);
                    }
                    else
                    {
                        ModLogger.Debug("当前状态与目标相同，跳过");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"处理全选按钮点击时出错: {ex}");
            }
        }
    }
}