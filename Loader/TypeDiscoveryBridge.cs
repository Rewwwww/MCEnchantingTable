using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace MCEnchantingTable.Loader;

internal static class TypeDiscoveryBridge
{
    private static Assembly? _contentAssembly;
    private static bool _installed;

    internal static void Install(Assembly contentAssembly)
    {
        _contentAssembly = contentAssembly;
        if (_installed) return;

        MethodInfo? modTypesGetter = AccessTools.PropertyGetter(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes));
        MethodInfo? getSubtypes = AccessTools.Method(typeof(ReflectionHelper), nameof(ReflectionHelper.GetSubtypesFromAssembly));
        if (modTypesGetter is null || getSubtypes is null)
            throw new MissingMethodException("Required ReflectionHelper type-discovery APIs were not found.");

        Harmony harmony = new("MCEnchantingTable.Loader.TypeDiscovery");
        harmony.Patch(modTypesGetter, postfix: new HarmonyMethod(typeof(TypeDiscoveryBridge), nameof(ModTypesPostfix)));
        harmony.Patch(getSubtypes, postfix: new HarmonyMethod(typeof(TypeDiscoveryBridge), nameof(GetSubtypesPostfix)));
        RuntimeValidationBridge.Install(harmony, contentAssembly);
        _installed = true;
    }

    private static void ModTypesPostfix(ref Type[] __result)
    {
        Type[] extra = GetLoadableTypes();
        if (extra.Length > 0) __result = [.. __result.Concat(extra).Distinct()];
    }

    private static void GetSubtypesPostfix(Assembly assembly, Type parentType, ref IEnumerable<Type> __result)
    {
        if (!ReferenceEquals(assembly, typeof(LoaderMain).Assembly)) return;
        Type[] extra = GetLoadableTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && parentType.IsAssignableFrom(type))
            .ToArray();
        if (extra.Length > 0) __result = __result.Concat(extra).Distinct();
    }

    private static Type[] GetLoadableTypes()
    {
        if (_contentAssembly is null) return [];
        try { return _contentAssembly.GetTypes(); }
        catch (ReflectionTypeLoadException exception)
        {
            LoaderMain.LogWarning("Partial Content type load: " +
                string.Join(" | ", exception.LoaderExceptions.OfType<Exception>().Select(e => e.Message).Take(8)));
            return exception.Types.OfType<Type>().ToArray();
        }
    }
}
