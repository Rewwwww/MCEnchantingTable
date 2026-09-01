using BaseLib.Config;
using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace MCEnchantingTable.MCEnchantingTableCode.Config;

/// <summary>Content of the existing BaseLib settings page; no second menu or persistence.</summary>
internal static class UnifiedSettingsUi
{
    private static LocString Loc(string key) => new("settings_ui", "MCENCHANTINGTABLE-V2." + key);
    private static string Text(string key) => Loc(key).GetFormattedText();
    private static string FormatText(string key, params (string Name, string Value)[] values)
    {
        LocString text = Loc(key);
        foreach ((string name, string value) in values) text.Add(name, value);
        return text.GetFormattedText();
    }

    internal static void Build(GameplaySettings settings, Control parent) => Build(settings, parent, []);

    private static void Build(GameplaySettings settings, Control parent, Dictionary<string, bool> expanded)
    {
        VBoxContainer body = new() { Name = "UnifiedGameplaySettings", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 16);
        parent.AddChild(body); // BaseLib supplies the scrolling viewport.
        bool rebuildQueued = false;
        void QueueRebuild()
        {
            if (rebuildQueued) return;
            rebuildQueued = true;
            Callable.From(Rebuild).CallDeferred();
        }
        void Rebuild()
        {
            if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(body)) return;
            parent.RemoveChild(body);
            body.QueueFree();
            Build(settings, parent, expanded);
            GameplaySettings.SetupFocusNeighbors(parent);
        }

        VBoxContainer Section(Control owner, string key, string title, bool collapsed = false)
        {
            var section = settings.CreateSettingsSection(!expanded.GetValueOrDefault(key, !collapsed));
            // Keep BaseLib's arrow, focus, sounds and expansion lifecycle. Only the dynamic title differs.
            var label = section.FindChildren("*", "", true, false).OfType<RichTextLabel>().Single();
            ConfigureText(label);
            label.Text = "[b]" + title + "[/b]";
            label.AddThemeFontSizeOverride("normal_font_size", 28);
            label.AddThemeFontSizeOverride("bold_font_size", 28);
            for (Node? node = label.GetParent(); node is Control control && node != section; node = node.GetParent())
                control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            section.OnToggled += value => expanded[key] = value;
            owner.AddChild(section);
            return section.ContentContainer;
        }

        var progress = Section(body, "progress", Text("normalProgress"));
        var drops = Section(body, "drops", Text("bookDrops"));
        foreach (string name in new[] { nameof(BookGainSettings.Act1NormalCombatsPerReward), nameof(BookGainSettings.Act2NormalCombatsPerReward), nameof(BookGainSettings.Act3NormalCombatsPerReward), nameof(BookGainSettings.NormalCombatBookRewardAmount), nameof(BookGainSettings.EliteBookRewardAmount), nameof(BookGainSettings.BossBookRewardAmount) })
        {
            var property = typeof(BookGainSettings).GetProperty(name)!;
            Number(settings, name.Contains("PerReward", StringComparison.Ordinal) ? progress : drops, Text(name), (int)property.GetValue(GameplaySettings.Current.BookGain)!,
                name.Contains("PerReward", StringComparison.Ordinal) ? 1 : 0, 20, 1,
                n => settings.Edit(c => property.SetValue(c.BookGain, (int)n)));
        }
        Toggle(settings, drops, Text("remainder"), GameplaySettings.Current.BookGain.RemainderCompensation,
            b => settings.Edit(c => c.BookGain.RemainderCompensation = b));

        var candidates = Section(body, "candidates", Text("candidates"));
        for (int bandIndex = 0; bandIndex < GameplaySettings.Current.CandidateGeneration.BookCountBands.Count; bandIndex++)
        {
            int b = bandIndex;
            var band = GameplaySettings.Current.CandidateGeneration.BookCountBands[b];
            string minBooks = band.MinBookCount.ToString(System.Globalization.CultureInfo.CurrentCulture);
            string bandTitle = band.MaxBookCount.HasValue
                ? FormatText("band", ("Min", minBooks), ("Max", band.MaxBookCount.Value.ToString(System.Globalization.CultureInfo.CurrentCulture)))
                : FormatText("bandOpen", ("Min", minBooks));
            var bandBody = Section(candidates, "band:" + b, bandTitle, collapsed: true);
            if (band.MaxBookCount.HasValue)
                Number(settings, bandBody, Text("upperBound"), band.MaxBookCount.Value, band.MinBookCount,
                    GameplaySettings.Current.CandidateGeneration.BookCountBands[b + 1].MaxBookCount - 1 ?? 9999, 1,
                    n =>
                    {
                        settings.Edit(c =>
                        {
                            c.CandidateGeneration.BookCountBands[b].MaxBookCount = (int)n;
                            c.CandidateGeneration.BookCountBands[b + 1].MinBookCount = (int)n + 1;
                        });
                        QueueRebuild();
                    });
            Number(settings, bandBody, Text("slots"), band.Slots.Count, 1, 3, 1, n =>
            {
                settings.Edit(c =>
                {
                    var slots = c.CandidateGeneration.BookCountBands[b].Slots;
                    var defaults = DefaultSettingsFactory.CreateDefaultConfig().CandidateGeneration.BookCountBands;
                    var template = defaults[Math.Min(b, defaults.Count - 1)].Slots;
                    while (slots.Count < (int)n) slots.Add(new SlotSettings { LevelWeights = new(template[Math.Min(slots.Count, template.Count - 1)].LevelWeights) });
                    while (slots.Count > (int)n) slots.RemoveAt(slots.Count - 1);
                });
                QueueRebuild();
            });
            for (int slotIndex = 0; slotIndex < band.Slots.Count; slotIndex++)
            {
                int s = slotIndex;
                Header(bandBody, FormatText("slotProbability", ("Slot", (s + 1).ToString(System.Globalization.CultureInfo.CurrentCulture))), 26);
                Dictionary<string, NativeSettingsSlider> controls = [];
                foreach (string level in GameplayConfigCodec.Levels)
                {
                    NativeSettingsSlider slider = new(band.Slots[s].LevelWeights.GetValueOrDefault(level) * 100, 0, 100, 1, percent: true);
                    controls[level] = slider;
                    AddSetting(settings, bandBody, FormatText("levelProbability", ("Level", level)), slider);
                    slider.ValueChanged += n =>
                    {
                        settings.Edit(c =>
                        {
                            var weights = c.CandidateGeneration.BookCountBands[b].Slots[s].LevelWeights;
                            string[] other = GameplayConfigCodec.Levels.Where(l => l != level).ToArray();
                            float rest = other.Sum(l => weights.GetValueOrDefault(l));
                            weights[level] = (float)n / 100;
                            foreach (string l in other)
                                weights[l] = (1 - weights[level]) * (rest > 0 ? weights.GetValueOrDefault(l) / rest : 1f / other.Length);
                            foreach ((string l, NativeSettingsSlider control) in controls) control.SetValueWithoutSignal(weights.GetValueOrDefault(l) * 100);
                        });
                    };
                }
            }
        }
        var repeatWeights = Section(body, "repeatWeights", Text("repeatWeights"), collapsed: true);
        Header(repeatWeights, Text("repeatWeightDescription"), 22);
        foreach (string name in new[] { nameof(RepeatWeightSettings.SecondOccurrence), nameof(RepeatWeightSettings.ThirdAndLaterOccurrence) })
        {
            var property = typeof(RepeatWeightSettings).GetProperty(name)!;
            Number(settings, repeatWeights, Text(name), (float)property.GetValue(GameplaySettings.Current.CandidateGeneration.RepeatWeights)!, 0, 1, 0.05,
                n => settings.Edit(c => property.SetValue(c.CandidateGeneration.RepeatWeights, (float)n)));
        }

        var enchantments = Section(body, "enchantments", Text("enchantments"));
        for (int index = 0; index < GameplaySettings.Current.Enchantments.Count; index++)
        {
            int i = index;
            var enchantment = GameplaySettings.Current.Enchantments[i];
            string title = new LocString("enchantments", enchantment.Id + ".title").GetFormattedText();
            var enchantBody = Section(enchantments, "enchant:" + enchantment.Id, title, collapsed: true);
            foreach (string level in GameplayConfigCodec.Levels)
            {
                NativeSettingsTickbox enabled = new(() => GameplaySettings.Current.Enchantments[i].Levels[level].Enabled,
                    value => settings.Edit(c => c.Enchantments[i].Levels[level].Enabled = value));
                AddSetting(settings, enchantBody, FormatText("levelEnabled", ("Level", level)), enabled);
                if (EnchantmentMetadata.UsesAmount(enchantment.Id))
                {
                    Number(settings, enchantBody, FormatText("levelAmount", ("Level", level)), enchantment.Levels[level].Amount, 1, 999, 1,
                        n => settings.Edit(c => c.Enchantments[i].Levels[level].Amount = (int)n));
                }
            }
        }
        var entrances = Section(body, "entrances", Text("entrances"));
        Entrance("campfire", c => c.Campfire);
        Entrance("ancient", c => c.Ancient);
        void Entrance(string key, Func<GameplayConfig, EntranceSettings> section)
        {
            Header(entrances, Text(key), 26);
            Toggle(settings, entrances, Text(key + "Enabled"), section(GameplaySettings.Current).Enabled,
                value => settings.Edit(c => section(c).Enabled = value));
            Number(settings, entrances, Text("healPercent"), (double)section(GameplaySettings.Current).HealPercent, 0, 100, 1,
                value => settings.Edit(c => section(c).HealPercent = (decimal)value));
        }

        // One centered native BaseLib button; confirmation and reset behavior remain unchanged.
        var reset = settings.CreateSettingsButton(Text("reset"), async () =>
        {
            NGenericPopup? popup = NGenericPopup.Create();
            if (popup is null || NModalContainer.Instance is null) return;
            NModalContainer.Instance.Add(popup);
            if (await popup.WaitForConfirmation(Loc("resetBody"), Loc("resetTitle"), Loc("cancel"), Loc("restore")))
            {
                settings.ResetAll();
                QueueRebuild();
            }
        });
        reset.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        MarginContainer resetMargin = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        resetMargin.AddThemeConstantOverride("margin_top", 32);
        resetMargin.AddThemeConstantOverride("margin_bottom", 32);
        resetMargin.AddChild(reset);
        body.AddChild(resetMargin);
    }

    private static void ConfigureText(RichTextLabel text)
    {
        text.FitContent = true;
        text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        text.HorizontalAlignment = HorizontalAlignment.Left;
    }

    private static void Header(Control parent, string text, int size = 32)
    {
        var label = ModConfig.CreateRawLabelControl(text, size);
        ConfigureText(label);
        parent.AddChild(label);
    }
    private static void AddSetting(GameplaySettings settings, Control parent, string label, Control setting)
    {
        var text = ModConfig.CreateRawLabelControl(label, 28);
        ConfigureText(text);
        text.CustomMinimumSize = new Vector2(0, 64);
        MarginContainer labelArea = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        labelArea.AddThemeConstantOverride("margin_right", 416);
        labelArea.AddChild(text);
        var row = settings.CreateSettingsRow("SettingRow", labelArea, setting);
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("margin_left", 16);
        row.AddThemeConstantOverride("margin_right", 32);
        parent.AddChild(row);
        parent.AddChild(settings.CreateSettingsDivider());
    }
    private static void Number(GameplaySettings settings, Control parent, string label, double value, double min, double max, double step, Action<double> changed, bool percent = false)
    {
        NativeSettingsSlider slider = new(value, min, max, step, percent);
        AddSetting(settings, parent, label, slider);
        slider.ValueChanged += changed;
    }
    private static void Toggle(GameplaySettings settings, Control parent, string label, bool value, Action<bool> changed)
    {
        NativeSettingsTickbox button = new(() => value, changed);
        AddSetting(settings, parent, label, button);
    }
}
