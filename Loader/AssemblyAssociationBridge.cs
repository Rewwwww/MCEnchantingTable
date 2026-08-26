using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Modding;

namespace MCEnchantingTable.Loader;

internal static class AssemblyAssociationBridge
{
    private const string ModId = "MCEnchantingTable";
    private static readonly MethodInfo? AssociateAssemblyWithModMethod = typeof(ModManager).GetMethod(
        "AssociateAssemblyWithMod", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
        binder: null, [typeof(string), typeof(Assembly)], modifiers: null);
    private static readonly FieldInfo? ModAssembliesField = typeof(Mod).GetField(
        "assemblies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? LegacyModAssemblyField = typeof(Mod).GetField(
        "assembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static Assembly? _contentAssembly;
    private static bool _waitingForLegacyRecord;

    internal static void Associate(Assembly contentAssembly)
    {
        _contentAssembly = contentAssembly;
        if (AssociateAssemblyWithModMethod is not null)
        {
            try
            {
                AssociateAssemblyWithModMethod.Invoke(null, [ModId, contentAssembly]);
                LoaderMain.LogInfo("Content assembly associated through ModManager.AssociateAssemblyWithMod.");
                return;
            }
            catch (Exception exception)
            {
                LoaderMain.LogWarning("AssociateAssemblyWithMod failed: " + exception.GetBaseException().Message);
            }
        }

        Mod? mod = FindMod();
        if (mod is not null && ModAssembliesField?.GetValue(mod) is IList assemblies)
        {
            if (!assemblies.Cast<object>().Any(item => ReferenceEquals(item, contentAssembly)))
                assemblies.Add(contentAssembly);
            LoaderMain.LogInfo("Content assembly added to the host Mod assembly list.");
            return;
        }

        // In the 0.107.x host, TryLoadMod assigns mod.assembly to the Loader only
        // after this initializer returns. Replace it when that record is published.
        if (LegacyModAssemblyField is not null && !_waitingForLegacyRecord)
        {
            ModManager.OnModDetected += OnModDetected;
            _waitingForLegacyRecord = true;
            LoaderMain.LogInfo("Waiting to associate Content with the legacy single-assembly Mod record.");
            return;
        }

        throw new MissingMemberException("No supported ModManager assembly association mechanism was found.");
    }

    private static void OnModDetected(Mod mod)
    {
        if (_contentAssembly is null || !string.Equals(mod.manifest?.id, ModId, StringComparison.Ordinal))
            return;
        LegacyModAssemblyField!.SetValue(mod, _contentAssembly);
        ModManager.OnModDetected -= OnModDetected;
        _waitingForLegacyRecord = false;
        LoaderMain.LogInfo("Content assembly associated with the legacy single-assembly Mod record.");
    }

    private static Mod? FindMod() => ModManager.Mods.FirstOrDefault(mod =>
        string.Equals(mod.manifest?.id, ModId, StringComparison.Ordinal));
}
