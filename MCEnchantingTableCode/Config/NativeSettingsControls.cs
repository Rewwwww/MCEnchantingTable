using System.Globalization;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.addons.mega_text;

namespace MCEnchantingTable.MCEnchantingTableCode.Config;

/// <summary>BaseLib's native 320x64 settings tickbox, bound to a dynamic config value.</summary>
internal partial class NativeSettingsTickbox : NSettingsTickbox
{
    private readonly Func<bool> _get;
    private readonly Action<bool> _set;

    internal NativeSettingsTickbox(Func<bool> get, Action<bool> set)
    {
        _get = get;
        _set = set;
        CustomMinimumSize = new Vector2(380, 64);
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        this.TransferAllNodes<NativeSettingsTickbox>(SceneHelper.GetScenePath("screens/settings_tickbox"));
    }

    public override void _Ready()
    {
        ConnectSignals();
        IsTicked = _get();
    }

    protected override void OnTick() => _set(true);
    protected override void OnUntick() => _set(false);
}

/// <summary>BaseLib's native settings slider with its value label replaced by an editable value field.</summary>
internal partial class NativeSettingsSlider : Control
{
    private readonly double _min;
    private readonly double _max;
    private readonly double _step;
    private readonly bool _percent;
    private double _initialValue;
    private double _value;
    private bool _suppress;
    private NSlider? _slider;
    private NMegaLineEdit? _input;
    private NSelectionReticle? _reticle;

    internal event Action<double>? ValueChanged;

    internal NativeSettingsSlider(double value, double min, double max, double step, bool percent)
    {
        _initialValue = value;
        _min = min;
        _max = max;
        _step = step;
        _percent = percent;
        CustomMinimumSize = new Vector2(380, 64);
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        FocusMode = FocusModeEnum.All;
        this.TransferAllNodes<NativeSettingsSlider>(SceneHelper.GetScenePath("screens/settings_slider"));
    }

    public override void _Ready()
    {
        _slider = GetNode<NSlider>("Slider");
        MegaLabel nativeValue = GetNode<MegaLabel>("SliderValue");
        _reticle = GetNode<NSelectionReticle>("SelectionReticle");
        nativeValue.Visible = false;

        // The stock scene overlays its value label on the track. Put the track and editable
        // value into separate container cells so neither control can occupy the other's area.
        HBoxContainer valueRow = new()
        {
            Name = "SliderAndNumberInput",
            MouseFilter = MouseFilterEnum.Pass,
        };
        valueRow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(valueRow);

        _slider.Reparent(valueRow);
        _slider.SetAnchorsPreset(LayoutPreset.TopLeft);
        _slider.OffsetLeft = 0;
        _slider.OffsetTop = 0;
        _slider.OffsetRight = 0;
        _slider.OffsetBottom = 0;
        _slider.CustomMinimumSize = new Vector2(0, 64);
        _slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _slider.SizeFlagsVertical = SizeFlags.Fill;

        Control spacer = new()
        {
            Name = "NumberInputSpacer",
            CustomMinimumSize = new Vector2(28, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        valueRow.AddChild(spacer);

        _input = new NMegaLineEdit
        {
            Name = "EditableSliderValue",
            Flat = false,
            SelectAllOnFocus = true,
            CaretBlink = true,
            Alignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(128, 52),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.All,
        };
        _input.AddThemeFontOverride("font", nativeValue.GetThemeFont("font"));
        _input.AddThemeFontSizeOverride("font_size", 28);
        _input.AddThemeColorOverride("font_color", nativeValue.GetThemeColor("font_color"));
        valueRow.AddChild(_input);
        MoveChild(valueRow, _reticle.GetIndex());

        _slider.MinValue = 0;
        _slider.MaxValue = _max - _min;
        _slider.Step = _step;
        _slider.ValueChanged += OnSliderValueChanged;
        _input.TextSubmitted += _ => CommitInput();
        _input.FocusExited += CommitInput;
        FocusEntered += () => _reticle.OnSelect();
        FocusExited += () => _reticle.OnDeselect();
        SetValueWithoutSignal(_initialValue);
    }

    public override void _GuiInput(InputEvent input)
    {
        base._GuiInput(input);
        if (input.IsActionPressed(MegaInput.left)) SetValue(_value - _step, notify: true);
        else if (input.IsActionPressed(MegaInput.right)) SetValue(_value + _step, notify: true);
    }

    internal void SetValueWithoutSignal(double value) => SetValue(value, notify: false);

    private void OnSliderValueChanged(double proxyValue)
    {
        if (!_suppress) SetValue(proxyValue + _min, notify: true);
    }

    private void CommitInput()
    {
        if (_input is null) return;
        string raw = _input.Text.Trim().TrimEnd('%');
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) &&
            !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            value = _value;
        SetValue(value, notify: true);
    }

    private void SetValue(double value, bool notify)
    {
        double clamped = Math.Clamp(value, _min, _max);
        if (_step > 0) clamped = Math.Clamp(_min + Math.Round((clamped - _min) / _step) * _step, _min, _max);
        bool changed = Math.Abs(_value - clamped) > 0.000001;
        _value = clamped;
        if (_slider is not null)
        {
            _suppress = true;
            _slider.SetValueWithoutAnimation(clamped - _min);
            _suppress = false;
        }
        if (_input is not null)
            _input.Text = _percent ? $"{clamped:0}%" : (Math.Abs(clamped - Math.Round(clamped)) < 0.000001 ? $"{clamped:0}" : $"{clamped:0.##}");
        if (notify && changed) ValueChanged?.Invoke(clamped);
    }
}
