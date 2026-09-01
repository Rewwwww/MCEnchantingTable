using MCEnchantingTable.MCEnchantingTableCode.Config;
using MCEnchantingTable.MCEnchantingTableCode.RestSite;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace MCEnchantingTable.MCEnchantingTableCode.Compatibility;

/// <summary>The audited RestSiteOption and CreatureCmd signatures match in both hosts.</summary>
internal static class EnchantEntranceAdapter
{
    internal static bool CanShowCampfireEnchant() => GameVersionCompatibility.IsSupported && GameplaySettings.Current.Campfire.Enabled;
    internal static bool CanShowAncientEnchant() => GameVersionCompatibility.IsSupported && GameplaySettings.Current.Ancient.Enabled;
    internal static RestSiteOption CreateCampfireEnchantOption(Player owner) => new EnchantRestSiteOption(owner);
    internal static Task ApplyCampfireEnchantSuccess(Player owner) => Heal(owner, GameplaySettings.Current.Campfire.HealPercent);
    internal static Task ApplyAncientEnchantSuccess(Player owner) => Heal(owner, GameplaySettings.Current.Ancient.HealPercent);

    private static Task Heal(Player owner, decimal percent) => percent <= 0
        ? Task.CompletedTask
        : CreatureCmd.Heal(owner.Creature, owner.Creature.MaxHp * percent / 100m);
}
