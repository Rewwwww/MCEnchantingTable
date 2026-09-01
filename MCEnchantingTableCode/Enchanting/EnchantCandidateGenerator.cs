using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting;

internal sealed class EnchantCandidateGenerator
{
    public IReadOnlyList<MCEnchantmentCandidate> Generate(
        CardModel card,
        int bookCount,
        Rng rng)
    {
        MCEnchantmentConfig.RuntimeConfig config = MCEnchantmentConfig.Current;
        IReadOnlyList<MCEnchantmentLevel> levels = RollSlotLevels(bookCount, rng, config);
        List<MCEnchantmentCandidate> results = new(levels.Count);
        Dictionary<ModelId, int> selectedNameCounts = [];

        foreach (MCEnchantmentLevel level in levels)
        {
            List<MCEnchantmentConfig.Entry> legalEntries = MCEnchantmentConfig.Entries
                .Where(entry =>
                    entry.TryGetAmount(level, out _) &&
                    GetWeight(entry, selectedNameCounts, config.RepeatWeights) > 0 &&
                    entry.CanonicalModel.CanEnchant(card) &&
                    IsMcCompatible(entry, card) &&
                    results.All(candidate =>
                        candidate.EnchantmentModelId != entry.ModelId ||
                        candidate.Level != level))
                .OrderBy(entry => entry.ModelId.Entry, StringComparer.Ordinal)
                .ToList();

            if (legalEntries.Count == 0)
            {
                continue;
            }

            MCEnchantmentConfig.Entry? selected = rng.WeightedNextItem(
                legalEntries,
                entry => GetWeight(entry!, selectedNameCounts, config.RepeatWeights));
            if (selected is null || !selected.TryGetAmount(level, out int amount))
            {
                continue;
            }

            selectedNameCounts[selected.ModelId] =
                selectedNameCounts.GetValueOrDefault(selected.ModelId) + 1;
            string keyBase = selected.ModelId.Entry;
            results.Add(new MCEnchantmentCandidate(
                selected.ModelId,
                level,
                amount,
                $"{keyBase}.title",
                $"{keyBase}.description",
                selected.IconPath));
        }

        return results;
    }

    private static bool IsMcCompatible(
        MCEnchantmentConfig.Entry definition,
        CardModel card)
    {
        CompatibilityResult result = MCEnchantCompatibility.Evaluate(definition, card);
        if (result.IsAllowed)
        {
            return true;
        }

        MainFile.Logger.Debug(
            $"MC enchantment candidate rejected: Card={card.Id}, " +
            $"Enchantment={definition.ModelId.Entry}, Reason={result.Reason}");
        return false;
    }

    internal static IReadOnlyList<MCEnchantmentLevel> RollSlotLevels(
        int bookCount,
        Rng rng,
        MCEnchantmentConfig.RuntimeConfig? config = null)
    {
        MCEnchantmentConfig.RuntimeConfig current = config ?? MCEnchantmentConfig.Current;
        MCEnchantmentConfig.BookCountBand band = current.GetBand(Math.Max(0, bookCount));
        return band.Slots.Select(slot => rng.WeightedNextItem(
                slot.LevelWeights.Keys.OrderBy(level => level),
                level => slot.LevelWeights[level]))
            .ToArray();
    }

    private static float GetWeight(
        MCEnchantmentConfig.Entry entry,
        IReadOnlyDictionary<ModelId, int> selectedNameCounts,
        MCEnchantmentConfig.RepeatWeights repeatWeights)
    {
        int previousSelections = selectedNameCounts.GetValueOrDefault(entry.ModelId);
        float repeatMultiplier = previousSelections switch
        {
            0 => repeatWeights.FirstOccurrence,
            1 => repeatWeights.SecondOccurrence,
            _ => repeatWeights.ThirdAndLaterOccurrence,
        };
        return entry.BaseWeight * repeatMultiplier;
    }
}
