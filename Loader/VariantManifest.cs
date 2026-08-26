using System.Text.Json;

namespace MCEnchantingTable.Loader;

internal sealed class VariantManifest
{
    public int SchemaVersion { get; set; }
    public List<VariantEntry> Variants { get; set; } = [];

    internal static VariantManifest Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Variant manifest is missing.", path);
        VariantManifest? manifest = JsonSerializer.Deserialize<VariantManifest>(
            File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (manifest is null || manifest.SchemaVersion != 1 || manifest.Variants.Count == 0)
            throw new InvalidDataException("Variant manifest is empty or uses an unsupported schema.");
        return manifest;
    }
}

internal sealed class VariantEntry
{
    public string Id { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Assembly { get; set; } = string.Empty;
}
