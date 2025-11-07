using BetterModManager.Core;
using BetterModManager.Patches;
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
            ModConfig.Load();
            harmonyHelper.OnEnable();
            ModLogger.Info("模组已启用");
        }

        void OnDisable()
        {
            harmonyHelper.OnDisable();

            ModConfig.Save();
            ModLogger.Info("Mod 已禁用，配置已保存");
        }

        void OnDestroy()
        {
        }
    }
}
