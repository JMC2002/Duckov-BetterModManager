using BetterModManager.Patches;
using JmcModLib.Core;
using JmcModLib.Utils;

namespace BetterModManager.Core
{
    public class ModBehaviourImpl : Duckov.Modding.ModBehaviour
    {
        private readonly HarmonyHelper harmonyHelper = new("BetterModManager");

        protected override void OnAfterSetup()
        {
            ModLogger.Info($"前置加载完成，开始启动Mod...");

            ModRegistry.Register(true, this.info, VersionInfo.Name, VersionInfo.Version)?
                       .RegisterL10n()
                       .RegisterLogger()
                       .Done();

            harmonyHelper.OnEnable();
            ModLogger.Info("模组已启用 (延迟加载成功)");
        }

        // 对应 Loader 调用的手动清理
        public void ManualDeactivate()
        {
            // 这里也可以调用 base.NotifyBeforeDeactivate() 如果你需要触发 OnBeforeDeactivate
            harmonyHelper.OnDisable();
            ModLogger.Info("Mod 已禁用，配置已保存");
        }

        // 甚至可以重写这个，如果 Loader 也是通过 NotifyBeforeDeactivate 调用的
        protected override void OnBeforeDeactivate()
        {
            // 具体的清理逻辑
        }

        private void OnDestroy()
        {
            harmonyHelper.OnDisable();
        }
    }
}