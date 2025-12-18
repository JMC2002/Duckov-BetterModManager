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
        public static int BottomIdx => ModManager.modInfos.Count - 1;   // 由于 Mod 列表中必然至少有这个Mod，因此 BottomIdx 永远合法

        // 置顶区：[TopIdx, 置顶数-1]
        // 假如只有 1 个置顶
        // 普通区：[置顶数, BottomIdx]
        // 假如没有置顶，范围是 [TopIdx, BottomIdx]，范围是 [TopIdx, TopIdx]
        public static int PinnedCount => PinManager.GetPinnedCount();   // 置顶 Mod 数量，同时是普通 Mod 区的起始索引

        public static int GetIndex(ModEntry modEntry)
        {
            return MemberAccessor.Get(typeof(ModEntry), "index")
                                 .GetValue<ModEntry, int>(modEntry);
        }

        // --- 获取某个 Mod 允许移动的合法区间 ---
        public static (int min, int max) GetValidRange(ModInfo info)
        {
            bool isPinned = PinManager.IsPinned(info);

            if (isPinned)
            {
                return (TopIdx, Math.Max(TopIdx, PinnedCount - 1));
            }
            else
            {
                return (PinnedCount, Math.Max(PinnedCount, BottomIdx));
            }
        }

        public static bool IsValidIndex(int idx)
        {
            return idx >= TopIdx && idx <= BottomIdx;
        }

        public static int Clamp(int idx, bool isPinned)
        {
            return Math.Clamp(idx, isPinned ? TopIdx : PinnedCount, isPinned ? Math.Max(TopIdx, PinnedCount - 1) : BottomIdx);
        }

        public static ModInfo GetModInfo(int idx)
        {
            return IsValidIndex(idx) ? ModManager.modInfos[idx] : throw new IndexOutOfRangeException($"{idx} 下标不合法!");
        }

        public static string GetName(int idx)
        {
            return GetModInfo(idx).name;
        }

        private static bool ReorderImpl(int srcIdx, int dstIdx)
        {
            if (!IsValidIndex(srcIdx) || !IsValidIndex(dstIdx) || srcIdx == dstIdx)
                return false;

            var srcInfo = GetModInfo(srcIdx);
            bool isSrcPinned = PinManager.IsPinned(srcInfo);

            if (isSrcPinned && dstIdx >= PinnedCount)
            {
                ModLogger.Warn($"无法将置顶 Mod '{srcInfo.name}' 移出置顶区");
                return false;
            }
            else if (!isSrcPinned && dstIdx < PinnedCount)
            {
                ModLogger.Warn($"无法将普通 Mod '{srcInfo.name}' 移入置顶区");
                return false;
            }

            return ModManager.Reorder(srcIdx, dstIdx);
        }

        // --- 新增：切换置顶状态 ---
        public static void TogglePin(ModEntry modEntry)
        {
            int srcIdx = GetIndex(modEntry);
            var info = GetModInfo(srcIdx);
            bool currentlyPinned = PinManager.IsPinned(info);
            bool newPinnedState = !currentlyPinned;

            // 1. 保存状态
            PinManager.SetPinned(info, newPinnedState);

            // 2. 移动位置
            // 注意：GetPinnedCount 是基于 IsPinned 统计的。
            // 此时我们已经 SetPinned 更新了状态，所以 pinnedCount 已经是更新后的数量了。

            int dstIdx;
            if (newPinnedState)
            {
                // 置顶操作：
                // 把它放到置顶区的最下面。
                // 现在的 pinnedCount 包含它自己，所以置顶区是 [0, pinnedCount-1]。
                // 它的目标位置应该是 pinnedCount - 1。
                dstIdx = PinnedCount - 1;
            }
            else
            {
                // 取消置顶：
                // 把它放到置顶区下方，即普通区的第一个位置。
                // 取消置顶后，pinnedCount 变小了。它应该去 pinnedCount 这个位置。
                // (因为 list 索引从 0 开始，pinnedCount 正好是置顶区后面第一个)
                dstIdx = PinnedCount;
            }

            if (srcIdx != dstIdx)
            {
                ReorderImpl(srcIdx, dstIdx);
                ModLogger.Info($"'{info.name}' {(newPinnedState ? "置顶" : "取消置顶")} -> 移动到 {dstIdx}");
            }
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
            var info = GetModInfo(srcIdx);
            int dstIdx = Clamp(srcIdx + delta, PinManager.IsPinned(info));

            ModLogger.Info($"Mod '{GetName(srcIdx)}' " +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? $"成功从 {srcIdx} 变更到 {dstIdx}" : "目标序号不变，保留原样")}");
        }

        public static int ToTopOrBottom(ModEntry modEntry, bool isToTop)
        {
            int srcIdx = GetIndex(modEntry);
            var info = GetModInfo(srcIdx);

            // 获取该 Mod 的合法边界
            var (min, max) = GetValidRange(info);

            // 如果是往上，就去 min；如果是往下，就去 max
            // 这样置顶 Mod 点击"置底"时，会停在置顶区的最下面，而不是列表最下面
            int dstIdx = isToTop ? min : max;

            string hintInfo = isToTop ? "置顶" : "置底";
            ModLogger.Info($"Mod '{info.name}' {hintInfo} -> {dstIdx} " +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? "成功" : "无需移动")}");

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
            // 优先级上升，代表着序号下降
            int dstIdx = Clamp(isInc ? srcIdx - 1 : srcIdx + 1, PinManager.IsPinned(GetModInfo(srcIdx)));
            ModLogger.Info($"Mod '{GetName(srcIdx)}' " +
                           $"顺序{(isInc ? "上升" : "下降")}" +
                           $"{(ReorderImpl(srcIdx, dstIdx) ? "成功" : $"失败，因为已在最{(isInc ? "顶" : "底")}")}");
            return dstIdx;
        }

        // 优先级上升一位（序号减一）
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
