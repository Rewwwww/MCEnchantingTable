using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting;

/// <summary>
/// Candidate rolls owned by one Rest Site or Ancient enchanting opportunity.
/// The cache contains immutable candidate data only; it never owns UI or model instances.
/// </summary>
internal sealed class EnchantSession
{
    private readonly Dictionary<uint, IReadOnlyList<MCEnchantmentCandidate>> _candidateCache = [];
    private string? _encounterKey;
    private ulong _encounterSeed;

    public void Configure(Player player, string encounterKey)
    {
        if (string.Equals(_encounterKey, encounterKey, StringComparison.Ordinal))
        {
            return;
        }

        _candidateCache.Clear();
        _encounterKey = encounterKey;
        _encounterSeed = unchecked((ulong)(
            (long)player.RunState.Rng.Seed +
            player.RunState.GetPlayerSlotIndex(player) +
            (long)StringHelper.GetDeterministicHashCode(
                $"MCEnchantingTable:EnchantSession:{encounterKey}")));
    }

    public IReadOnlyList<MCEnchantmentCandidate> GetOrCreateCandidates(
        CardModel card,
        Func<Rng, IReadOnlyList<MCEnchantmentCandidate>> generate)
    {
        uint deckIndex = NetDeckCard.FromModel(card).DeckIndex;
        if (_candidateCache.TryGetValue(deckIndex, out IReadOnlyList<MCEnchantmentCandidate>? cached))
        {
            return cached;
        }

        if (_encounterKey is null)
        {
            throw new InvalidOperationException("EnchantSession must be configured before generating candidates.");
        }

        Rng cardRng = new(unchecked(_encounterSeed + deckIndex));
        IReadOnlyList<MCEnchantmentCandidate> candidates = generate(cardRng).ToArray();
        _candidateCache.Add(deckIndex, candidates);
        return candidates;
    }

    public void Clear()
    {
        _candidateCache.Clear();
    }
}
