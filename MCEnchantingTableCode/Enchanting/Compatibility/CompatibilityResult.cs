namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting.Compatibility;

internal readonly record struct CompatibilityResult(bool IsAllowed, string Reason)
{
    public static CompatibilityResult Allowed { get; } = new(true, string.Empty);

    public static CompatibilityResult Reject(string reason) => new(false, reason);
}
