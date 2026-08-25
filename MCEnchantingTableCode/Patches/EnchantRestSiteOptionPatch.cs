using Godot;
using HarmonyLib;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.RestSite;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MCEnchantingTable.MCEnchantingTableCode.Patches;

[HarmonyPatch]
internal static class EnchantRestSiteOptionPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
    private static void AddEnchantOption(Player player, List<RestSiteOption> __result)
    {
        if (player.Deck.Cards.Any(card => card.Enchantment is Clone)
            && __result.All(option => option.OptionId != "CLONE"))
        {
            // PaelsGrowth normally supplies this exact base-game option. Cards
            // enchanted with Clone through this mod need the same native entry
            // even when that relic is not the source of the enchantment.
            __result.Add(new CloneRestSiteOption(player));
        }

        if (__result.All(option => option.OptionId != EnchantRestSiteOption.Id))
        {
            __result.Add(new EnchantRestSiteOption(player));
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Icon), MethodType.Getter)]
    private static bool UseOriginalRestSiteIcon(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is not EnchantRestSiteOption)
        {
            return true;
        }

        string path = __instance.IsEnabled
            ? MCEnchantingTableAssets.RestSiteAssets.EnchantButtonPath
            : MCEnchantingTableAssets.RestSiteAssets.EnchantButtonDisabledPath;
        __result = PreloadManager.Cache.GetTexture2D(path);
        return false;
    }

}
