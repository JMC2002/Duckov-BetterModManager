using Duckov.Modding;
using JmcModLib.Storage; // 使用我们之前封装的 JmcES3Manager
using System.Reflection;

namespace BetterModManager.Utils
{
    public static class PinManager
    {
        private static readonly Assembly _asm = Assembly.GetExecutingAssembly();

        public static bool IsPinned(ModInfo info)
        {
            // Key 格式: BMM_Pin_ModName_SteamID
            string key = "BMM_Pin_" + info.GetUniqueId();
            return JmcES3Manager.Load(key, false, _asm);
        }

        public static void SetPinned(ModInfo info, bool isPinned)
        {
            string key = "BMM_Pin_" + info.GetUniqueId();
            JmcES3Manager.Save(key, isPinned, _asm);
        }

        /// <summary>
        /// 获取当前置顶的 Mod 数量（即分界线索引）
        /// </summary>
        public static int GetPinnedCount()
        {
            int count = 0;
            // 遍历当前列表，统计有多少个是置顶状态
            // 注意：这里假设列表已经经过排序，置顶的都在上面。
            // 但为了安全，我们统计所有 IsPinned 为 true 的个数。
            foreach (var info in ModManager.modInfos)
            {
                if (IsPinned(info)) count++;
            }
            return count;
        }
    }
}