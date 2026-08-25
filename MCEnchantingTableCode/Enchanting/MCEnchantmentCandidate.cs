using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting;

/// <summary>
/// Immutable UI-facing result of one candidate roll. It does not mutate or
/// attach an enchantment to a card.
/// </summary>
internal sealed record MCEnchantmentCandidate(
    ModelId EnchantmentModelId,
    MCEnchantmentLevel Level,
    int Amount,
    string NameKey,
    string DescriptionKey,
    string IconPath)
{
    public EnchantmentModel CreateDisplayModel()
    {
        EnchantmentModel model = ModelDb.GetById<EnchantmentModel>(EnchantmentModelId).ToMutable();
        model.Amount = Amount;
        model.RecalculateValues();
        return model;
    }
}
