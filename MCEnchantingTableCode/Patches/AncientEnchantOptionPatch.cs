using System.Collections;
using System.Reflection;
using Godot;
using HarmonyLib;
using MCEnchantingTable.MCEnchantingTableCode.UI;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace MCEnchantingTable.MCEnchantingTableCode.Patches;

[HarmonyPatch]
internal static class AncientEnchantOptionPatch
{
    private static readonly FieldInfo DialogueField =
        AccessTools.Field(typeof(NAncientEventLayout), "_dialogue");

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetEvent))]
    private static void AddOption(NEventLayout __instance, EventModel eventModel)
    {
        if (__instance is not NAncientEventLayout ancientLayout ||
            eventModel is not AncientEventModel ancient ||
            !LocalContext.IsMe(ancient.Owner) ||
            ancientLayout.GetNodeOrNull<AncientEnchantOption>(AncientEnchantOption.NodeName) is not null)
        {
            return;
        }

        ancientLayout.AddChild(AncientEnchantOption.Create(ancient));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NAncientEventLayout), "SetDialogueLineAndAnimate")]
    private static void EnableAfterFinalDialogueLine(NAncientEventLayout __instance, int lineIndex)
    {
        if (__instance.GetNodeOrNull<AncientEnchantOption>(AncientEnchantOption.NodeName) is not { } option ||
            DialogueField.GetValue(__instance) is not ICollection dialogue)
        {
            return;
        }

        option.OnDialogueLineChanged(lineIndex, dialogue.Count);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NAncientEventLayout), nameof(NAncientEventLayout.ClearDialogue))]
    private static void HideWhenAncientStateChanges(NAncientEventLayout __instance)
    {
        __instance.GetNodeOrNull<AncientEnchantOption>(AncientEnchantOption.NodeName)
            ?.HideForEventStateChange();
    }
}
