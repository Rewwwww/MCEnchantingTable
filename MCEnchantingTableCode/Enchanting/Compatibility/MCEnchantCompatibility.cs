using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;

internal static class MCEnchantCompatibility
{
    public static bool CanEnchantSafely(
        MCEnchantmentConfig.Entry definition,
        CardModel card)
    {
        return Evaluate(definition, card).IsAllowed;
    }

    public static CompatibilityResult Evaluate(
        MCEnchantmentConfig.Entry definition,
        CardModel card)
    {
        return EnchantmentCompatibilityRules.Evaluate(definition.ModelId.Entry, card);
    }
}
