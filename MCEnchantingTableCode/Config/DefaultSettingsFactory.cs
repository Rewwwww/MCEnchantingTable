namespace MCEnchantingTable.MCEnchantingTableCode.Config;

internal static class DefaultSettingsFactory
{
    // Embedded from the SAME config/MCEnchantingConfig.json exported into the PCK.
    // This also keeps migration/defaults usable before Godot resource services are ready.
    public static GameplayConfig CreateDefaultConfig()
    {
        using Stream stream = typeof(DefaultSettingsFactory).Assembly.GetManifestResourceStream(
            "MCEnchantingTable.GameplayDefaults.json")
            ?? throw new InvalidDataException("GameplayDefaults embedded resource is missing.");
        using StreamReader reader = new(stream);
        return GameplayConfigCodec.ReadDefaults(reader.ReadToEnd());
    }
}
