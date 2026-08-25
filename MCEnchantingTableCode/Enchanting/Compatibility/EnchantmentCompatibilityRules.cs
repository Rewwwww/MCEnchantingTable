using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;

/// <summary>
/// MC-only safety rules audited against the current Beta implementations.
/// These rules do not alter the canonical enchantments' CanEnchant behavior.
/// </summary>
internal static class EnchantmentCompatibilityRules
{
    private delegate CompatibilityResult Rule(CardModel card);

    private static readonly IReadOnlyDictionary<string, Rule> Rules =
        new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["ADROIT"] = AlwaysAllow,
            ["CLONE"] = AlwaysAllow,
            ["CORRUPTED"] = RequireAttack,
            ["GLAM"] = AlwaysAllow,
            ["GOOPY"] = RequireDefendTag,
            ["IMBUED"] = RequireSkill,
            ["INKY"] = RequireEnemyTarget,
            ["INSTINCT"] = RequireAttack,
            ["MOMENTUM"] = RequireAttack,
            ["NIMBLE"] = RequireBlock,
            ["PERFECT_FIT"] = AlwaysAllow,
            ["ROYALLY_APPROVED"] = RequireAttackOrSkill,
            ["SHARP"] = RequireAttack,
            ["SLITHER"] = RequireRandomizableEnergyCost,
            ["SLUMBERING_ESSENCE"] = RequireReducibleEnergyCost,
            ["SOULS_POWER"] = RequireLocalExhaust,
            ["SOWN"] = AlwaysAllow,
            ["SPIRAL"] = RequireBasicStrikeOrDefend,
            ["STEADY"] = AlwaysAllow,
            ["SWIFT"] = AlwaysAllow,
            ["TEZCATARAS_EMBER"] = AlwaysAllow,
            ["VIGOROUS"] = RequireAttack,
        };

    public static CompatibilityResult Evaluate(string enchantmentId, CardModel card)
    {
        return Rules.TryGetValue(enchantmentId, out Rule? rule)
            ? rule(card)
            : CompatibilityResult.Reject("UnreviewedEnchantment");
    }

    private static CompatibilityResult AlwaysAllow(CardModel _) => CompatibilityResult.Allowed;

    private static CompatibilityResult RequireAttack(CardModel card) =>
        card.Type == CardType.Attack
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("RequiresAttackCard");

    private static CompatibilityResult RequireSkill(CardModel card) =>
        card.Type == CardType.Skill
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("RequiresSkillCard");

    private static CompatibilityResult RequireAttackOrSkill(CardModel card) =>
        card.Type is CardType.Attack or CardType.Skill
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("RequiresAttackOrSkillCard");

    private static CompatibilityResult RequireDefendTag(CardModel card) =>
        card.Tags.Contains(CardTag.Defend)
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("RequiresDefendTag");

    private static CompatibilityResult RequireBlock(CardModel card) =>
        card.GainsBlock
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("CardDoesNotGainBlock");

    private static CompatibilityResult RequireLocalExhaust(CardModel card) =>
        card.GetKeywordsWithSources(KeywordSources.Local).Contains(CardKeyword.Exhaust)
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("NoLocalExhaustKeyword");

    private static CompatibilityResult RequireEnemyTarget(CardModel card) =>
        card.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("NoSafeEnemyTarget");

    private static CompatibilityResult RequireRandomizableEnergyCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
        {
            return CompatibilityResult.Reject("XCostCannotBeRandomized");
        }
        return card.Keywords.Contains(CardKeyword.Unplayable)
            ? CompatibilityResult.Reject("UnplayableCard")
            : CompatibilityResult.Allowed;
    }

    private static CompatibilityResult RequireReducibleEnergyCost(CardModel card) =>
        card.EnergyCost.CostsX
            ? CompatibilityResult.Reject("XCostIgnoresRelativeCostReduction")
            : CompatibilityResult.Allowed;

    private static CompatibilityResult RequireBasicStrikeOrDefend(CardModel card) =>
        card.Rarity == CardRarity.Basic &&
        (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend))
            ? CompatibilityResult.Allowed
            : CompatibilityResult.Reject("RequiresBasicStrikeOrDefend");
}
