using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MCEnchantingTable.MCEnchantingTableCode.UI.Enchant;

namespace MCEnchantingTable.MCEnchantingTableCode.RestSite;

public sealed class EnchantRestSiteOption : RestSiteOption
{
    public const string Id = "MCENCHANTINGTABLE_ENCHANT";
    private const decimal EnchantHealMaxHpFraction = 0.10m;
    private readonly EnchantSession _session = new();

    public override string OptionId => Id;

    internal EnchantButtonState State => RestSiteEnchantController.IsOpportunityUsed(Owner)
        ? EnchantButtonState.AlreadyUsed
        : RestSiteEnchantController.CanEnchant(Owner) &&
          Owner.Deck.Cards.Any(MCEnchantmentConfig.CanAnyEnchant)
            ? EnchantButtonState.Available
            : EnchantButtonState.NoValidCard;

    public override bool IsEnabled => State == EnchantButtonState.Available;

    public override LocString Description => new(
        "rest_site_ui",
        $"OPTION_{OptionId}.description{(IsEnabled ? string.Empty : "Disabled")}");

    public override IEnumerable<string> AssetPaths =>
        new[]
        {
            MCEnchantingTableAssets.RestSiteAssets.EnchantButtonPath,
            MCEnchantingTableAssets.RestSiteAssets.EnchantButtonDisabledPath,
        }
            .Concat(NCardEnchantVfx.AssetPaths);

    public EnchantRestSiteOption(Player owner)
        : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        if (!RestSiteEnchantController.TryCreateEncounterKey(Owner, out string encounterKey))
        {
            return false;
        }

        _session.Configure(Owner, encounterKey);
        if (!await EnchantScreen.Show(
                Owner,
                _session,
                () => RestSiteEnchantController.CanEnchant(Owner),
                () => RestSiteEnchantController.CommitEnchant(Owner),
                HealAfterEnchant))
        {
            return false;
        }

        _session.Clear();
        return true;
    }

    private Task HealAfterEnchant()
    {
        decimal amount = Owner.Creature.MaxHp * EnchantHealMaxHpFraction;
        return CreatureCmd.Heal(Owner.Creature, amount);
    }
}
