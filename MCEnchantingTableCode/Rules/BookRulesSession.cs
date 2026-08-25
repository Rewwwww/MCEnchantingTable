namespace MCEnchantingTable.MCEnchantingTableCode.Rules;

internal static class BookRulesSession
{
    public static BookRulesSnapshot? PendingNewRunSnapshot { get; set; }

    public static BookRulesSnapshot ForNewRun() =>
        PendingNewRunSnapshot ?? BookRulesSnapshot.FromGlobalSettings();

    public static void Clear() => PendingNewRunSnapshot = null;
}
