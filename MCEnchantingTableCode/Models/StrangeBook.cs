using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using BaseLib.Utils;
using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.Rules;
using MCEnchantingTable.MCEnchantingTableCode.Rewards;
using MCEnchantingTable.MCEnchantingTableCode.UI;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rewards;

namespace MCEnchantingTable.MCEnchantingTableCode.Models;

/// <summary>
/// Per-player state carrier for the enchanting-table system.
/// This relic is registered in the event pool only so it has a valid BaseLib relic pool;
/// normal relic rewards do not draw from that pool.
/// </summary>
[Pool(typeof(EventRelicPool))]
public sealed class StrangeBook : CustomRelicModel, ICustomUiModel
{
    private int _bookCount;

    private int _normalCombatProgress;

    private string _lastAncientEnchantEncounterKey = string.Empty;

    private string _lastRestSiteEnchantEncounterKey = string.Empty;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BookCount;

    public override string PackedIconPath => MCEnchantingTableAssets.RelicAssets.StrangeBookPackedIconPath;

    protected override string PackedIconOutlinePath => MCEnchantingTableAssets.RelicAssets.StrangeBookPackedIconPath;

    protected override string BigIconPath => MCEnchantingTableAssets.RelicAssets.StrangeBookBigIconPath;

    public override List<(string, string)>? Localization => null;

    public override bool IsAllowed(IRunState runState) => false;

    [SavedProperty]
    public int BookCount
    {
        get => _bookCount;
        private set
        {
            AssertMutable();
            _bookCount = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public int NormalCombatProgress
    {
        get => _normalCombatProgress;
        private set
        {
            AssertMutable();
            if (_normalCombatProgress == value)
            {
                return;
            }

            _normalCombatProgress = value;
            ProgressDisplayChanged?.Invoke();
        }
    }

    public event Action? ProgressDisplayChanged;

    public event Action? AncientEnchantOpportunityChanged;

    public event Action? RestSiteEnchantOpportunityChanged;

    [SavedProperty]
    public string LastAncientEnchantEncounterKey
    {
        get => _lastAncientEnchantEncounterKey;
        private set
        {
            AssertMutable();
            if (string.Equals(_lastAncientEnchantEncounterKey, value, StringComparison.Ordinal))
            {
                return;
            }

            _lastAncientEnchantEncounterKey = value;
            AncientEnchantOpportunityChanged?.Invoke();
        }
    }

    [SavedProperty]
    public string LastRestSiteEnchantEncounterKey
    {
        get => _lastRestSiteEnchantEncounterKey;
        private set
        {
            AssertMutable();
            if (string.Equals(_lastRestSiteEnchantEncounterKey, value, StringComparison.Ordinal))
            {
                return;
            }

            _lastRestSiteEnchantEncounterKey = value;
            RestSiteEnchantOpportunityChanged?.Invoke();
        }
    }

    [SavedProperty] public int Act1NormalCombatsPerReward { get; private set; } = 1;
    [SavedProperty] public int Act2NormalCombatsPerReward { get; private set; } = 2;
    [SavedProperty] public int Act3NormalCombatsPerReward { get; private set; } = 3;
    [SavedProperty] public int NormalCombatBookRewardAmount { get; private set; } = 1;
    [SavedProperty] public int EliteBookRewardAmount { get; private set; } = 1;
    [SavedProperty] public int BossBookRewardAmount { get; private set; } = 2;

    public BookRulesSnapshot Rules => new BookRulesSnapshot(
        Act1NormalCombatsPerReward,
        Act2NormalCombatsPerReward,
        Act3NormalCombatsPerReward,
        NormalCombatBookRewardAmount,
        EliteBookRewardAmount,
        BossBookRewardAmount).Sanitized();

    public void ApplyRulesSnapshot(BookRulesSnapshot snapshot)
    {
        AssertMutable();
        snapshot = snapshot.Sanitized();
        Act1NormalCombatsPerReward = snapshot.Act1NormalCombatsPerReward;
        Act2NormalCombatsPerReward = snapshot.Act2NormalCombatsPerReward;
        Act3NormalCombatsPerReward = snapshot.Act3NormalCombatsPerReward;
        NormalCombatBookRewardAmount = snapshot.NormalCombatBookRewardAmount;
        EliteBookRewardAmount = snapshot.EliteBookRewardAmount;
        BossBookRewardAmount = snapshot.BossBookRewardAmount;
        ProgressDisplayChanged?.Invoke();
    }

    public void CreateCustomUi(Control toAdd)
    {
        toAdd.MouseFilter = Control.MouseFilterEnum.Ignore;
        toAdd.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        toAdd.AddChild(new StrangeBookProgressOverlay(this));
    }

    public bool TryGetProgressDisplay(out int current, out int required)
    {
        current = 0;
        required = 0;

        if (!IsMutable || Owner.RunState is NullRunState)
        {
            return false;
        }

        current = NormalCombatProgress;
        required = Rules.GetNormalCombatsPerReward(Owner.RunState.CurrentActIndex);
        return true;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        int rewardAmount = CombatBookRewardCalculator.Calculate(this, room);
        if (rewardAmount > 0)
        {
            room.AddExtraReward(Owner, new BookReward(rewardAmount, Owner));
        }

        return Task.CompletedTask;
    }

    public void AddBooks(int amount)
    {
        AssertMutable();

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Book rewards cannot be negative.");
        }

        if (amount == 0)
        {
            return;
        }

        BookCount += amount;
    }

    public bool HasAncientEnchantOpportunity(string encounterKey) =>
        !string.IsNullOrEmpty(encounterKey) &&
        !string.Equals(LastAncientEnchantEncounterKey, encounterKey, StringComparison.Ordinal);

    public bool TryUseAncientEnchantOpportunity(string encounterKey)
    {
        AssertMutable();
        if (!HasAncientEnchantOpportunity(encounterKey))
        {
            return false;
        }

        LastAncientEnchantEncounterKey = encounterKey;
        return true;
    }

    public bool HasRestSiteEnchantOpportunity(string encounterKey) =>
        !string.IsNullOrEmpty(encounterKey) &&
        !string.Equals(LastRestSiteEnchantEncounterKey, encounterKey, StringComparison.Ordinal);

    public bool TryUseRestSiteEnchantOpportunity(string encounterKey)
    {
        AssertMutable();
        if (!HasRestSiteEnchantOpportunity(encounterKey))
        {
            return false;
        }

        LastRestSiteEnchantEncounterKey = encounterKey;
        return true;
    }

    public int AdvanceNormalCombatProgress(int actIndex)
    {
        AssertMutable();

        BookRulesSnapshot rules = Rules;
        (bool rewardEarned, int nextProgress) = BookProgressionRules.AdvanceNormalCombat(
            NormalCombatProgress,
            rules.GetNormalCombatsPerReward(actIndex));
        NormalCombatProgress = nextProgress;
        return rewardEarned ? rules.NormalCombatBookRewardAmount : 0;
    }

    public int ConsumeActRemainderReward()
    {
        AssertMutable();

        if (NormalCombatProgress <= 0)
        {
            return 0;
        }

        NormalCombatProgress = 0;
        return Rules.NormalCombatBookRewardAmount;
    }
}
