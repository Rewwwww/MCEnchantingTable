using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.UI.Enchant;

/// <summary>
/// Displays one generated candidate without applying it to a card.
/// </summary>
internal sealed partial class EnchantOptionSlot : Button
{
    private const string HoverSound = "event:/sfx/ui/clicks/ui_hover";
    private const string ClickSound = "event:/sfx/ui/clicks/ui_click";
    private static readonly Vector2 HoverScale = Vector2.One * 1.05f;
    private static readonly Color EmptyEntryColor = new(0.34f, 0.25f, 0.16f, 0.18f);
    private static readonly Color NormalEntryColor = new(0.28f, 0.17f, 0.12f, 0.88f);
    private static readonly Color HoverEntryColor = new(0.39f, 0.24f, 0.31f, 0.96f);
    private static readonly Color SelectedEntryColor = new(0.31f, 0.20f, 0.42f, 0.98f);
    private static readonly Color HighlightPurple = new(0.52f, 0.30f, 0.72f, 1f);
    private readonly TextureRect _image;
    private readonly Panel _candidateBackground;
    private readonly Panel _highlightBorder;
    private readonly Control _candidateContent;
    private readonly TextureRect _icon;
    private readonly VBoxContainer _textContainer;
    private readonly HBoxContainer _titleLine;
    private readonly Label _title;
    private readonly Label _level;
    private readonly MegaRichTextLabel _description;
    private Tween? _hoverTween;
    private bool _hovered;
    private bool _selected;

    public event Action<MCEnchantmentCandidate>? CandidateSelected;

    public MCEnchantmentCandidate? Candidate { get; private set; }

    public EnchantOptionSlot(int index)
    {
        Name = $"OptionSlot{index}";
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;

        StyleBoxEmpty empty = new();
        foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
        {
            AddThemeStyleboxOverride(state, empty);
        }

        _image = new TextureRect
        {
            Name = "SlotImage",
            Texture = MCEnchantingTableAssets.LoadTexture(
                MCEnchantingTableAssets.EnchantUiAssets.OptionSlotPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_image);

        StyleBoxFlat candidateStyle = new()
        {
            BgColor = Colors.White,
            BorderColor = new Color(0.42f, 0.27f, 0.18f, 0.95f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
        };
        _candidateBackground = new Panel
        {
            Name = "BackgroundSlot",
            MouseFilter = MouseFilterEnum.Ignore,
            SelfModulate = EmptyEntryColor,
        };
        _candidateBackground.AddThemeStyleboxOverride("panel", candidateStyle);
        _candidateBackground.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // Matches the visible bounds of the empty-slot artwork. This node is
        // permanent; EmptyState and CandidateState are foreground layers.
        _candidateBackground.OffsetLeft = 28f;
        _candidateBackground.OffsetTop = 49f;
        _candidateBackground.OffsetRight = -28f;
        _candidateBackground.OffsetBottom = -88f;
        AddChild(_candidateBackground);

        // The supplied slot texture contains the question-mark empty state.
        // Keep it as a foreground layer instead of treating it as the card
        // background so candidate content can use the same permanent frame.
        MoveChild(_candidateBackground, 0);

        StyleBoxFlat highlightStyle = new()
        {
            BgColor = Colors.Transparent,
            BorderColor = HighlightPurple,
            BorderWidthLeft = 5,
            BorderWidthTop = 5,
            BorderWidthRight = 5,
            BorderWidthBottom = 5,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
        };
        _highlightBorder = new Panel
        {
            Name = "HighlightBorder",
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        _highlightBorder.AddThemeStyleboxOverride("panel", highlightStyle);
        _highlightBorder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _highlightBorder.OffsetLeft = 28f;
        _highlightBorder.OffsetTop = 49f;
        _highlightBorder.OffsetRight = -28f;
        _highlightBorder.OffsetBottom = -88f;
        AddChild(_highlightBorder);

        _candidateContent = new MarginContainer
        {
            Name = "CandidateState",
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        _candidateContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _candidateContent.OffsetLeft = 42f;
        _candidateContent.OffsetTop = 57f;
        _candidateContent.OffsetRight = -42f;
        _candidateContent.OffsetBottom = -96f;
        AddChild(_candidateContent);

        HBoxContainer contentRoot = new()
        {
            Name = "ContentRoot",
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        contentRoot.AddThemeConstantOverride("separation", 14);
        _candidateContent.AddChild(contentRoot);

        CenterContainer iconContainer = new()
        {
            Name = "IconContainer",
            CustomMinimumSize = new Vector2(72f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        contentRoot.AddChild(iconContainer);

        _icon = new TextureRect
        {
            Name = "Icon",
            CustomMinimumSize = new Vector2(58f, 58f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        iconContainer.AddChild(_icon);

        Font? regularFont = ResourceLoader.Load<Font>(
            MCEnchantingTableAssets.EnchantUiAssets.PromptRegularFontPath);

        _textContainer = new VBoxContainer
        {
            Name = "TextContainer",
            CustomMinimumSize = new Vector2(0f, 82f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _textContainer.AddThemeConstantOverride("separation", 4);
        contentRoot.AddChild(_textContainer);

        _titleLine = new HBoxContainer
        {
            Name = "TitleLine",
            CustomMinimumSize = new Vector2(0f, 28f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Begin,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _titleLine.AddThemeConstantOverride("separation", 6);
        _textContainer.AddChild(_titleLine);

        _title = new Label
        {
            Name = "Title",
            CustomMinimumSize = new Vector2(0f, 28f),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _title.AddThemeFontSizeOverride("font_size", 22);
        if (regularFont is not null)
        {
            _title.AddThemeFontOverride("font", regularFont);
        }
        _titleLine.AddChild(_title);

        _level = new Label
        {
            Name = "Level",
            CustomMinimumSize = new Vector2(30f, 28f),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _level.AddThemeFontSizeOverride("font_size", 22);
        if (regularFont is not null)
        {
            _level.AddThemeFontOverride("font", regularFont);
        }
        _titleLine.AddChild(_level);

        _description = new MegaRichTextLabel
        {
            Name = "DescriptionLine",
            CustomMinimumSize = new Vector2(0f, 48f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            BbcodeEnabled = true,
            FitContent = false,
            ScrollActive = false,
            AutoSizeEnabled = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _description.AddThemeFontSizeOverride("normal_font_size", 18);
        if (regularFont is not null)
        {
            _description.AddThemeFontOverride("normal_font", regularFont);
        }
        _textContainer.AddChild(_description);

        MouseEntered += OnHovered;
        MouseExited += OnUnhovered;
        Pressed += OnPressed;
        Disabled = true;
    }

    public void SetCandidate(MCEnchantmentCandidate candidate)
    {
        Candidate = candidate;
        EnchantmentModel displayModel = candidate.CreateDisplayModel();
        _icon.Texture = MCEnchantingTableAssets.LoadTexture(candidate.IconPath);
        _title.Text = displayModel.Title.GetFormattedText();
        _level.Text = candidate.Level.ToString();
        _description.Text = displayModel.DynamicDescription.GetFormattedText();
        _image.Visible = false;
        _candidateContent.Visible = true;
        Disabled = false;
        SetSelected(false);
    }

    public void ClearCandidate()
    {
        _hoverTween?.Kill();
        _hovered = false;
        Scale = Vector2.One;
        Candidate = null;
        _icon.Texture = null;
        _title.Text = string.Empty;
        _level.Text = string.Empty;
        _description.Text = string.Empty;
        _image.Visible = true;
        _candidateBackground.SelfModulate = EmptyEntryColor;
        _candidateContent.Visible = false;
        Disabled = true;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        _candidateContent.Modulate = selected
            ? new Color(1.08f, 1.02f, 1.14f, 1f)
            : Colors.White;
        _candidateBackground.SelfModulate = Candidate is null
            ? EmptyEntryColor
            : _selected
            ? SelectedEntryColor
            : _hovered
                ? HoverEntryColor
                : NormalEntryColor;
        RefreshHighlight();
    }

    public override void _ExitTree()
    {
        _hoverTween?.Kill();
        MouseEntered -= OnHovered;
        MouseExited -= OnUnhovered;
        Pressed -= OnPressed;
        base._ExitTree();
    }

    private void OnHovered()
    {
        if (Candidate is null || Disabled)
        {
            return;
        }

        _hovered = true;
        RefreshHighlight();
        SfxCmd.Play(HoverSound);
        Animate(HoverScale, HoverEntryColor, 0.05);
    }

    private void OnUnhovered()
    {
        if (Candidate is null || Disabled)
        {
            return;
        }

        _hovered = false;
        RefreshHighlight();
        Animate(
            Vector2.One,
            Candidate is null
                ? EmptyEntryColor
                : _selected ? SelectedEntryColor : NormalEntryColor,
            0.35);
    }

    private void OnPressed()
    {
        if (Candidate is null)
        {
            return;
        }

        SfxCmd.Play(ClickSound);
        CandidateSelected?.Invoke(Candidate);
    }

    private void RefreshHighlight()
    {
        _highlightBorder.Visible = Candidate is not null && (_hovered || _selected);
    }

    private void Animate(Vector2 scale, Color color, double duration)
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenProperty(this, "scale", scale, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _hoverTween.TweenProperty(_image, "modulate", color, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _hoverTween.TweenProperty(_candidateBackground, "self_modulate", color, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }
}
