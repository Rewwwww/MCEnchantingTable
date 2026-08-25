using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace MCEnchantingTable.MCEnchantingTableCode.UI.Enchant;

internal sealed partial class EnchantCardSlot : Button
{
    private const string HoverSound = "event:/sfx/ui/clicks/ui_hover";
    private const string ClickSound = "event:/sfx/ui/clicks/ui_click";
    private const float MaxCardScale = 1.2f;
    private static readonly Vector2 HoverScale = Vector2.One * 1.05f;
    private static readonly Vector2 PressScale = Vector2.One * 0.97f;

    private readonly TextureRect _slotImage;
    private readonly TextureRect _emptyPlus;
    private readonly Control _cardContainer;
    private NCard? _cardNode;
    private Tween? _interactionTween;

    public event Action? SelectionRequested;
    public event Action? RemovalRequested;

    public EnchantCardSlot()
    {
        Name = "CardSlot";
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;

        StyleBoxEmpty empty = new();
        foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
        {
            AddThemeStyleboxOverride(state, empty);
        }

        _slotImage = new TextureRect
        {
            Name = "SlotImage",
            Texture = MCEnchantingTableAssets.LoadTexture(
                MCEnchantingTableAssets.EnchantUiAssets.CardSlotPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _slotImage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_slotImage);

        _emptyPlus = new TextureRect
        {
            Name = "EmptyPlus",
            Texture = MCEnchantingTableAssets.LoadTexture(
                MCEnchantingTableAssets.EnchantUiAssets.CardSlotPlusPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(96f, 96f),
        };
        _emptyPlus.SetAnchorsPreset(LayoutPreset.Center);
        _emptyPlus.Position = -_emptyPlus.CustomMinimumSize * 0.5f;
        _emptyPlus.Size = _emptyPlus.CustomMinimumSize;
        AddChild(_emptyPlus);

        _cardContainer = new Control
        {
            Name = "SelectedCard",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _cardContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_cardContainer);

        Pressed += OnPrimaryPressed;
        MouseEntered += OnHovered;
        MouseExited += OnUnhovered;
        ButtonDown += OnButtonDown;
    }

    public bool HasCard => _cardNode is not null;

    public override void _ExitTree()
    {
        _interactionTween?.Kill();
        ClearCard();
        Pressed -= OnPrimaryPressed;
        MouseEntered -= OnHovered;
        MouseExited -= OnUnhovered;
        ButtonDown -= OnButtonDown;
        base._ExitTree();
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Right,
                Pressed: false,
            } && HasCard)
        {
            AcceptEvent();
            PlayRemovalFeedback();
            RemovalRequested?.Invoke();
        }
    }

    public async Task ShowCard(CardModel card)
    {
        ClearCard();
        _cardNode = NCard.Create(card);
        if (_cardNode is null)
        {
            return;
        }

        _cardNode.MouseFilter = MouseFilterEnum.Ignore;
        _cardContainer.AddChildSafely(_cardNode);
        _emptyPlus.Visible = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        FitCardToSlot();
        _cardNode.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
    }

    public void ClearCard()
    {
        if (_cardNode is null || !GodotObject.IsInstanceValid(_cardNode))
        {
            _cardNode = null;
            _emptyPlus.Visible = true;
            return;
        }

        _cardNode.QueueFreeSafely();
        _cardNode = null;
        _emptyPlus.Visible = true;
    }

    private void FitCardToSlot()
    {
        if (_cardNode is null)
        {
            return;
        }

        Vector2 available = _cardContainer.Size * 0.94f;
        float scale = Mathf.Min(
            MaxCardScale,
            Mathf.Min(available.X / NCard.defaultSize.X, available.Y / NCard.defaultSize.Y));
        _cardNode.Scale = Vector2.One * scale;
        // NCard's artwork is centered around the node origin (roughly -150..150,
        // -211..211), so its origin belongs at the slot center.
        _cardNode.Position = _cardContainer.Size * 0.5f;
    }

    private void OnPrimaryPressed()
    {
        SelectionRequested?.Invoke();
    }

    private void OnHovered()
    {
        SfxCmd.Play(HoverSound);
        Animate(HoverScale, new Color(1.15f, 1.15f, 1.15f, 1f), 0.05);
    }

    private void OnUnhovered()
    {
        Animate(Vector2.One, Colors.White, 0.35);
    }

    private void OnButtonDown()
    {
        SfxCmd.Play(ClickSound);
        Animate(PressScale, Colors.Gray, 0.12);
    }

    private void PlayRemovalFeedback()
    {
        SfxCmd.Play(ClickSound);
        _interactionTween?.Kill();
        _interactionTween = CreateTween();
        _interactionTween.SetParallel();
        _interactionTween.TweenProperty(this, "scale", PressScale, 0.08)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _interactionTween.TweenProperty(_slotImage, "modulate", Colors.Gray, 0.08)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _interactionTween.Chain();
        _interactionTween.TweenProperty(this, "scale", HoverScale, 0.18)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _interactionTween.TweenProperty(_slotImage, "modulate", Colors.White, 0.18)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    private void Animate(Vector2 scale, Color color, double duration)
    {
        _interactionTween?.Kill();
        _interactionTween = CreateTween().SetParallel();
        _interactionTween.TweenProperty(this, "scale", scale, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _interactionTween.TweenProperty(_slotImage, "modulate", color, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }
}
