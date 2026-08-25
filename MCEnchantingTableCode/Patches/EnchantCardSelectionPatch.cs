using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace MCEnchantingTable.MCEnchantingTableCode.UI.Enchant;

[HarmonyPatch(typeof(NDeckCardSelectScreen), "OnCardClicked")]
internal static class EnchantCardSelectionPatch
{
    internal const string SelectorNodeName = "MCEnchantingTable_EnchantCardSelector";

    private static bool Prefix(NDeckCardSelectScreen __instance, CardModel card)
    {
        if (__instance.Name != SelectorNodeName)
        {
            return true;
        }

        __instance.CardsSelectedCompletionSource().TrySetResult(new[] { card });
        NOverlayStack.Instance?.Remove(__instance);
        return false;
    }

    private static TaskCompletionSource<IEnumerable<CardModel>> CardsSelectedCompletionSource(
        this NDeckCardSelectScreen screen)
    {
        return (TaskCompletionSource<IEnumerable<CardModel>>)AccessTools
            .Field(typeof(NCardGridSelectionScreen), "_completionSource")
            .GetValue(screen)!;
    }
}
