using Godot;

namespace MCEnchantingTable.MCEnchantingTableCode.Assets;

/// <summary>
/// Central catalog for player-facing image resources.
/// </summary>
internal static class MCEnchantingTableAssets
{
    internal static class ConfigAssets
    {
        public const string EnchantmentConfigPath =
            "res://config/MCEnchantingConfig.json";
    }

    internal static class RelicAssets
    {
        public const string StrangeBookPackedIconPath =
            "res://MCEnchantingTable/images/relics/strange_book_packed.png";

        public const string StrangeBookBigIconPath =
            "res://MCEnchantingTable/images/relics/strange_book.png";
    }

    internal static class AncientAssets
    {
        public const string EnchantButtonPath =
            "res://MCEnchantingTable/images/ancient/ancient_enchant_button.png";

        public const string EnchantButtonDisabledPath =
            "res://MCEnchantingTable/images/ancient/ancient_enchant_button_disabled.png";
    }

    internal static class RestSiteAssets
    {
        public const string EnchantButtonPath =
            "res://MCEnchantingTable/images/rest_site/campfire_enchant_button.png";

        public const string EnchantButtonDisabledPath =
            "res://MCEnchantingTable/images/rest_site/campfire_enchant_button_disabled.png";
    }

    internal static class AudioAssets
    {
        public static IReadOnlyList<string> EnchantConfirmSounds { get; } =
            Array.AsReadOnly(new[]
            {
                "res://MCEnchantingTable/audio/enchant/enchant_01.ogg",
                "res://MCEnchantingTable/audio/enchant/enchant_02.ogg",
                "res://MCEnchantingTable/audio/enchant/enchant_03.ogg",
            });
    }

    internal static class EnchantUiAssets
    {
        public const string BackgroundPath =
            "res://MCEnchantingTable/images/enchant/enchant_background.png";

        public const string CardSlotPath =
            "res://MCEnchantingTable/images/enchant/card_slot.png";

        public const string CardSlotPlusPath =
            "res://MCEnchantingTable/images/enchant/card_slot_plus.svg";

        public const string OptionSlotPath =
            "res://MCEnchantingTable/images/enchant/enchant_option_slot.png";

        public const string BackButtonScenePath = "res://scenes/ui/back_button.tscn";

        public const string ConfirmButtonScenePath = "res://scenes/ui/confirm_button.tscn";

        public const string NativeEnchantSelectionScenePath =
            "res://scenes/screens/card_selection/deck_enchant_select_screen.tscn";

        public const string PromptRegularFontPath = "res://themes/kreon_regular_shared.tres";

        public const string PromptBoldFontPath = "res://themes/kreon_bold_shared.tres";
    }

    public static Texture2D? LoadTexture(string path) => ResourceLoader.Load<Texture2D>(path);
}
