using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MCEnchantingTable.Loader;

[ModInitializer(nameof(Initialize))]
public static class LoaderMain
{
    private const string ModId = "MCEnchantingTable";
    private const string ManifestName = "mc-enchanting-variants.manifest";
    private static readonly Logger Logger = new(ModId, LogType.Generic);
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            LogWarning("Initialize was requested more than once; duplicate initialization was ignored.");
            return;
        }
        LoaderDiagnostics.Start();
        _initialized = true;
        HostApiFamily family = HostVersionDetector.Detect(out string probes);
        LogInfo($"Host probes: {probes}");
        LogInfo($"Host API Family: {family}");
        if (family == HostApiFamily.Unknown)
        {
            LogError("No compatible content variant found. Content initialization aborted.");
            return;
        }

        try
        {
            string modRoot = Path.GetDirectoryName(typeof(LoaderMain).Assembly.Location)
                ?? throw new InvalidOperationException("Loader assembly directory is unavailable.");
            VariantManifest manifest = VariantManifest.Load(Path.Combine(modRoot, ManifestName));
            (Assembly content, string relativePath) = VariantLoader.Load(modRoot, manifest, family);
            LogInfo($"Variant: {family.ToString().ToLowerInvariant()}");
            LogInfo($"Content Assembly: {relativePath}");

            AssemblyAssociationBridge.Associate(content);
            TypeDiscoveryBridge.Install(content);
            LoaderDiagnostics.RecordContentTypes(content);
            LogInfo("Type Discovery: OK");
            InvokeContentInitializer(content);
            LoaderDiagnostics.RecordHarmonyPatches();
            LogInfo("Content Load / Harmony Init: OK");
        }
        catch (Exception exception)
        {
            LogError("Content initialization aborted: " + exception);
        }
    }

    private static void InvokeContentInitializer(Assembly assembly)
    {
        Type initializerType = assembly.GetType(
            "MCEnchantingTable.MCEnchantingTableCode.MainFile", throwOnError: true)!;
        MethodInfo method = initializerType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(initializerType.FullName, "Initialize");
        try { method.Invoke(null, null); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        { throw exception.InnerException; }
    }

    internal static void LogInfo(string message)
    {
        LoaderDiagnostics.Write("INFO", message);
        Logger.Info("[MCEnchantingTable Loader] " + message);
    }

    internal static void LogWarning(string message)
    {
        LoaderDiagnostics.Write("WARN", message);
        Logger.Warn("[MCEnchantingTable Loader] " + message);
    }

    internal static void LogError(string message)
    {
        LoaderDiagnostics.Write("ERROR", message);
        Logger.Error("[MCEnchantingTable Loader] " + message);
    }
}
