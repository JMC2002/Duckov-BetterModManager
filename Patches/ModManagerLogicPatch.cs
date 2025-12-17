using BetterModManager.Utils;
using Duckov.Modding;
using HarmonyLib;
using JmcModLib.Storage; // 引用新的 JmcES3Manager
using System.Reflection; // 需要引用这个来获取 Assembly

namespace BetterModManager.Patches
{
    [HarmonyPatch(typeof(ModManager))]
    public static class ModManagerLogicPatch
    {
        // 缓存当前程序集，避免每次调用 Save/Load 都走 StackTrace 查找 (微小的性能优化)
        private static readonly Assembly _currentAssembly = Assembly.GetExecutingAssembly();

        private static void SetUniquePriority(ModInfo info, int priority)
        {
            string key = info.GetBmmPriorityKey();
            // 直接调用，路径会自动处理为 Saves/JmcModLib/Storage/BetterModManager.es3
            JmcES3Manager.Save(key, priority, _currentAssembly);
        }

        private static int GetUniquePriority(ModInfo info)
        {
            string key = info.GetBmmPriorityKey();
            // 读取
            int val = JmcES3Manager.Load(key, int.MinValue, _currentAssembly);

            if (val == int.MinValue)
            {
                return ModManager.GetModPriority(info.name);
            }
            return val;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ShouldActivateMod")]
        private static bool PrefixShouldActivateMod(ModInfo info, ref bool __result)
        {
            string key = info.GetBmmStateKey();
            int state = JmcES3Manager.Load(key, -1, _currentAssembly);

            if (state == -1) return true;
            __result = (state == 1);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetShouldActivateMod")]
        private static void PostfixSetShouldActivateMod(ModInfo info, bool value)
        {
            string key = info.GetBmmStateKey();
            JmcES3Manager.Save(key, value ? 1 : 0, _currentAssembly);
        }
    }
}