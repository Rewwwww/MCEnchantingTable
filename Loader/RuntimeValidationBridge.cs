using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MCEnchantingTable.Loader;

internal static class RuntimeValidationBridge
{
    private const string StrangeBookTypeName =
        "MCEnchantingTable.MCEnchantingTableCode.Models.StrangeBook";
    private static Assembly? _contentAssembly;

    internal static void Install(Harmony harmony, Assembly contentAssembly)
    {
        _contentAssembly = contentAssembly;
        harmony.Patch(AccessTools.Method(typeof(ModelDb), nameof(ModelDb.Init)),
            postfix: new HarmonyMethod(typeof(RuntimeValidationBridge), nameof(ModelDbInitPostfix)));
        harmony.Patch(AccessTools.Method(typeof(MessageTypes), nameof(MessageTypes.Initialize)),
            postfix: new HarmonyMethod(typeof(RuntimeValidationBridge), nameof(MessageTypesInitializePostfix)));
    }

    private static void ModelDbInitPostfix()
    {
        Type? strangeBook = _contentAssembly?.GetType(StrangeBookTypeName);
        bool registered = strangeBook is not null && ModelDb.Contains(strangeBook);
        LoaderDiagnostics.Write("INFO", $"ModelDb StrangeBook registration: {(registered ? "FOUND" : "MISSING")}");
    }

    private static void MessageTypesInitializePostfix()
    {
        if (_contentAssembly is null) return;
        Type[] messages = _contentAssembly.GetTypes().Where(type => type.GetInterfaces().Any(contract =>
            contract.Name.Contains("CustomTargetedMessage", StringComparison.Ordinal))).ToArray();
        Type? wrapper = Type.GetType("BaseLib.Abstracts.CustomTargetedMessageWrapper, BaseLib", throwOnError: false);
        PropertyInfo? targetedMessages = wrapper?.GetProperty(
            "TargetedMessages", BindingFlags.Public | BindingFlags.Static);
        IEnumerable<Type> registered = targetedMessages?.GetValue(null) as IEnumerable<Type> ?? [];
        foreach (Type message in messages)
        {
            bool found = registered.Contains(message);
            LoaderDiagnostics.Write(found ? "INFO" : "ERROR",
                $"BaseLib custom targeted message registration: {(found ? "FOUND" : "MISSING")}; {message.FullName}");
        }
    }
}
