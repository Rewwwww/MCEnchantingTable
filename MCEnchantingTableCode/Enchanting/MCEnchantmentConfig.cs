using MCEnchantingTable.MCEnchantingTableCode.Config;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;
using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting;

internal static class MCEnchantmentConfig
{
    private static RuntimeConfig? _current;
    private static int _revision = -1;

    public static RuntimeConfig Current
    {
        get
        {
            if (_current is null || _revision != GameplaySettings.Revision)
            {
                _current = Load();
                _revision = GameplaySettings.Revision;
            }
            return _current;
        }
    }

    public static IReadOnlyList<Entry> Entries => Current.Entries;

    public static bool CanAnyEnchant(CardModel card) => Entries.Any(entry =>
        entry.CanonicalModel.CanEnchant(card) &&
        MCEnchantCompatibility.CanEnchantSafely(entry, card));

    private static RuntimeConfig Load()
    {
        GameplayConfig document = GameplayConfigCodec.Load(
            GameplaySettings.SerializeGameplaySettings(), DefaultSettingsFactory.CreateDefaultConfig(),
            message => MainFile.Logger.Warn("Gameplay config: " + message));
        List<Entry> entries = [];
        foreach (EnchantmentSettings source in document.Enchantments.OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            if (source.BaseWeight <= 0 || !source.Levels.Values.Any(l => l.Enabled)) continue;
            try
            {
                ModelId id = new(ModelId.SlugifyCategory<EnchantmentModel>(), source.Id);
                EnchantmentModel canonical = ModelDb.GetById<EnchantmentModel>(id);
                var amounts = source.Levels.Where(p => p.Value.Enabled).ToDictionary(
                    p => Enum.Parse<MCEnchantmentLevel>(p.Key),
                    p => Compatibility.EnchantmentMetadata.UsesAmount(source.Id) ? p.Value.Amount : 1);
                entries.Add(new Entry(canonical, amounts, source.BaseWeight, source.IconPath));
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Skipping unavailable enchantment {source.Id}: {e.Message}");
            }
        }
        var bands = document.CandidateGeneration.BookCountBands.Select(b => new BookCountBand(
            b.MinBookCount, b.MaxBookCount, b.Slots.Select(s => new SlotWeights(
                s.LevelWeights.Where(p => p.Value > 0).ToDictionary(
                    p => Enum.Parse<MCEnchantmentLevel>(p.Key), p => p.Value))).ToArray())).ToArray();
        var r = document.CandidateGeneration.RepeatWeights;
        return new RuntimeConfig(entries, bands, new RepeatWeights(
            r.FirstOccurrence, r.SecondOccurrence, r.ThirdAndLaterOccurrence));
    }

    internal sealed record RuntimeConfig(
        IReadOnlyList<Entry> Entries,
        IReadOnlyList<BookCountBand> BookCountBands,
        RepeatWeights RepeatWeights)
    {
        public BookCountBand GetBand(int bookCount)
        {
            return BookCountBands.FirstOrDefault(band => band.Contains(bookCount))
                ?? throw new InvalidDataException($"No book-count band covers {bookCount} books.");
        }
    }

    internal sealed record BookCountBand(
        int MinBookCount,
        int? MaxBookCount,
        IReadOnlyList<SlotWeights> Slots)
    {
        public bool Contains(int bookCount)
        {
            return bookCount >= MinBookCount &&
                (MaxBookCount is null || bookCount <= MaxBookCount.Value);
        }
    }

    internal sealed record SlotWeights(
        IReadOnlyDictionary<MCEnchantmentLevel, float> LevelWeights);

    internal sealed record RepeatWeights(
        float FirstOccurrence,
        float SecondOccurrence,
        float ThirdAndLaterOccurrence);

    internal sealed class Entry
    {
        private readonly IReadOnlyDictionary<MCEnchantmentLevel, int> _amountByLevel;

        public Entry(
            EnchantmentModel canonicalModel,
            IReadOnlyDictionary<MCEnchantmentLevel, int> amountByLevel,
            float baseWeight,
            string iconPath)
        {
            CanonicalModel = canonicalModel;
            _amountByLevel = amountByLevel;
            BaseWeight = baseWeight;
            IconPath = iconPath;
        }

        public EnchantmentModel CanonicalModel { get; }
        public ModelId ModelId => CanonicalModel.Id;
        public float BaseWeight { get; }
        public string IconPath { get; }

        public bool TryGetAmount(MCEnchantmentLevel level, out int amount)
        {
            return _amountByLevel.TryGetValue(level, out amount);
        }
    }

}
