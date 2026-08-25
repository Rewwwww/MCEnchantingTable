using System.Text.Json;
using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;
using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting;

/// <summary>
/// Loads candidate rules from the packaged JSON and resolves its IDs to the
/// current Beta's canonical EnchantmentModel instances.
/// </summary>
internal static class MCEnchantmentConfig
{
    private const int SupportedSchemaVersion = 1;
    private static readonly Lazy<RuntimeConfig> LazyCurrent = new(Load);

    private static readonly HashSet<string> AmountIndependentModels =
    [
        "CLONE", "CORRUPTED", "GLAM", "IMBUED", "INKY", "INSTINCT",
        "PERFECT_FIT", "ROYALLY_APPROVED", "SLITHER", "SLUMBERING_ESSENCE",
        "SOULS_POWER", "SPIRAL", "STEADY", "TEZCATARAS_EMBER",
    ];

    public static RuntimeConfig Current => LazyCurrent.Value;

    public static IReadOnlyList<Entry> Entries => Current.Entries;

    public static bool CanAnyEnchant(CardModel card)
    {
        return Entries.Any(entry =>
            entry.CanonicalModel.CanEnchant(card) &&
            MCEnchantCompatibility.CanEnchantSafely(entry, card));
    }

    private static RuntimeConfig Load()
    {
        string path = MCEnchantingTableAssets.ConfigAssets.EnchantmentConfigPath;
        string json = Godot.FileAccess.GetFileAsString(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException($"Enchantment config is empty or unavailable: {path}");
        }

        ConfigDocument document = JsonSerializer.Deserialize<ConfigDocument>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Failed to deserialize MCEnchantingConfig.json.");
        if (document.SchemaVersion != SupportedSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported enchantment config schema {document.SchemaVersion}; expected {SupportedSchemaVersion}.");
        }

        IReadOnlyList<Entry> entries = document.Enchantments
            .Select(CreateEntry)
            .OrderBy(entry => entry.ModelId.Entry, StringComparer.Ordinal)
            .ToArray();
        if (entries.Count == 0)
        {
            throw new InvalidDataException("Enchantment config contains no candidate entries.");
        }

        IReadOnlyList<BookCountBand> bands = document.CandidateGeneration.BookCountBands
            .Select(CreateBand)
            .OrderBy(band => band.MinBookCount)
            .ToArray();
        ValidateBands(bands);

        RepeatWeightsDocument repeat = document.CandidateGeneration.RepeatWeights;
        if (repeat.FirstOccurrence <= 0f || repeat.SecondOccurrence <= 0f ||
            repeat.ThirdAndLaterOccurrence <= 0f)
        {
            throw new InvalidDataException("All repeat weights must be positive.");
        }

        return new RuntimeConfig(
            entries,
            bands,
            new RepeatWeights(
                repeat.FirstOccurrence,
                repeat.SecondOccurrence,
                repeat.ThirdAndLaterOccurrence));
    }

    private static Entry CreateEntry(EnchantmentDocument source)
    {
        if (string.IsNullOrWhiteSpace(source.Id))
        {
            throw new InvalidDataException("An enchantment config entry has no id.");
        }

        string idEntry = source.Id.Trim().ToUpperInvariant();
        ModelId modelId = new(ModelId.SlugifyCategory<EnchantmentModel>(), idEntry);
        EnchantmentModel canonical = ModelDb.GetById<EnchantmentModel>(modelId);
        Dictionary<MCEnchantmentLevel, int> amounts = [];
        foreach ((string levelName, int amount) in source.AmountByLevel)
        {
            MCEnchantmentLevel level = ParseLevel(levelName);
            if (amount <= 0)
            {
                throw new InvalidDataException($"{idEntry} {level} Amount must be positive.");
            }
            amounts.Add(level, amount);
        }

        HashSet<MCEnchantmentLevel> availableLevels = source.AvailableLevels
            .Select(ParseLevel)
            .ToHashSet();
        if (!availableLevels.SetEquals(amounts.Keys))
        {
            throw new InvalidDataException(
                $"{idEntry} availableLevels must exactly match amountByLevel keys.");
        }
        MCEnchantmentLevel maxLevel = ParseLevel(source.MaxMCLevel);
        if (availableLevels.Count == 0 || availableLevels.Max() != maxLevel)
        {
            throw new InvalidDataException(
                $"{idEntry} maxMCLevel does not match the highest available level.");
        }
        if (source.BaseWeight <= 0f)
        {
            throw new InvalidDataException($"{idEntry} baseWeight must be positive.");
        }
        if (string.IsNullOrWhiteSpace(source.IconPath))
        {
            throw new InvalidDataException($"{idEntry} has no iconPath.");
        }

        ValidateAmountSemantics(idEntry, amounts);
        return new Entry(canonical, amounts, source.BaseWeight, source.IconPath);
    }

    private static BookCountBand CreateBand(BookCountBandDocument source)
    {
        if (source.MinBookCount < 0 ||
            (source.MaxBookCount is not null && source.MaxBookCount < source.MinBookCount) ||
            source.Slots.Count == 0)
        {
            throw new InvalidDataException("Invalid bookCountBand range or empty slot list.");
        }

        SlotWeights[] slots = source.Slots.Select(slot =>
        {
            Dictionary<MCEnchantmentLevel, float> weights = slot.LevelWeights.ToDictionary(
                pair => ParseLevel(pair.Key),
                pair => pair.Value);
            if (weights.Count == 0 || weights.Values.Any(weight => weight <= 0f))
            {
                throw new InvalidDataException("Slot level weights must be non-empty and positive.");
            }
            return new SlotWeights(weights);
        }).ToArray();
        return new BookCountBand(source.MinBookCount, source.MaxBookCount, slots);
    }

    private static void ValidateBands(IReadOnlyList<BookCountBand> bands)
    {
        if (bands.Count == 0 || bands[0].MinBookCount != 0)
        {
            throw new InvalidDataException("Book-count bands must start at zero.");
        }

        for (int i = 0; i < bands.Count; i++)
        {
            BookCountBand band = bands[i];
            if (i < bands.Count - 1)
            {
                if (band.MaxBookCount is null ||
                    band.MaxBookCount.Value + 1 != bands[i + 1].MinBookCount)
                {
                    throw new InvalidDataException(
                        "Book-count bands must be contiguous and non-overlapping.");
                }
            }
            else if (band.MaxBookCount is not null)
            {
                throw new InvalidDataException("The final book-count band must have no maximum.");
            }
        }
    }

    private static void ValidateAmountSemantics(
        string id,
        IReadOnlyDictionary<MCEnchantmentLevel, int> amounts)
    {
        if (!AmountIndependentModels.Contains(id))
        {
            return;
        }

        foreach ((MCEnchantmentLevel level, int amount) in amounts)
        {
            if (amount != 1)
            {
                GD.PushWarning(
                    $"[MCEnchantingTable] {id} {level} configures Amount={amount}, " +
                    "but the current Beta EnchantmentModel does not read Amount. " +
                    "The original effect will remain fixed.");
            }
        }
    }

    private static MCEnchantmentLevel ParseLevel(string value)
    {
        return Enum.TryParse(value, ignoreCase: false, out MCEnchantmentLevel level)
            ? level
            : throw new InvalidDataException($"Unknown MC enchantment level '{value}'.");
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

    private sealed class ConfigDocument
    {
        public int SchemaVersion { get; init; }
        public CandidateGenerationDocument CandidateGeneration { get; init; } = new();
        public List<EnchantmentDocument> Enchantments { get; init; } = [];
    }

    private sealed class CandidateGenerationDocument
    {
        public List<BookCountBandDocument> BookCountBands { get; init; } = [];
        public RepeatWeightsDocument RepeatWeights { get; init; } = new();
    }

    private sealed class BookCountBandDocument
    {
        public int MinBookCount { get; init; }
        public int? MaxBookCount { get; init; }
        public List<SlotDocument> Slots { get; init; } = [];
    }

    private sealed class SlotDocument
    {
        public Dictionary<string, float> LevelWeights { get; init; } = [];
    }

    private sealed class RepeatWeightsDocument
    {
        public float FirstOccurrence { get; init; }
        public float SecondOccurrence { get; init; }
        public float ThirdAndLaterOccurrence { get; init; }
    }

    private sealed class EnchantmentDocument
    {
        public string Id { get; init; } = string.Empty;
        public List<string> AvailableLevels { get; init; } = [];
        public string MaxMCLevel { get; init; } = string.Empty;
        public Dictionary<string, int> AmountByLevel { get; init; } = [];
        public float BaseWeight { get; init; }
        public string IconPath { get; init; } = string.Empty;
    }
}
