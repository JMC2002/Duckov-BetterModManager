using BetterModManager.Patches;
using Duckov.Modding;
using BetterModManager.Utils; // 假设你用相同工具模块

namespace BetterModManager
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private HarmonyHelper harmonyHelper = new("BetterModManager");

        void Awake()
        {
        }

        void OnEnable()
        {
            ModLogger.Info("启用中...");
            harmonyHelper.OnEnable();
            ModLogger.Info("已加载 Harmony Patch");
        }

        void OnDisable()
        {
            harmonyHelper.OnDisable();
            ModLogger.Info("已禁用 Harmony Patch");
        }

        void OnDestroy()
        {
        }
    }
}
