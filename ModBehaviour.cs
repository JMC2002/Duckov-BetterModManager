using BetterModManager.Core;
using BetterModManager.Patches;
using JmcModLib.Core;
using JmcModLib.Utils;

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

        protected override void OnAfterSetup()
        {
            ModRegistry.Register(true, info, VersionInfo.Name, VersionInfo.Version)?
                       .RegisterLogger()
                       .Done();
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
