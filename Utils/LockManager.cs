using Duckov.Modding;
using JmcModLib.Storage;
using JmcModLib.Utils;
using System.Reflection;

namespace BetterModManager.Utils
{
    public static class LockManager
    {
        // 缓存当前程序集引用，微小性能优化
        private static readonly Assembly _asm = Assembly.GetExecutingAssembly();

        /// <summary>
        /// 获取存储用的 Key。
        /// 使用 GetUniqueId() 替代 dllPath，更稳定（SteamID/Local 后缀）。
        /// </summary>
        private static string GetKey(ModInfo info) => "BMM_Lock_" + info.GetUniqueId();

        /// <summary>
        /// 检查 Mod 是否被锁定
        /// </summary>
        public static bool IsLocked(ModInfo info)
        {
            // 直接从 ES3 读取。如果 Key 不存在，返回 false。
            // 这里的读取非常快，因为 JmcES3Manager 内部缓存了 Settings。
            return JmcES3Manager.Load(GetKey(info), false, _asm);
        }

        /// <summary>
        /// 设置 Mod 锁定状态
        /// </summary>
        public static void SetLocked(ModInfo info, bool isLocked)
        {
            string key = GetKey(info);

            // 直接写入 ES3
            // 1 = True, 0 = False (或者直接存 bool 也可以，ES3 支持 bool)
            // 这里存 bool 最直观
            JmcES3Manager.Save(key, isLocked, _asm);

            ModLogger.Debug($"[LockManager] {(isLocked ? "锁定" : "解锁")}: {info.name}");
        }
    }
}