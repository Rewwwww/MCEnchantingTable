using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace MCEnchantingTable.MCEnchantingTableCode.UI;

internal sealed partial class StrangeBookProgressOverlay : Control
{
    private const string GameBoldFontPath = "res://themes/kreon_bold_glyph_space_one.tres";

    private readonly StrangeBook _model;
    private Label? _label;

    public StrangeBookProgressOverlay(StrangeBook model)
    {
        _model = model;
        Name = "StrangeBookProgressOverlay";
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Ready()
    {
        // ICustomUiModel is also used by large relic previews. The progress is deliberately
        // limited to the top-bar inventory holder and is never drawn into the shared icon.
        if (!HasInventoryHolderAncestor())
        {
            Visible = false;
            return;
        }

        _label = CreateLabel();
        AddChild(_label);

        _model.ProgressDisplayChanged += Refresh;
        RunManager.Instance.ActEntered += Refresh;
        Refresh();
    }

    private bool HasInventoryHolderAncestor()
    {
        for (Node? ancestor = GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is NRelicInventoryHolder)
            {
                return true;
            }
        }

        return false;
    }

    public override void _ExitTree()
    {
        _model.ProgressDisplayChanged -= Refresh;
        RunManager.Instance.ActEntered -= Refresh;
        base._ExitTree();
    }

    private static Label CreateLabel()
    {
        Label label = new()
        {
            Name = "ProgressLabel",
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        label.SetAnchorsPreset(LayoutPreset.TopLeft);
        label.Position = new Vector2(13, 13);
        label.Size = new Vector2(43, 29);
        label.AddThemeColorOverride("font_color", new Color("#fff1bf"));
        label.AddThemeColorOverride("font_outline_color", new Color("#35170d"));
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeFontSizeOverride("font_size", 14);

        Font? gameFont = ResourceLoader.Load<Font>(GameBoldFontPath);
        if (gameFont is not null)
        {
            label.AddThemeFontOverride("font", gameFont);
        }

        return label;
    }

    private void Refresh()
    {
        if (_label is null)
        {
            return;
        }

        if (!_model.TryGetProgressDisplay(out int current, out int required) || required <= 1)
        {
            _label.Visible = false;
            return;
        }

        _label.Text = $"{current}/{required}";
        _label.Visible = true;
    }
}
