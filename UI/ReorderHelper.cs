using BetterModManager.Utils;
using Duckov.Modding;
using System;
using System.Collections.Generic;
using System.Text;
using Duckov.Modding.UI;


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

        // 不检查idx的范围是否合法
        private static string GetName(int idx)
        {
            return IsValidIndex(idx) ? ModManager.modInfos[idx].name : throw new IndexOutOfRangeException($"{idx} 下标不合法!");
        }

        private static bool ReorderImpl(int srcIdx, int dstIdx)
        {
            return ModManager.Reorder(srcIdx, dstIdx);
        }

        public static void Reorder(int srcIdx, int dstIdx)
        {
            ModLogger.Info($"Mod '{GetName(srcIdx)}' ");

            // srcIdx不合法在获取名字处就会抛异常，因此返回false只会是目标序号的问题
            if (ReorderImpl(srcIdx, dstIdx))
                ModLogger.Info($"成功从 {srcIdx} 变更到 {dstIdx}");
            else
                ModLogger.Info("目标序号非法或不变，保留原样");

        }

        public static void ToTopOrBottom(int srcIdx, bool isToTop)
        {
            string hintInfo = isToTop ? "置顶" : "置底";
            int dstIdx = isToTop ? TopIdx : BottomIdx;

            ModLogger.Info($"Mod '{GetName(srcIdx)}' ");

            if (srcIdx == dstIdx)
                ModLogger.Info($"{hintInfo} 失败，因为已经{hintInfo}了");
            else if (ReorderImpl(srcIdx, dstIdx))
                ModLogger.Info($"{hintInfo}成功");
            else
                ModLogger.Info($"{hintInfo}失败");    // 理论上来讲不会发生
        }

        public static void ToTop(int Idx)
        {
            ToTopOrBottom(Idx, true);
        }

        public static void ToBottom(int Idx)
        {
            ToTopOrBottom(Idx, false);
        }

        public static void IncOrDec(int srcIdx, bool isInc)
        {
            int dstIdx = isInc ? srcIdx - 1 : srcIdx + 1;
            ModLogger.Info($"Mod '{GetName(srcIdx)}' " +
                           $"顺序{(isInc ? "上升" : "下降")}" +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? "成功" : $"失败，因为已在最{(isInc ? "顶" : "底")}")}");
        }

        public static void Inc(int idx)
        {
            IncOrDec(idx, true);
        }

        public static void Dec(int idx)
        {
            IncOrDec(idx, false);
        }
    }
}
