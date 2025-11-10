using BetterModManager.Utils;
using Duckov.Modding;
using Duckov.Modding.UI;
using System;


namespace BetterModManager.UI
{
    public static class ReorderHelper
    {
        public static readonly int TopIdx = 0;
        public static readonly int BottomIdx = ModManager.modInfos.Count - 1;

        public static int GetIndex(ModEntry modEntry)
        {
            return ReflectionHelper.GetFieldValue<int>(modEntry, "index");
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

        public static int IncOrDec(ModEntry modEntry, bool isInc)
        {
            int srcIdx = GetIndex(modEntry);
            int dstIdx = Clamp(isInc ? srcIdx - 1 : srcIdx + 1);
            ModLogger.Info($"Mod '{GetName(srcIdx)}' " +
                           $"顺序{(isInc ? "上升" : "下降")}" +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? "成功" : $"失败，因为已在最{(isInc ? "顶" : "底")}")}");
            return dstIdx;
        }

        public static int Inc(ModEntry modEntry)
        {
            return IncOrDec(modEntry, true);
        }

        public static int Dec(ModEntry modEntry)
        {
            return IncOrDec(modEntry, false);
        }
    }
}
