using System.Reflection;
using HarmonyLib;

namespace MCEnchantingTable.Loader;

internal static class LoaderDiagnostics
{
    private const string LogFileName = "MCEnchantingTable.loader.log";
    private static string? _logPath;

    internal static void Start()
    {
        try
        {
            string root = Path.GetDirectoryName(typeof(LoaderDiagnostics).Assembly.Location)
                ?? AppContext.BaseDirectory;
            _logPath = Path.Combine(root, LogFileName);
            File.WriteAllText(_logPath,
                $"MCEnchantingTable Loader diagnostics {DateTimeOffset.Now:O}{Environment.NewLine}");
        }
        catch
        {
            _logPath = null;
        }
    }

    internal static void Write(string level, string message)
    {
        if (_logPath is null) return;
        try
        {
            File.AppendAllText(_logPath,
                $"[{DateTimeOffset.Now:O}] [{level}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostics must never prevent the Mod from loading.
        }
    }

    internal static void RecordContentTypes(Assembly content)
    {
        Type[] types = content.GetTypes();
        string[] required =
        [
            "MCEnchantingTable.MCEnchantingTableCode.Models.StrangeBook",
            "MCEnchantingTable.MCEnchantingTableCode.Rewards.BookReward"
        ];
        foreach (string typeName in required)
            Write("INFO", $"Content type {typeName}: {(content.GetType(typeName) is null ? "MISSING" : "FOUND")}");

        int harmonyPatchTypes = types.Count(type => type.GetCustomAttributesData().Any(attribute =>
            string.Equals(attribute.AttributeType.FullName, typeof(HarmonyPatch).FullName, StringComparison.Ordinal)));
        int savedPropertyTypes = types.Count(type => type.GetMembers(BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).Any(member => member.GetCustomAttributesData().Any(attribute =>
                    attribute.AttributeType.Name == "SavedPropertyAttribute")));
        int customUiTypes = types.Count(type => type.GetInterfaces().Any(contract =>
            contract.Name == "ICustomUiModel"));
        int targetedMessageTypes = types.Count(type => type.GetInterfaces().Any(contract =>
            contract.Name.Contains("CustomTargetedMessage", StringComparison.Ordinal)));
        Write("INFO", $"Content type inventory: total={types.Length}, HarmonyPatchTypes={harmonyPatchTypes}, " +
            $"SavedPropertyTypes={savedPropertyTypes}, CustomUiTypes={customUiTypes}, " +
            $"CustomTargetedMessageTypes={targetedMessageTypes}");
    }

    internal static void RecordHarmonyPatches()
    {
        IEnumerable<MethodBase> patched = Harmony.GetAllPatchedMethods()
            .Where(method =>
            {
                Patches? info = Harmony.GetPatchInfo(method);
                return info?.Owners.Any(owner => owner.StartsWith("MCEnchantingTable", StringComparison.Ordinal)) == true;
            });
        foreach (MethodBase method in patched.OrderBy(method => method.DeclaringType?.FullName).ThenBy(method => method.Name))
        {
            Patches info = Harmony.GetPatchInfo(method)!;
            string owners = string.Join(",", info.Owners.Where(owner =>
                owner.StartsWith("MCEnchantingTable", StringComparison.Ordinal)).OrderBy(owner => owner));
            Write("INFO", $"Harmony target FOUND: {method.DeclaringType?.FullName}.{method.Name}; owners={owners}");
        }
    }
}
