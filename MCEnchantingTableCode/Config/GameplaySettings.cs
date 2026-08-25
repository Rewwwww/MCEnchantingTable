using BaseLib.Config;
using Godot;

namespace MCEnchantingTable.MCEnchantingTableCode.Config;

[ConfigHoverTipsByDefault]
public sealed class GameplaySettings : SimpleModConfig
{
    public override void SetupConfigUI(Control optionContainer)
    {
        optionContainer.AddChild(CreateSectionHeader("ConfigTitle", alignToTop: true));
        GenerateOptionsForAllProperties(optionContainer);
        AddRestoreDefaultsButton(optionContainer);
        SetupFocusNeighbors(optionContainer);
    }

    [ConfigSection("NormalCombatProgress")]
    [ConfigSlider(1, 20, 1, Format = "{0:0}")]
    public static int Act1NormalCombatsPerReward { get; set; } = 1;

    [ConfigSlider(1, 20, 1, Format = "{0:0}")]
    public static int Act2NormalCombatsPerReward { get; set; } = 2;

    [ConfigSlider(1, 20, 1, Format = "{0:0}")]
    public static int Act3NormalCombatsPerReward { get; set; } = 3;

    [ConfigSection("BookRewards")]
    [ConfigSlider(0, 20, 1, Format = "{0:0}")]
    public static int NormalCombatBookRewardAmount { get; set; } = 1;

    [ConfigSlider(0, 20, 1, Format = "{0:0}")]
    public static int EliteBookRewardAmount { get; set; } = 1;

    [ConfigSlider(0, 20, 1, Format = "{0:0}")]
    public static int BossBookRewardAmount { get; set; } = 2;
}
