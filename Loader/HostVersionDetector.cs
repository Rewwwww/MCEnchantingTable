using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace MCEnchantingTable.Loader;

internal enum HostApiFamily { Unknown, Beta, Release }

internal static class HostVersionDetector
{
    internal static HostApiFamily Detect(out string diagnostics)
    {
        ConstructorInfo? betaRng = typeof(Rng).GetConstructor([typeof(ulong)]);
        ConstructorInfo? releaseRng = typeof(Rng).GetConstructor([typeof(uint), typeof(int)]);
        MethodInfo? oldRewardHook = typeof(AbstractModel).GetMethod(
            "BeforeCombatRewardOffered",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        diagnostics = $"Rng(ulong)={(betaRng is not null)}, " +
            $"Rng(uint,int)={(releaseRng is not null)}, " +
            $"BeforeCombatRewardOffered={(oldRewardHook is not null)}";

        if (betaRng is not null && releaseRng is null && oldRewardHook is not null)
            return HostApiFamily.Beta;
        if (betaRng is null && releaseRng is not null && oldRewardHook is null)
            return HostApiFamily.Release;
        return HostApiFamily.Unknown;
    }
}
