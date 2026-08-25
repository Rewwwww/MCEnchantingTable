using System.Reflection;
using Godot;
using HarmonyLib;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace MCEnchantingTable.MCEnchantingTableCode.UI;

internal static class BookRewardRelicAnimation
{
    private static readonly FieldInfo ObtainedTweenField =
        AccessTools.Field(typeof(NRelicInventoryHolder), "_obtainedTween");

    public static async Task Play(
        StrangeBook strangeBook,
        Control rewardButton,
        Control rewardIconContainer)
    {
        NRun? run = NRun.Instance;
        NRelicInventory? inventory = run?.GlobalUi.RelicInventory;
        NRelicInventoryHolder? target = inventory?.RelicNodes
            .FirstOrDefault(node => ReferenceEquals(node.Relic.Model, strangeBook));
        NRelicInventoryHolder? temporary = NRelicInventoryHolder.Create(strangeBook);
        if (run is null || target is null || temporary is null)
        {
            return;
        }

        temporary.Visible = false;
        temporary.MouseFilter = Control.MouseFilterEnum.Ignore;
        run.GlobalUi.AboveTopBarVfxContainer.AddChildSafely(temporary);
        temporary.GlobalPosition = target.GlobalPosition;
        temporary.Size = target.Size;

        await temporary.AwaitProcessFrame();
        temporary.GetNodeOrNull<Control>("%AmountLabel")?.Hide();
        if (temporary.FindChild("StrangeBookProgressOverlay", recursive: true, owned: false) is CanvasItem overlay)
        {
            overlay.Hide();
        }

        await temporary.PlayNewlyAcquiredAnimation(
            rewardIconContainer.GlobalPosition,
            startScale: null);

        // The real reward remains in the synchronized set until OnSelect completes, but
        // remove its entire row from the container layout as soon as the fly animation starts.
        // NRewardsScreen will still perform the authoritative removal through RewardClaimed.
        rewardButton.Visible = false;
        temporary.Visible = true;

        if (ObtainedTweenField.GetValue(temporary) is Tween tween && tween.IsValid())
        {
            await temporary.ToSignal(tween, Tween.SignalName.Finished);
        }

        temporary.QueueFreeSafely();
    }
}
