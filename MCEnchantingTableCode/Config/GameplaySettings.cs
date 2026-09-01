using BaseLib.Config;
using Godot;

namespace MCEnchantingTable.MCEnchantingTableCode.Config;

/// <summary>One BaseLib registration and the original MCEnchantingTable.cfg file.</summary>
public sealed class GameplaySettings : SimpleModConfig
{
    private static GameplayConfig _current = DefaultSettingsFactory.CreateDefaultConfig();
    private static readonly Dictionary<string, int> LegacyValues = [];
    private static bool _loadedUnified;
    public static GameplayConfig Current => _current;
    [ConfigIgnore] public static int Revision { get; private set; }

    // BaseLib 3.4.5 persists properties through TypeConverter strings, not nested JSON.
    // A single JSON string preserves its existing atomic .cfg writer and save lifecycle.
    [ConfigHideInUI]
    public static string GameplayJson
    {
        get => SerializeGameplaySettings();
        set
        {
            _current = GameplayConfigCodec.Load(value, DefaultSettingsFactory.CreateDefaultConfig(),
                message => MainFile.Logger.Warn("Gameplay config: " + message));
            _loadedUnified = true;
            Revision++;
        }
    }

    public GameplaySettings()
    {
        // base.Init has already loaded old property names, if present.
        if (!_loadedUnified)
        {
            string path = Assets.MCEnchantingTableAssets.ConfigAssets.EnchantmentConfigPath;
            if (Godot.FileAccess.FileExists(path))
                _current = GameplayConfigCodec.Load(Godot.FileAccess.GetFileAsString(path),
                    DefaultSettingsFactory.CreateDefaultConfig(), message => MainFile.Logger.Warn("Gameplay migration: " + message));
            foreach ((string name, int value) in LegacyValues)
                typeof(BookGainSettings).GetProperty(name)!.SetValue(_current.BookGain, value);
            _current = GameplayConfigCodec.Load(SerializeGameplaySettings(), DefaultSettingsFactory.CreateDefaultConfig());
            Revision++;
        }
        LegacyValues.Clear();
        // Legacy aliases are read once, never written back as a parallel configuration.
        ConfigProperties.RemoveAll(property => property.Name != nameof(GameplayJson));
        Save();
    }

    public override bool VisibleInModList() => true;

    public override void SetupConfigUI(Control optionContainer)
    {
        UnifiedSettingsUi.Build(this, optionContainer);
        SetupFocusNeighbors(optionContainer);
    }

    internal BaseLib.Config.UI.NConfigCollapsibleSection CreateSettingsSection(bool collapsed) =>
        CreateCollapsibleSection("ConfigTitle", collapsedByDefault: collapsed);

    internal Control CreateSettingsButton(string label, Action pressed) => CreateRawButtonControl(label, pressed);

    internal BaseLib.Config.UI.NConfigOptionRow CreateSettingsRow(string name, Control label, Control setting) =>
        new(ModPrefix, name, label, setting);

    internal Control CreateSettingsDivider() => CreateDividerControl();

    internal void Edit(Action<GameplayConfig> edit)
    {
        edit(_current);
        Revision++;
        Changed();
        Save();
    }

    internal void ResetAll()
    {
        _current = DefaultSettingsFactory.CreateDefaultConfig();
        Revision++;
        Changed();
        Save();
        ConfigReloaded();
    }

    protected override void RestoreDefaultsNoConfirm() => ResetAll();

    public static string SerializeGameplaySettings() => GameplayConfigCodec.SerializeGameplaySettings(_current);
    public static string GetGameplayConfigFingerprint() => GameplayConfigCodec.GetGameplayConfigFingerprint(_current);

    [ConfigHideInUI] public static int Act1NormalCombatsPerReward { get => Current.BookGain.Act1NormalCombatsPerReward; set => LegacyValues[nameof(Act1NormalCombatsPerReward)] = value; }
    [ConfigHideInUI] public static int Act2NormalCombatsPerReward { get => Current.BookGain.Act2NormalCombatsPerReward; set => LegacyValues[nameof(Act2NormalCombatsPerReward)] = value; }
    [ConfigHideInUI] public static int Act3NormalCombatsPerReward { get => Current.BookGain.Act3NormalCombatsPerReward; set => LegacyValues[nameof(Act3NormalCombatsPerReward)] = value; }
    [ConfigHideInUI] public static int NormalCombatBookRewardAmount { get => Current.BookGain.NormalCombatBookRewardAmount; set => LegacyValues[nameof(NormalCombatBookRewardAmount)] = value; }
    [ConfigHideInUI] public static int EliteBookRewardAmount { get => Current.BookGain.EliteBookRewardAmount; set => LegacyValues[nameof(EliteBookRewardAmount)] = value; }
    [ConfigHideInUI] public static int BossBookRewardAmount { get => Current.BookGain.BossBookRewardAmount; set => LegacyValues[nameof(BossBookRewardAmount)] = value; }
}
