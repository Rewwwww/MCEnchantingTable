namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;

/// <summary>Audited against both bundled API snapshots; fixed effects always apply Amount=1.</summary>
internal static class EnchantmentMetadata
{
    public static bool UsesAmount(string id) => id is
        "ADROIT" or "GOOPY" or "MOMENTUM" or "NIMBLE" or
        "SHARP" or "SOWN" or "SWIFT" or "VIGOROUS";
}
