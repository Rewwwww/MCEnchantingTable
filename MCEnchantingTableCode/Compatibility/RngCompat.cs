using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;

namespace MCEnchantingTable.MCEnchantingTableCode.Compatibility;

/// <summary>
/// The only compile-time boundary for the Beta/Release Rng API difference.
/// Candidate/session business code remains shared between both variants.
/// </summary>
internal static class RngCompat
{
    internal static ulong ReadRunSeed(Player player)
    {
#if STS2_BETA
        return player.RunState.Rng.Seed;
#elif STS2_RELEASE
        return player.RunState.Rng.Seed;
#else
#error A supported StS2 compatibility target must be selected.
#endif
    }

    internal static Rng CreateDeterministic(ulong seed)
    {
#if STS2_BETA
        return new Rng(seed);
#elif STS2_RELEASE
        uint foldedSeed = unchecked((uint)seed ^ (uint)(seed >> 32));
        return new Rng(foldedSeed, 0);
#else
#error A supported StS2 compatibility target must be selected.
#endif
    }
}
