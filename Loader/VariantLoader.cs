using System.Reflection;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Random;

namespace MCEnchantingTable.Loader;

internal static class VariantLoader
{
    private const string ExpectedAssemblyName = "MCEnchantingTable.Content";
    private const string FamilyMetadataKey = "MCEnchantingTableCompatibilityFamily";

    internal static (Assembly Assembly, string RelativePath) Load(
        string modRoot, VariantManifest manifest, HostApiFamily family)
    {
        string familyName = family.ToString().ToLowerInvariant();
        VariantEntry entry = manifest.Variants.SingleOrDefault(v =>
            string.Equals(v.Family, familyName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"No variant is registered for host family '{familyName}'.");

        string fullRoot = Path.GetFullPath(modRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(modRoot, entry.Assembly.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Variant assembly path escapes the mod directory.");
        if (!File.Exists(path)) throw new FileNotFoundException("Variant Content DLL is missing.", path);

        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(typeof(LoaderMain).Assembly) ?? AssemblyLoadContext.Default;
        Assembly? ResolveHostAssembly(AssemblyLoadContext _, AssemblyName requested) =>
            string.Equals(requested.Name, typeof(Rng).Assembly.GetName().Name, StringComparison.Ordinal)
                ? typeof(Rng).Assembly
                : null;
        context.Resolving += ResolveHostAssembly;
        Assembly assembly;
        try
        {
            // Decompiled API snapshots may carry a development AssemblyVersion.
            // Always bind their sts2 reference to the host's already-loaded game assembly.
            assembly = context.LoadFromAssemblyPath(path);
        }
        finally
        {
            context.Resolving -= ResolveHostAssembly;
        }
        if (!string.Equals(assembly.GetName().Name, ExpectedAssemblyName, StringComparison.Ordinal))
            throw new BadImageFormatException($"Unexpected Content assembly identity: {assembly.GetName().Name}.");

        string? embeddedFamily = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, FamilyMetadataKey, StringComparison.Ordinal))?.Value;
        if (!string.Equals(embeddedFamily, familyName, StringComparison.OrdinalIgnoreCase))
            throw new BadImageFormatException($"Content family '{embeddedFamily ?? "<missing>"}' does not match host '{familyName}'.");
        return (assembly, Path.GetRelativePath(modRoot, path).Replace('\\', '/'));
    }
}
