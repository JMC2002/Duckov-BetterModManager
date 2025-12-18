using Duckov.Modding;
using Duckov.Modding.UI;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using System;

namespace BetterModManager.Utils
{
    public static class ReorderHelper
    {
        public static readonly int TopIdx = 0;
        public static readonly int BottomIdx = ModManager.modInfos.Count - 1;

        public static int GetIndex(ModEntry modEntry)
        {
            return MemberAccessor.Get(typeof(ModEntry), "index")
                                 .GetValue<ModEntry, int>(modEntry);
        }

        public static bool IsValidIndex(int idx)
        {
            return idx >= TopIdx && idx <= BottomIdx;
        }

        public static int Clamp(int idx)
        {
            return Math.Clamp(idx, TopIdx, BottomIdx);
        }

        // 不检查idx的范围是否合法
        public static string GetName(int idx)
        {
            return IsValidIndex(idx) ? ModManager.modInfos[idx].name : throw new IndexOutOfRangeException($"{idx} 下标不合法!");
        }

        private static bool ReorderImpl(int srcIdx, int dstIdx)
        {
            // 基础合法性检查
            if (srcIdx < 0 || srcIdx > BottomIdx || dstIdx < 0 || dstIdx > BottomIdx || srcIdx == dstIdx)
                return false;

            var srcInfo = ModManager.modInfos[srcIdx];
            bool isSrcPinned = PinManager.IsPinned(srcInfo);
            int pinnedCount = PinManager.GetPinnedCount();

            // 情况A: 移动的是置顶 Mod
            if (isSrcPinned)
            {
                // 置顶 Mod 只能在 [0, pinnedCount - 1] 范围内移动
                // 这里的 pinnedCount - 1 是置顶区的最后一个位置
                if (dstIdx >= pinnedCount)
                {
                    ModLogger.Warn($"[Reorder] 无法将置顶 Mod '{srcInfo.name}' 移出置顶区");
                    return false;
                }
            }
            // 情况B: 移动的是普通 Mod
            else
            {
                // 普通 Mod 只能在 [pinnedCount, BottomIdx] 范围内移动
                if (dstIdx < pinnedCount)
                {
                    ModLogger.Warn($"[Reorder] 无法将普通 Mod '{srcInfo.name}' 移入置顶区");
                    return false;
                }
            }

            return ModManager.Reorder(srcIdx, dstIdx);
        }

        public static void Reorder(int srcIdx, int dstIdx)
        {
            // srcIdx不合法在获取名字处就会抛异常，因此返回false只会是目标序号的问题
            ModLogger.Info($"Mod '{GetName(srcIdx)}' " +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? $"成功从 {srcIdx} 变更到 {dstIdx}" : "目标序号非法或不变，保留原样")}");
        }

        // 将modEntry的序号变更delta，若delta越界，自动收缩到合法区间
        public static void AdjustIndex(ModEntry modEntry, int delta)
        {
            int srcIdx = GetIndex(modEntry);
            int dstIdx = Clamp(srcIdx + delta);

            ModLogger.Info($"Mod '{GetName(srcIdx)}' " +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? $"成功从 {srcIdx} 变更到 {dstIdx}" : "目标序号不变，保留原样")}");
        }

        public static int ToTopOrBottom(ModEntry modEntry, bool isToTop)
        {
            string hintInfo = isToTop ? "置顶" : "置底";
            int srcIdx = GetIndex(modEntry);
            int dstIdx = isToTop ? TopIdx : BottomIdx;

            ModLogger.Info($"Mod '{GetName(srcIdx)}' {hintInfo}" +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? "成功" : $"失败，因为已经{hintInfo}了")}");

            return dstIdx;
        }

        public static int ToTop(ModEntry modEntry)
        {
            return ToTopOrBottom(modEntry, true);
        }

        public static int ToBottom(ModEntry modEntry)
        {
            return ToTopOrBottom(modEntry, false);
        }

        public static int IncOrDec(int srcIdx, bool isInc)
        {
            int dstIdx = Clamp(isInc ? srcIdx - 1 : srcIdx + 1);
            ModLogger.Info($"Mod '{GetName(srcIdx)}' " +
                           $"顺序{(isInc ? "上升" : "下降")}" +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? "成功" : $"失败，因为已在最{(isInc ? "顶" : "底")}")}");
            return dstIdx;
        }

        public static int Inc(int srcIdx)
        {
            return IncOrDec(srcIdx, true);
        }

        public static int Dec(int srcIdx)
        {
            return IncOrDec(srcIdx, false);
        }
    }
}
