using HarmonyLib;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MCEnchantingTable.MCEnchantingTableCode.Rules;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.Patches;

/// <summary>
/// Adds one independent StrangeBook instance to every newly-created player.
/// Player.FromSerializable uses a separate load path, so loading a save does not add a duplicate.
/// </summary>
[HarmonyPatch(typeof(Player), "PopulateStartingRelics")]
internal static class PlayerStartingRelicsPatch
{
    [HarmonyPostfix]
    private static void AddStrangeBook(Player __instance)
    {
        StrangeBook strangeBook = (StrangeBook)ModelDb.Relic<StrangeBook>().ToMutable();
        strangeBook.ApplyRulesSnapshot(BookRulesSession.ForNewRun());
        __instance.AddRelicInternal(strangeBook);
    }
}
