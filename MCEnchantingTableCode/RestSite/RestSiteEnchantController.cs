using System.Globalization;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace MCEnchantingTable.MCEnchantingTableCode.RestSite;

internal static class RestSiteEnchantController
{
    public static bool CanEnchant(Player player) =>
        TryCreateEncounterKey(player, out string encounterKey) &&
        FindStrangeBook(player)?.HasRestSiteEnchantOpportunity(encounterKey) == true;

    public static bool IsOpportunityUsed(Player player) =>
        TryCreateEncounterKey(player, out string encounterKey) &&
        FindStrangeBook(player) is { } book &&
        !book.HasRestSiteEnchantOpportunity(encounterKey);

    public static Task<bool> CommitEnchant(Player player)
    {
        if (!TryCreateEncounterKey(player, out string encounterKey) ||
            FindStrangeBook(player)?.TryUseRestSiteEnchantOpportunity(encounterKey) != true)
        {
            MainFile.Logger.Error($"Rest Site enchant opportunity commit failed: encounter={encounterKey}.");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    internal static bool TryCreateEncounterKey(Player player, out string encounterKey)
    {
        encounterKey = string.Empty;
        if (player.RunState is not RunState runState)
        {
            return false;
        }

        int column = runState.CurrentMapCoord?.col ?? -1;
        int row = runState.CurrentMapCoord?.row ?? -1;
        encounterKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{runState.CurrentActIndex}:{column}:{row}");
        return true;
    }

    private static StrangeBook? FindStrangeBook(Player player) =>
        player.Relics.OfType<StrangeBook>().SingleOrDefault();
}
