using Duckov.Modding;

namespace BetterModManager.Utils
{
    public static class ModInfoExtensions
    {
        /// <summary>
        /// 获取 Mod 的唯一标识符。
        /// 本地 Mod: name_local
        /// Steam Mod: name_steam_ID
        /// </summary>
        public static string GetUniqueId(this ModInfo info)
        {
            if (info.isSteamItem)
                return $"{info.name}_steam_{info.publishedFileId}";

            return $"{info.name}_local";
        }

        public static string GetBmmPriorityKey(this ModInfo info)
            => "BMM_Priority_" + info.GetUniqueId();

        public static string GetBmmStateKey(this ModInfo info)
            => "BMM_State_" + info.GetUniqueId();
    }
}