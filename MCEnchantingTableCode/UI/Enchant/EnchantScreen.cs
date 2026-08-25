using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace MCEnchantingTable.MCEnchantingTableCode.UI.Enchant;

/// <summary>
/// Phase 3A presentation and card-selection shell. It deliberately stores only a
/// local CardModel reference and never mutates the selected card.
/// </summary>
internal sealed partial class EnchantScreen : Control, IOverlayScreen
{
    private const float BookDesignWidth = 1521f;
    private const float BookDesignHeight = 1034f;
    private const float TargetBookWidth = 1280f;
    private const float TopBarReserve = 92f;
    private const float BottomUiReserve = 72f;

    private readonly Player _player;
    private readonly EnchantSession _session;
    private readonly Func<bool>? _validateOpportunity;
    private readonly Func<Task<bool>>? _commitOpportunity;
    private readonly Func<Task>? _afterEnchantApplied;
    private readonly TaskCompletionSource<bool> _completionSource = new();
    private Control _bookRoot = null!;
    private EnchantCardSlot _cardSlot = null!;
    private NBackButton _backButton = null!;
    private NConfirmButton _confirmButton = null!;
    private readonly List<EnchantOptionSlot> _optionSlots = [];
    private readonly EnchantCandidateGenerator _candidateGenerator = new();
    private CardModel? _selectedCard;
    private MCEnchantmentCandidate? _selectedCandidate;
    private bool _selectionInProgress;
    private bool _applicationInProgress;

    private EnchantScreen(
        Player player,
        EnchantSession session,
        Func<bool>? validateOpportunity,
        Func<Task<bool>>? commitOpportunity,
        Func<Task>? afterEnchantApplied)
    {
        _player = player;
        _session = session;
        _validateOpportunity = validateOpportunity;
        _commitOpportunity = commitOpportunity;
        _afterEnchantApplied = afterEnchantApplied;
        Name = "MCEnchantingTable_EnchantScreen";
    }

    public NetScreenType ScreenType => NetScreenType.CardSelection;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _cardSlot;

    public static async Task<bool> Show(
        Player player,
        EnchantSession session,
        Func<bool>? validateOpportunity = null,
        Func<Task<bool>>? commitOpportunity = null,
        Func<Task>? afterEnchantApplied = null)
    {
        if (NOverlayStack.Instance is null)
        {
            return false;
        }

        EnchantScreen screen = new(
            player,
            session,
            validateOpportunity,
            commitOpportunity,
            afterEnchantApplied);
        NOverlayStack.Instance.Push(screen);
        return await screen._completionSource.Task;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        BuildBook();
        BuildBottomPrompt();
        BuildNativeBackButton();
        BuildNativeConfirmButton();
        GetTree().Root.Connect(Viewport.SignalName.SizeChanged, Callable.From(UpdateResponsiveLayout));
        UpdateResponsiveLayout();
    }

    public override void _ExitTree()
    {
        _completionSource.TrySetResult(false);
        base._ExitTree();
    }

    private void BuildBook()
    {
        _bookRoot = new Control
        {
            Name = "BookRoot",
            Size = new Vector2(BookDesignWidth, BookDesignHeight),
            MouseFilter = MouseFilterEnum.Pass,
        };
        AddChild(_bookRoot);

        TextureRect background = new()
        {
            Name = "Background",
            Texture = MCEnchantingTableAssets.LoadTexture(
                MCEnchantingTableAssets.EnchantUiAssets.BackgroundPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _bookRoot.AddChild(background);

        _cardSlot = new EnchantCardSlot
        {
            Position = new Vector2(230f, 130f),
            Size = new Vector2(420f, 561f),
            PivotOffset = new Vector2(210f, 280.5f),
        };
        _cardSlot.SelectionRequested += BeginCardSelection;
        _cardSlot.RemovalRequested += RemoveSelectedCard;
        _bookRoot.AddChild(_cardSlot);

        Control options = new()
        {
            Name = "EnchantOptions",
            Position = new Vector2(825f, 60f),
            Size = new Vector2(500f, 724f),
            MouseFilter = MouseFilterEnum.Pass,
        };
        _bookRoot.AddChild(options);
        for (int i = 0; i < 3; i++)
        {
            EnchantOptionSlot slot = new(i + 1)
            {
                Position = new Vector2(0f, i * 225f),
                Size = new Vector2(500f, 274f),
                PivotOffset = new Vector2(250f, 137f),
            };
            slot.CandidateSelected += SelectCandidate;
            _optionSlots.Add(slot);
            options.AddChild(slot);
        }
    }

    private void BuildBottomPrompt()
    {
        Node nativeScreen = PreloadManager.Cache
            .GetScene(MCEnchantingTableAssets.EnchantUiAssets.NativeEnchantSelectionScenePath)
            .Instantiate(PackedScene.GenEditState.Disabled);
        MarginContainer bottomText = nativeScreen.GetNode<MarginContainer>("BottomText");
        nativeScreen.RemoveChild(bottomText);
        nativeScreen.Free();
        AddChild(bottomText);

        MegaRichTextLabel prompt = bottomText.GetNode<MegaRichTextLabel>("MarginContainer/BottomLabel");
        prompt.Text = GetPromptLoc().GetFormattedText();
    }

    private void BuildNativeBackButton()
    {
        _backButton = PreloadManager.Cache
            .GetScene(MCEnchantingTableAssets.EnchantUiAssets.BackButtonScenePath)
            .Instantiate<NBackButton>(PackedScene.GenEditState.Disabled);
        _backButton.Name = "BackButton";
        _backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnBackPressed));
        AddChild(_backButton);
        _backButton.Enable();

    }

    private void BuildNativeConfirmButton()
    {
        _confirmButton = PreloadManager.Cache
            .GetScene(MCEnchantingTableAssets.EnchantUiAssets.ConfirmButtonScenePath)
            .Instantiate<NConfirmButton>(PackedScene.GenEditState.Disabled);
        _confirmButton.Name = "ConfirmButton";
        _confirmButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(OnConfirmPressed));
        AddChild(_confirmButton);
        _confirmButton.Disable();
    }

    private void BeginCardSelection()
    {
        if (_selectionInProgress)
        {
            return;
        }

        TaskHelper.RunSafely(SelectCard());
    }

    private async Task SelectCard()
    {
        _selectionInProgress = true;
        try
        {
            List<CardModel> cards = PileType.Deck.GetPile(_player).Cards
                .Where(MCEnchantmentConfig.CanAnyEnchant)
                .ToList();
            if (cards.Count == 0 || NOverlayStack.Instance is null)
            {
                return;
            }

            CardSelectorPrefs prefs = new(
                GetPromptLoc(),
                1)
            {
                Cancelable = true,
                RequireManualConfirmation = false,
            };
            NDeckCardSelectScreen selector = NDeckCardSelectScreen.Create(cards, prefs);
            selector.Name = EnchantCardSelectionPatch.SelectorNodeName;
            NOverlayStack.Instance.Push(selector);
            CardModel? selected = (await selector.CardsSelected()).SingleOrDefault();
            if (selected is null || !GodotObject.IsInstanceValid(this))
            {
                return;
            }

            _selectedCard = selected;
            await _cardSlot.ShowCard(selected);
            RefreshCandidates(selected);
        }
        finally
        {
            _selectionInProgress = false;
        }
    }

    private void RemoveSelectedCard()
    {
        _selectedCard = null;
        _selectedCandidate = null;
        _cardSlot.ClearCard();
        ClearCandidates();
    }

    private void OnBackPressed(NButton _)
    {
        _selectedCard = null;
        _selectedCandidate = null;
        _cardSlot.ClearCard();
        ClearCandidates();
        _completionSource.TrySetResult(false);
        NOverlayStack.Instance?.Remove(this);
    }

    private void RefreshCandidates(CardModel card)
    {
        ClearCandidates();
        StrangeBook? book = _player.Relics.OfType<StrangeBook>().SingleOrDefault();
        int bookCount = book?.BookCount ?? 0;
        IReadOnlyList<MCEnchantmentCandidate> candidates = _session.GetOrCreateCandidates(
            card,
            rng => _candidateGenerator.Generate(
                card,
                bookCount,
                rng));

        for (int i = 0; i < candidates.Count && i < _optionSlots.Count; i++)
        {
            _optionSlots[i].SetCandidate(candidates[i]);
        }
    }

    private void ClearCandidates()
    {
        _selectedCandidate = null;
        _confirmButton?.Disable();
        foreach (EnchantOptionSlot slot in _optionSlots)
        {
            slot.ClearCandidate();
        }
    }

    private void SelectCandidate(MCEnchantmentCandidate candidate)
    {
        if (_applicationInProgress)
        {
            return;
        }

        _selectedCandidate = candidate;
        foreach (EnchantOptionSlot slot in _optionSlots)
        {
            slot.SetSelected(ReferenceEquals(slot.Candidate, candidate));
        }
        _confirmButton.Enable();
    }

    private void OnConfirmPressed(NButton _)
    {
        if (_applicationInProgress || _selectedCard is null || _selectedCandidate is null)
        {
            return;
        }

        TaskHelper.RunSafely(ConfirmEnchant());
    }

    private async Task ConfirmEnchant()
    {
        CardModel card = _selectedCard!;
        MCEnchantmentCandidate candidate = _selectedCandidate!;
        _applicationInProgress = true;
        _confirmButton.Disable();
        _backButton.Disable();
        foreach (EnchantOptionSlot slot in _optionSlots)
        {
            slot.Disabled = true;
        }

        if (!await EnchantController.TryApplyCardEnchant(
                card,
                candidate,
                _validateOpportunity,
                _commitOpportunity,
                _afterEnchantApplied))
        {
            _selectedCandidate = null;
            foreach (EnchantOptionSlot slot in _optionSlots)
            {
                slot.Disabled = slot.Candidate is null;
                slot.SetSelected(false);
            }
            _applicationInProgress = false;
            _backButton.Enable();
            return;
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _completionSource.TrySetResult(true);
        NOverlayStack.Instance?.Remove(this);
    }

    private void UpdateResponsiveLayout()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        float availableHeight = Mathf.Max(
            1f,
            viewportSize.Y - TopBarReserve - BottomUiReserve);
        float scale = Mathf.Min(
            1f,
            Mathf.Min(
                Mathf.Min(TargetBookWidth, viewportSize.X - 40f) / BookDesignWidth,
                availableHeight / BookDesignHeight));
        _bookRoot.Scale = Vector2.One * scale;
        Vector2 scaledSize = new(BookDesignWidth * scale, BookDesignHeight * scale);
        _bookRoot.Position = new Vector2(
            (viewportSize.X - scaledSize.X) * 0.5f,
            TopBarReserve + (availableHeight - scaledSize.Y) * 0.5f);
    }

    private static LocString GetPromptLoc()
    {
        LocString prompt = CardSelectorPrefs.EnchantSelectionPrompt;
        prompt.Add("Amount", 1);
        return prompt;
    }

    public void AfterOverlayOpened()
    {
    }

    public void AfterOverlayClosed()
    {
        this.QueueFreeSafely();
    }

    public void AfterOverlayShown()
    {
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Enabled;
        _backButton?.Enable();
        if (_selectedCandidate is not null && !_applicationInProgress)
        {
            _confirmButton?.Enable();
        }
    }

    public void AfterOverlayHidden()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Disabled;
        _backButton?.Disable();
        _confirmButton?.Disable();
    }
}
