using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MCEnchantingTable.MCEnchantingTableCode.UI;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MCEnchantingTable.MCEnchantingTableCode.Rewards;

public sealed class BookReward : CustomReward
{
    private Control? _selectionRewardButton;
    private Control? _selectionIconContainer;

    [CustomEnum]
    public static RewardType BookRewardType;

    public int Amount { get; }

    protected override RewardType RewardType => BookRewardType;

    protected override string IconPath => MCEnchantingTableAssets.RelicAssets.StrangeBookBigIconPath;

    public override LocString Description
    {
        get
        {
            LocString description = GetLoc();
            description.Add("Amount", Amount);
            return description;
        }
    }

    public override bool IsPopulated => true;

    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

    public BookReward(int amount, Player player)
        : base(player)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "A book reward must contain at least one book.");
        }

        Amount = amount;
    }

    public override void Populate()
    {
    }

    public override void MarkContentAsSeen()
    {
    }

    public override SerializableReward ToSerializable()
    {
        return new SerializableReward
        {
            RewardType = RewardType,
            GoldAmount = Amount,
        };
    }

    public static BookReward CreateFromSerializable(SerializableReward save, Player player)
    {
        return new BookReward(save.GoldAmount, player);
    }

    internal void SetSelectionVisuals(Control rewardButton, Control iconContainer)
    {
        _selectionRewardButton = rewardButton;
        _selectionIconContainer = iconContainer;
    }

    protected override async Task<bool> OnSelect()
    {
        StrangeBook? strangeBook = Player.Relics.OfType<StrangeBook>().SingleOrDefault();
        if (strangeBook is null)
        {
            return false;
        }

        if (LocalContext.IsMe(Player) &&
            _selectionRewardButton is { } rewardButton &&
            GodotObject.IsInstanceValid(rewardButton) &&
            _selectionIconContainer is { } iconContainer &&
            GodotObject.IsInstanceValid(iconContainer))
        {
            await BookRewardRelicAnimation.Play(strangeBook, rewardButton, iconContainer);
        }

        strangeBook.AddBooks(Amount);
        if (LocalContext.IsMe(Player))
        {
            NDebugAudioManager.Instance?.Play("relic_get.mp3");
        }

        return true;
    }
}
