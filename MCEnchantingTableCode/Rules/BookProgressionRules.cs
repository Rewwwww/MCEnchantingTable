namespace MCEnchantingTable.MCEnchantingTableCode.Rules;

internal static class BookProgressionRules
{
    public static (bool RewardEarned, int NextProgress) AdvanceNormalCombat(
        int currentProgress,
        int requiredProgress)
    {
        int nextProgress = currentProgress + 1;
        if (nextProgress < requiredProgress)
        {
            return (false, nextProgress);
        }

        return (true, 0);
    }
}
