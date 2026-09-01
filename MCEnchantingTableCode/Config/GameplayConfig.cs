using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MCEnchantingTable.MCEnchantingTableCode.Config;

public sealed class GameplayConfig
{
    public int SchemaVersion { get; set; }
    public BookGainSettings BookGain { get; set; } = new();
    public CandidateSettings CandidateGeneration { get; set; } = new();
    public List<EnchantmentSettings> Enchantments { get; set; } = [];
    public EntranceSettings Campfire { get; set; } = new();
    public EntranceSettings Ancient { get; set; } = new();
}

public sealed class BookGainSettings
{
    public int Act1NormalCombatsPerReward { get; set; }
    public int Act2NormalCombatsPerReward { get; set; }
    public int Act3NormalCombatsPerReward { get; set; }
    public int NormalCombatBookRewardAmount { get; set; }
    public int EliteBookRewardAmount { get; set; }
    public int BossBookRewardAmount { get; set; }
    public bool RemainderCompensation { get; set; }
}

public sealed class CandidateSettings
{
    public List<BookBandSettings> BookCountBands { get; set; } = [];
    public RepeatWeightSettings RepeatWeights { get; set; } = new();
}

public sealed class BookBandSettings
{
    public int MinBookCount { get; set; }
    public int? MaxBookCount { get; set; }
    public List<SlotSettings> Slots { get; set; } = [];
}

public sealed class SlotSettings
{
    public Dictionary<string, float> LevelWeights { get; set; } = [];
}

public sealed class RepeatWeightSettings
{
    public float FirstOccurrence { get; set; }
    public float SecondOccurrence { get; set; }
    public float ThirdAndLaterOccurrence { get; set; }
}

public sealed class EnchantmentSettings
{
    public string Id { get; set; } = "";
    public Dictionary<string, EnchantmentLevelSettings> Levels { get; set; } = [];
    public float BaseWeight { get; set; }
    public string IconPath { get; set; } = "";
}

public sealed class EnchantmentLevelSettings
{
    public bool Enabled { get; set; }
    public int Amount { get; set; }
}

public sealed class EntranceSettings
{
    public bool Enabled { get; set; }
    public decimal HealPercent { get; set; }
}

/// <summary>Pure codec: no Godot, model, UI, RNG or filesystem state.</summary>
public static class GameplayConfigCodec
{
    public static readonly string[] Levels = ["I", "II", "III", "IV"];
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static string SerializeGameplaySettings(GameplayConfig config) =>
        Canonicalize(JsonSerializer.SerializeToNode(config, Options))!.ToJsonString(Options);

    public static string GetGameplayConfigFingerprint(GameplayConfig config)
    {
        JsonObject gameplay = JsonSerializer.SerializeToNode(config, Options)!.AsObject();
        // Art paths do not affect gameplay or network compatibility.
        foreach (JsonNode? entry in gameplay["enchantments"]!.AsArray())
            entry!.AsObject().Remove("iconPath");
        string canonical = Canonicalize(gameplay)!.ToJsonString();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static GameplayConfig ReadDefaults(string json) =>
        JsonSerializer.Deserialize<GameplayConfig>(json, Options)
        ?? throw new InvalidDataException("Packaged gameplay defaults are missing.");

    public static GameplayConfig Load(string json, GameplayConfig defaults, Action<string>? warn = null)
    {
        JsonObject root;
        try { root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject(); }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            warn?.Invoke("Invalid config JSON; using packaged defaults: " + e.Message);
            return ReadDefaults(SerializeGameplaySettings(defaults));
        }
        int version = ReadInt(root["schemaVersion"], 1);
        if (version is not (1 or 2))
        {
            warn?.Invoke($"Unsupported schema {version}; using defaults without interpreting future fields.");
            return ReadDefaults(SerializeGameplaySettings(defaults));
        }
        if (version == 1) MigrateV1(root);
        JsonObject baseline = JsonSerializer.SerializeToNode(defaults, Options)!.AsObject();
        JsonObject merged = Merge(baseline, root, "", warn).AsObject();
        merged["schemaVersion"] = 2;
        // Collections have fixed semantic identities, not arbitrary array positions.
        JsonArray entries = new();
        JsonArray? supplied = root["enchantments"] as JsonArray;
        foreach (JsonNode? template in baseline["enchantments"]!.AsArray())
        {
            string id = template!["id"]!.GetValue<string>();
            JsonObject? source = supplied?.OfType<JsonObject>().FirstOrDefault(e =>
                string.Equals(ReadString(e["id"]), id, StringComparison.OrdinalIgnoreCase));
            entries.Add(Merge(template, source, "enchantments." + id, warn));
        }
        if (supplied is not null)
            foreach (JsonObject item in supplied.OfType<JsonObject>())
                if (!defaults.Enchantments.Any(e => e.Id.Equals(ReadString(item["id"]), StringComparison.OrdinalIgnoreCase)))
                    warn?.Invoke("Unknown enchantment ignored: " + ReadString(item["id"]));
        merged["enchantments"] = entries;
        GameplayConfig value = ReadDefaults(merged.ToJsonString());
        Validate(value, defaults, warn);
        return value;
    }

    private static void MigrateV1(JsonObject root)
    {
        if (root["enchantments"] is JsonArray entries)
            foreach (JsonObject entry in entries.OfType<JsonObject>())
            {
                JsonObject levels = new();
                foreach (string level in Levels)
                    levels[level] = new JsonObject
                    {
                        ["enabled"] = (entry["availableLevels"] as JsonArray)?.Any(n => ReadString(n) == level) == true,
                        // Absent levels use their packaged v2 default amount during merge.
                        ["amount"] = entry["amountByLevel"]?[level]?.DeepClone(),
                    };
                entry["levels"] = levels;
            }
        root["schemaVersion"] = 2;
    }

    private static JsonNode Merge(JsonNode template, JsonNode? supplied, string path, Action<string>? warn)
    {
        if (path.EndsWith(".levelWeights", StringComparison.Ordinal) && supplied is JsonObject probabilities)
        {
            JsonObject result = new();
            foreach (string level in Levels)
            {
                JsonNode fallback = template[level]?.DeepClone() ?? JsonValue.Create(0f)!;
                result[level] = Merge(fallback, probabilities[level] ?? JsonValue.Create(0f), path + "." + level, warn);
            }
            return result;
        }
        if (template is JsonObject obj)
        {
            JsonObject result = new();
            foreach ((string key, JsonNode? child) in obj)
            {
                JsonNode? incoming = (supplied as JsonObject)?[key];
                result[key] = child is null
                    ? (incoming is JsonValue nullableValue && nullableValue.TryGetValue<int>(out int nullableInt) ? JsonValue.Create(nullableInt) : null)
                    : Merge(child, incoming, path + "." + key, warn);
            }
            return result;
        }
        if (template is JsonArray array)
        {
            if (supplied is not JsonArray input || input.Count == 0) return array.DeepClone();
            JsonArray result = new();
            for (int i = 0; i < input.Count; i++)
                result.Add(Merge(array[Math.Min(i, array.Count - 1)]!, input[i], path + $"[{i}]", warn));
            return result;
        }
        if (supplied is null) return template.DeepClone();
        if (template.GetValueKind() != supplied.GetValueKind())
        {
            // true/false have distinct JsonValueKind values but are both booleans.
            bool boolean = template.GetValueKind() is JsonValueKind.True or JsonValueKind.False &&
                supplied.GetValueKind() is JsonValueKind.True or JsonValueKind.False;
            if (!boolean) { warn?.Invoke(path + ": wrong type; default restored."); return template.DeepClone(); }
        }
        if (template.GetValueKind() == JsonValueKind.Number)
        {
            bool integral = path.EndsWith(".amount", StringComparison.Ordinal) ||
                path.EndsWith("BookCount", StringComparison.Ordinal) || path.StartsWith(".bookGain", StringComparison.Ordinal);
            if (!double.TryParse(supplied.ToJsonString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double n) || !double.IsFinite(n) ||
                Math.Abs(n) > 1000000 || (integral && n != Math.Truncate(n)))
            { warn?.Invoke(path + ": invalid number; default restored."); return template.DeepClone(); }
        }
        return supplied.DeepClone();
    }

    private static void Validate(GameplayConfig c, GameplayConfig d, Action<string>? warn)
    {
        int Number(int n, int fallback, int min, int max, string key)
        { if (n >= min && n <= max) return n; warn?.Invoke(key + ": out of range; default restored."); return fallback; }
        float Weight(float n, float fallback, string key)
        { if (float.IsFinite(n) && n >= 0 && n <= 1000) return n; warn?.Invoke(key + ": invalid weight; default restored."); return fallback; }
        var b = c.BookGain; var db = d.BookGain;
        b.Act1NormalCombatsPerReward = Number(b.Act1NormalCombatsPerReward, db.Act1NormalCombatsPerReward, 1, 20, "Act1");
        b.Act2NormalCombatsPerReward = Number(b.Act2NormalCombatsPerReward, db.Act2NormalCombatsPerReward, 1, 20, "Act2");
        b.Act3NormalCombatsPerReward = Number(b.Act3NormalCombatsPerReward, db.Act3NormalCombatsPerReward, 1, 20, "Act3");
        b.NormalCombatBookRewardAmount = Number(b.NormalCombatBookRewardAmount, db.NormalCombatBookRewardAmount, 0, 20, "Normal");
        b.EliteBookRewardAmount = Number(b.EliteBookRewardAmount, db.EliteBookRewardAmount, 0, 20, "Elite");
        b.BossBookRewardAmount = Number(b.BossBookRewardAmount, db.BossBookRewardAmount, 0, 20, "Boss");
        foreach (var (e, de) in new[] { (c.Campfire, d.Campfire), (c.Ancient, d.Ancient) })
            if (e.HealPercent < 0 || e.HealPercent > 100) { e.HealPercent = de.HealPercent; warn?.Invoke("HealPercent: default restored."); }
        for (int i = 0; i < c.Enchantments.Count; i++)
        {
            var e = c.Enchantments[i]; var de = d.Enchantments[i];
            e.Id = de.Id;
            e.BaseWeight = Weight(e.BaseWeight, de.BaseWeight, e.Id);
            e.IconPath = de.IconPath;
            foreach (string level in Levels)
                e.Levels[level].Amount = Number(e.Levels[level].Amount, de.Levels[level].Amount, 1, 999, e.Id + "." + level);
        }
        var r = c.CandidateGeneration.RepeatWeights; var dr = d.CandidateGeneration.RepeatWeights;
        // The first occurrence is the base weight by definition; exposing a common multiplier has no effect.
        r.FirstOccurrence = 1f;
        r.SecondOccurrence = Weight(r.SecondOccurrence, dr.SecondOccurrence, "Repeat.second");
        r.ThirdAndLaterOccurrence = Weight(r.ThirdAndLaterOccurrence, dr.ThirdAndLaterOccurrence, "Repeat.third");
        var bands = c.CandidateGeneration.BookCountBands;
        bool continuous = bands.Count > 0 && bands[0].MinBookCount == 0;
        for (int i = 0; i < bands.Count; i++)
        {
            var band = bands[i];
            continuous &= band.MinBookCount >= 0 && (band.MaxBookCount is null || band.MaxBookCount >= band.MinBookCount);
            continuous &= i == bands.Count - 1 ? band.MaxBookCount is null : band.MaxBookCount + 1 == bands[i + 1].MinBookCount;
            var fallback = d.CandidateGeneration.BookCountBands[Math.Min(i, d.CandidateGeneration.BookCountBands.Count - 1)];
            if (band.Slots.Count is < 1 or > 3) { band.Slots = fallback.Slots; warn?.Invoke("Invalid slot count; default restored."); }
            for (int s = 0; s < band.Slots.Count; s++)
            {
                var weights = band.Slots[s].LevelWeights;
                float sum = weights.Values.Sum();
                if (weights.Count == 0 || weights.Any(p => !Levels.Contains(p.Key) || !float.IsFinite(p.Value) || p.Value < 0) || sum <= 0)
                { band.Slots[s] = fallback.Slots[Math.Min(s, fallback.Slots.Count - 1)]; warn?.Invoke("Invalid slot probability; default restored."); }
                else
                {
                    if (Math.Abs(sum - 1) > 0.00001f) warn?.Invoke("Slot probabilities normalized to 1.");
                    foreach (string key in weights.Keys.ToArray()) weights[key] /= sum;
                }
            }
        }
        if (!continuous) { c.CandidateGeneration.BookCountBands = d.CandidateGeneration.BookCountBands; warn?.Invoke("Book bands are not contiguous; default bands restored (other settings retained)."); }
    }

    private static int ReadInt(JsonNode? node, int fallback) => node is JsonValue v && v.TryGetValue<int>(out int n) ? n : fallback;
    private static string? ReadString(JsonNode? node) => node is JsonValue v && v.TryGetValue<string>(out string? s) ? s : null;
    private static JsonNode? Canonicalize(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new KeyValuePair<string, JsonNode?>(p.Key, Canonicalize(p.Value)))),
        JsonArray array => new JsonArray(array.Select(Canonicalize).ToArray()),
        _ => node?.DeepClone(),
    };
}
