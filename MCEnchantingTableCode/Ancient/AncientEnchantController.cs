using System.Globalization;
using BaseLib.Abstracts;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MCEnchantingTable.MCEnchantingTableCode.Networking;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MCEnchantingTable.MCEnchantingTableCode.Ancient;

internal static class AncientEnchantController
{
    public static bool BeginEnchant(AncientEventModel ancient)
    {
        Player? player = ancient.Owner;
        if (player is null || !TryCreateEncounterKey(ancient, out string encounterKey))
        {
            return false;
        }

        if (!TryApplyUse(player, encounterKey))
        {
            return false;
        }

        if (RunManager.Instance.NetService.Type.IsMultiplayer())
        {
            CustomTargetedMessageWrapper.Send(new AncientEnchantOpportunityUsedMessage
            {
                EncounterKey = encounterKey,
                AncientId = ancient.Id.ToString(),
                LocationValue = player.RunState.RunLocation,
            });
        }

        return true;
    }

    public static bool CanEnchant(AncientEventModel ancient)
    {
        Player? player = ancient.Owner;
        if (player is null || !TryCreateEncounterKey(ancient, out string encounterKey))
        {
            return false;
        }

        return FindStrangeBook(player)?.HasAncientEnchantOpportunity(encounterKey) == true;
    }

    public static bool IsOpportunityUsed(AncientEventModel ancient)
    {
        Player? player = ancient.Owner;
        if (player is null || !TryCreateEncounterKey(ancient, out string encounterKey))
        {
            return false;
        }

        StrangeBook? book = FindStrangeBook(player);
        return book is not null && !book.HasAncientEnchantOpportunity(encounterKey);
    }

    internal static void ApplyRemoteUse(ulong senderId, string ancientId, string encounterKey)
    {
        AncientEventModel? ancient = RunManager.Instance.EventSynchronizer.Events
            .OfType<AncientEventModel>()
            .FirstOrDefault(candidate =>
                candidate.Owner?.NetId == senderId &&
                string.Equals(candidate.Id.ToString(), ancientId, StringComparison.Ordinal));
        if (ancient is null ||
            ancient.Owner is not { } player ||
            !TryCreateEncounterKey(ancient, out string expectedKey) ||
            !string.Equals(expectedKey, encounterKey, StringComparison.Ordinal))
        {
            return;
        }

        TryApplyUse(player, encounterKey);
    }

    internal static bool TryCreateEncounterKey(AncientEventModel ancient, out string encounterKey)
    {
        encounterKey = string.Empty;
        if (ancient.Owner?.RunState is not RunState runState)
        {
            return false;
        }

        int column = runState.CurrentMapCoord?.col ?? -1;
        int row = runState.CurrentMapCoord?.row ?? -1;
        encounterKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{runState.CurrentActIndex}:{column}:{row}:{runState.TotalFloor}:{ancient.Id}");
        return true;
    }

    private static bool TryApplyUse(Player player, string encounterKey)
    {
        StrangeBook? strangeBook = FindStrangeBook(player);
        if (strangeBook?.TryUseAncientEnchantOpportunity(encounterKey) != true)
        {
            return false;
        }

        TaskHelper.RunSafely(SaveManager.Instance.SaveRun(null));
        return true;
    }

    private static StrangeBook? FindStrangeBook(Player player) =>
        player.Relics.OfType<StrangeBook>().SingleOrDefault();
}
