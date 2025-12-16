using BetterModManager.Core;
using Duckov.Modding;
using UnityEngine;

namespace BetterModManager
{
    // 继承 DependencyModLoader
    public class ModBehaviour : DependencyModLoader
    {
        protected override ModDependency[] GetDependencies()
        {
            // 这里是所有的前置依赖项，在加载完前置后才会加载实际的脚本
            return
            [
                new ModDependency("JmcModLib", 3613297900),
            ];
        }

        // 挂载实际业务脚本
        protected override MonoBehaviour CreateImplementation(ModManager master, ModInfo info)
        {
            // 1. 挂载组件
            var impl = this.gameObject.AddComponent<ModBehaviourImpl>();

            impl.Setup(master, info);

            return impl;
        }
    }
}