using Duckov.Modding;
using JmcModLib.Config;
using JmcModLib.Utils;
using System;
using System.Collections.Generic;

namespace BetterModManager.Utils
{
    public static class LockManager
    {
        // 内部使用 HashSet 保证查询速度和唯一性
        private static readonly HashSet<string> _lockedSet = new();

        // 缓存 Key
        private static string _configKey = string.Empty;

        // === 核心技巧 ===
        // 定义一个 string 属性作为“代理”。
        // 当 ConfigManager 读取时，get 会把 Set 拼成字符串。
        // 当 ConfigManager 写入时（比如从文件加载），set 会把字符串解析回 Set。
        // 这里的 StaticName 就是 "SerializedPaths"
        private static string SerializedPaths
        {
            get
            {
                // 用竖线 | 分隔，因为路径里不可能包含这个字符
                return string.Join("|", _lockedSet);
            }
            set
            {
                _lockedSet.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    var items = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in items)
                    {
                        _lockedSet.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// 初始化并注册配置。务必在 MOD 加载早期调用。
        /// </summary>
        public static void Initialize()
        {
            if (!string.IsNullOrEmpty(_configKey)) return;

            // 注册的是 SerializedPaths 这个属性
            // ConfigManager 会在注册时自动调用一次 SerializedPaths 的 set 方法（如果文件里有值）
            _configKey = ConfigManager.RegisterConfig(
                "LockedModPaths",
                () => SerializedPaths,
                "BetterModManager"
            );

            ModLogger.Info($"[LockManager] 配置已注册，Key: {_configKey}，当前锁定: {_lockedSet.Count} 个");
        }

        public static bool IsLocked(ModInfo info)
        {
            if (string.IsNullOrEmpty(info.dllPath)) return false;
            return _lockedSet.Contains(info.dllPath);
        }

        public static void SetLocked(ModInfo info, bool isLocked)
        {
            if (string.IsNullOrEmpty(info.dllPath)) return;

            bool changed;
            if (isLocked)
            {
                changed = _lockedSet.Add(info.dllPath);
            }
            else
            {
                changed = _lockedSet.Remove(info.dllPath);
            }

            if (changed)
            {
                if (!string.IsNullOrEmpty(_configKey))
                {
                    // 数据变动后，调用 SetValue。
                    // 此时 SerializedPaths 的 get 访问器会被调用，生成新的字符串传给 ConfigManager 保存。
                    ConfigManager.SetValue(_configKey, SerializedPaths);
                    ModLogger.Debug($"[LockManager] 状态更新: {info.name} (Locked: {isLocked})");
                }
                else
                {
                    ModLogger.Error("[LockManager] 未初始化！请先调用 Initialize()");
                }
            }
        }
    }
}