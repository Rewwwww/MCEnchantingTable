using MCEnchantingTable.MCEnchantingTableCode.Models;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;

namespace MCEnchantingTable.MCEnchantingTableCode.Rules;

internal static class CombatBookRewardCalculator
{
    public static int Calculate(StrangeBook strangeBook, CombatRoom room)
    {
        MapPointType sourceMapPointType =
            room.CombatState.RunState.CurrentMapPointHistoryEntry?.MapPointType ?? MapPointType.Unassigned;

        if (!IsEligibleCombat(room.RoomType, sourceMapPointType, room.ParentEventId is not null))
        {
            return 0;
        }

        return room.RoomType switch
        {
            RoomType.Monster => strangeBook.AdvanceNormalCombatProgress(room.CombatState.RunState.CurrentActIndex),
            RoomType.Elite => strangeBook.Rules.EliteBookRewardAmount,
            RoomType.Boss => strangeBook.Rules.BossBookRewardAmount + strangeBook.ConsumeActRemainderReward(),
            _ => 0,
        };
    }

    public static bool IsEligibleCombat(
        RoomType roomType,
        MapPointType sourceMapPointType,
        bool hasParentEvent)
    {
        if (hasParentEvent)
        {
            return false;
        }

        return roomType switch
        {
            RoomType.Monster => sourceMapPointType is MapPointType.Monster or MapPointType.Unknown,
            RoomType.Elite => sourceMapPointType == MapPointType.Elite,
            RoomType.Boss => sourceMapPointType == MapPointType.Boss,
            _ => false,
        };
    }
}
