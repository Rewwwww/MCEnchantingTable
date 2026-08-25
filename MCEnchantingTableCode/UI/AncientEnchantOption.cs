using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Ancient;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MCEnchantingTable.MCEnchantingTableCode.UI.Enchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace MCEnchantingTable.MCEnchantingTableCode.UI;

/// <summary>
/// Ancient-only enchanting entry that lives inside the Ancient event layout.
/// Its position is a fixed relative anchor and does not depend on native event options.
/// </summary>
internal sealed partial class AncientEnchantOption : Button
{
    public const string NodeName = "MCEnchantingTable_AncientEnchantOption";

    private const float ButtonSize = 132f;
    private const float FixedAnchorX = 0.82f;
    private const float FixedAnchorY = 0.5f;
    private const float BaseVisualScale = 1.2f;
    private const float HoverScale = 1.01f;
    private const float PressScale = 0.99f;
    private const double HoverDuration = 0.05;
    private const double ReturnDuration = 0.5;
    private const float EntranceOffsetY = 60f;
    private const double ContentEntranceDuration = 1.0;
    private const string HoverSound = "event:/sfx/ui/clicks/ui_hover";
    private const string ClickSound = "event:/sfx/ui/clicks/ui_click";

    private AncientEventModel _ancient = null!;
    private StrangeBook? _strangeBook;
    private readonly EnchantSession _session = new();
    private Control? _visuals;
    private TextureRect? _image;
    private Tween? _interactionTween;
    private Tween? _entranceTween;
    private bool _dialogueReady;
    private bool _hasAnimatedIn;
    private EnchantButtonState _state = EnchantButtonState.Available;

    public static AncientEnchantOption Create(AncientEventModel ancient) => new()
    {
        Name = NodeName,
        _ancient = ancient,
    };

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(ButtonSize, ButtonSize);
        SetFixedLayoutPosition();
        PivotOffset = Size * 0.5f;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;

        ApplyTransparentStyles();
        AddVisuals();
        Pressed += OnClicked;
        MouseEntered += OnHovered;
        MouseExited += OnUnhovered;
        ButtonDown += OnPressed;

        Disabled = false;
        Visible = false;

        _strangeBook = _ancient.Owner?.Relics.OfType<StrangeBook>().SingleOrDefault();
        if (_strangeBook is not null)
        {
            _strangeBook.AncientEnchantOpportunityChanged += RefreshState;
        }
    }

    public override void _ExitTree()
    {
        _entranceTween?.Kill();
        _interactionTween?.Kill();
        if (_strangeBook is not null)
        {
            _strangeBook.AncientEnchantOpportunityChanged -= RefreshState;
        }
        Pressed -= OnClicked;
        MouseEntered -= OnHovered;
        MouseExited -= OnUnhovered;
        ButtonDown -= OnPressed;
        base._ExitTree();
    }

    public void OnDialogueLineChanged(int lineIndex, int dialogueLineCount)
    {
        if (dialogueLineCount <= 0 || lineIndex < dialogueLineCount - 1)
        {
            return;
        }

        _dialogueReady = true;
        RefreshState();
    }

    public void HideForEventStateChange()
    {
        // ClearDialogue also runs when a native Ancient option advances the
        // encounter. Once this independent entry has been revealed, native
        // option transitions must not consume or hide it.
        RefreshState();
    }

    private void SetFixedLayoutPosition()
    {
        AnchorLeft = FixedAnchorX;
        AnchorRight = FixedAnchorX;
        AnchorTop = FixedAnchorY;
        AnchorBottom = FixedAnchorY;
        OffsetLeft = -ButtonSize * 0.5f;
        OffsetTop = -ButtonSize * 0.5f;
        OffsetRight = ButtonSize * 0.5f;
        OffsetBottom = ButtonSize * 0.5f;
    }

    private void AddVisuals()
    {
        _visuals = new Control
        {
            Name = "Visuals",
            MouseFilter = MouseFilterEnum.Ignore,
            PivotOffset = Size * 0.5f,
            Scale = Vector2.One * BaseVisualScale,
        };
        _visuals.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_visuals);

        _image = new TextureRect
        {
            Name = "Icon",
            Texture = MCEnchantingTableAssets.LoadTexture(MCEnchantingTableAssets.AncientAssets.EnchantButtonPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _visuals.AddChild(_image);
    }

    private void ApplyTransparentStyles()
    {
        StyleBoxEmpty empty = new();
        AddThemeStyleboxOverride("normal", empty);
        AddThemeStyleboxOverride("hover", empty);
        AddThemeStyleboxOverride("pressed", empty);
        AddThemeStyleboxOverride("focus", empty);
        AddThemeStyleboxOverride("disabled", empty);
    }

    private void RefreshState()
    {
        _state = GetCurrentState();

        bool shouldBeVisible = _dialogueReady;
        Visible = shouldBeVisible;
        Disabled = false;
        FocusMode = FocusModeEnum.All;

        if (_image is not null)
        {
            string iconPath = _state == EnchantButtonState.Available
                ? MCEnchantingTableAssets.AncientAssets.EnchantButtonPath
                : MCEnchantingTableAssets.AncientAssets.EnchantButtonDisabledPath;
            _image.Texture = MCEnchantingTableAssets.LoadTexture(iconPath);
        }

        if (shouldBeVisible && !_hasAnimatedIn)
        {
            AnimateIn();
        }
    }

    private EnchantButtonState GetCurrentState()
    {
        if (AncientEnchantController.IsOpportunityUsed(_ancient))
        {
            return EnchantButtonState.AlreadyUsed;
        }

        bool hasOpportunity = AncientEnchantController.CanEnchant(_ancient);
        bool hasValidCard = _ancient.Owner?.Deck.Cards.Any(MCEnchantmentConfig.CanAnyEnchant) == true;
        return hasOpportunity && hasValidCard
            ? EnchantButtonState.Available
            : EnchantButtonState.NoValidCard;
    }

    private void AnimateIn()
    {
        _hasAnimatedIn = true;
        if (_visuals is null)
        {
            return;
        }

        Vector2 finalVisualPosition = Vector2.Zero;

        if (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
        {
            _visuals.Position = finalVisualPosition;
            _visuals.Modulate = Colors.White;
            MouseFilter = MouseFilterEnum.Stop;
            return;
        }

        _visuals.Position = finalVisualPosition + new Vector2(0f, EntranceOffsetY);
        _visuals.Modulate = StsColors.transparentWhite;
        MouseFilter = MouseFilterEnum.Ignore;

        _entranceTween?.Kill();
        _entranceTween = CreateTween().SetParallel();
        _entranceTween.TweenProperty(_visuals, "position", finalVisualPosition, ContentEntranceDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _entranceTween.TweenProperty(_visuals, "modulate", Colors.White, ContentEntranceDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _entranceTween.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(this) && _visuals is not null)
            {
                _visuals.Position = finalVisualPosition;
                _visuals.Modulate = Colors.White;
                MouseFilter = MouseFilterEnum.Stop;
            }
        };
    }

    private void OnHovered()
    {
        SfxCmd.Play(HoverSound);
        AnimateVisuals(BaseVisualScale * HoverScale, new Color(1.2f, 1.2f, 1.2f, 1f), HoverDuration, false);
    }

    private void OnUnhovered()
    {
        AnimateVisuals(BaseVisualScale, Colors.White, ReturnDuration, true);
    }

    private void OnPressed()
    {
        RefreshState();
        if (_state != EnchantButtonState.Available)
        {
            return;
        }

        SfxCmd.Play(ClickSound);
        AnimateVisuals(BaseVisualScale * PressScale, Colors.White, ReturnDuration, true);
    }

    private void OnClicked()
    {
        RefreshState();
        if (_state != EnchantButtonState.Available)
        {
            return;
        }

        TaskHelper.RunSafely(OpenEnchantScreen());
    }

    private async Task OpenEnchantScreen()
    {
        if (_ancient.Owner is not { } player)
        {
            RefreshState();
            return;
        }

        bool opportunityConsumed = false;
        bool confirmed = await EnchantScreen.Show(
            player,
            _session,
            () =>
            {
                opportunityConsumed = EnchantController.BeginAncientEnchant(_ancient);
                RefreshState();
                return Task.CompletedTask;
            });
        if (confirmed && opportunityConsumed)
        {
            _session.Clear();
        }

        RefreshState();
    }

    private void AnimateVisuals(float targetScale, Color targetModulate, double duration, bool useExpoEase)
    {
        if (_visuals is null || _image is null)
        {
            return;
        }

        _interactionTween?.Kill();
        _interactionTween = CreateTween().SetParallel();
        PropertyTweener scaleTween = _interactionTween.TweenProperty(
            _visuals,
            "scale",
            Vector2.One * targetScale,
            duration);
        PropertyTweener colorTween = _interactionTween.TweenProperty(
            _image,
            "modulate",
            targetModulate,
            duration);

        if (useExpoEase)
        {
            scaleTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
            colorTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        }
    }
}
