using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting;

/// <summary>
/// Candidate rolls owned by one Rest Site or Ancient enchanting opportunity.
/// The cache contains immutable candidate data only; it never owns UI or model instances.
/// </summary>
internal sealed class EnchantSession
{
    private readonly Dictionary<uint, IReadOnlyList<MCEnchantmentCandidate>> _candidateCache = [];

    public IReadOnlyList<MCEnchantmentCandidate> GetOrCreateCandidates(
        CardModel card,
        Func<IReadOnlyList<MCEnchantmentCandidate>> generate)
    {
        uint deckIndex = NetDeckCard.FromModel(card).DeckIndex;
        if (_candidateCache.TryGetValue(deckIndex, out IReadOnlyList<MCEnchantmentCandidate>? cached))
        {
            return cached;
        }

        IReadOnlyList<MCEnchantmentCandidate> candidates = generate().ToArray();
        _candidateCache.Add(deckIndex, candidates);
        return candidates;
    }

    public void Clear()
    {
        _candidateCache.Clear();
    }
}
