using System.Reflection;
using Godot;
using HarmonyLib;
using MCEnchantingTable.MCEnchantingTableCode.Rewards;
using MegaCrit.Sts2.Core.Nodes.Rewards;

namespace MCEnchantingTable.MCEnchantingTableCode.Patches;

[HarmonyPatch]
internal static class BookRewardAnimationPatch
{
    private static readonly FieldInfo IconContainerField =
        AccessTools.Field(typeof(NRewardButton), "_iconContainer");

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NRewardButton), "GetReward")]
    private static void CaptureRewardIconPosition(NRewardButton __instance)
    {
        if (__instance.Reward is BookReward reward &&
            IconContainerField.GetValue(__instance) is Control iconContainer)
        {
            reward.SetSelectionVisuals(__instance, iconContainer);
        }
    }

}
