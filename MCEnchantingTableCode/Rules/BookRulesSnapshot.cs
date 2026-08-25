using MCEnchantingTable.MCEnchantingTableCode.Config;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MCEnchantingTable.MCEnchantingTableCode.Rules;

public readonly record struct BookRulesSnapshot(
    int Act1NormalCombatsPerReward,
    int Act2NormalCombatsPerReward,
    int Act3NormalCombatsPerReward,
    int NormalCombatBookRewardAmount,
    int EliteBookRewardAmount,
    int BossBookRewardAmount)
{
    public static BookRulesSnapshot Defaults => new(1, 2, 3, 1, 1, 2);

    public static BookRulesSnapshot FromGlobalSettings() => new BookRulesSnapshot(
        GameplaySettings.Act1NormalCombatsPerReward,
        GameplaySettings.Act2NormalCombatsPerReward,
        GameplaySettings.Act3NormalCombatsPerReward,
        GameplaySettings.NormalCombatBookRewardAmount,
        GameplaySettings.EliteBookRewardAmount,
        GameplaySettings.BossBookRewardAmount).Sanitized();

    public int GetNormalCombatsPerReward(int actIndex) => actIndex switch
    {
        0 => Act1NormalCombatsPerReward,
        1 => Act2NormalCombatsPerReward,
        2 => Act3NormalCombatsPerReward,
        _ => Act3NormalCombatsPerReward,
    };

    public BookRulesSnapshot Sanitized() => new BookRulesSnapshot(
        Math.Clamp(Act1NormalCombatsPerReward, 1, 20),
        Math.Clamp(Act2NormalCombatsPerReward, 1, 20),
        Math.Clamp(Act3NormalCombatsPerReward, 1, 20),
        Math.Clamp(NormalCombatBookRewardAmount, 0, 20),
        Math.Clamp(EliteBookRewardAmount, 0, 20),
        Math.Clamp(BossBookRewardAmount, 0, 20));

    public void Write(PacketWriter writer)
    {
        writer.WriteInt(Act1NormalCombatsPerReward);
        writer.WriteInt(Act2NormalCombatsPerReward);
        writer.WriteInt(Act3NormalCombatsPerReward);
        writer.WriteInt(NormalCombatBookRewardAmount);
        writer.WriteInt(EliteBookRewardAmount);
        writer.WriteInt(BossBookRewardAmount);
    }

    public static BookRulesSnapshot Read(PacketReader reader) => new BookRulesSnapshot(
        reader.ReadInt(), reader.ReadInt(), reader.ReadInt(),
        reader.ReadInt(), reader.ReadInt(), reader.ReadInt()).Sanitized();
}
