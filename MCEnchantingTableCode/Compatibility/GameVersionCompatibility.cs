namespace MCEnchantingTable.MCEnchantingTableCode.Compatibility;

internal enum GameVersionFamily { Unknown, Stable, Beta }

internal static class GameVersionCompatibility
{
    // Loader has already detected the host and selected exactly one Content assembly.
    // Reuse that decision; never introduce a competing runtime detector.
    internal static GameVersionFamily Current =>
#if STS2_BETA
        GameVersionFamily.Beta;
#elif STS2_RELEASE
        GameVersionFamily.Stable;
#else
        GameVersionFamily.Unknown;
#endif
    internal static bool IsSupported => Current != GameVersionFamily.Unknown;
}
