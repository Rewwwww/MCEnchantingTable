using Godot;
using MCEnchantingTable.MCEnchantingTableCode.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MCEnchantingTable.MCEnchantingTableCode.Ancient;

namespace MCEnchantingTable.MCEnchantingTableCode.Enchanting;

/// <summary>
/// Shared entry point for Ancient and Rest Site enchanting flows.
/// </summary>
internal static class EnchantController
{
    public static Task<bool> CommitAncientEnchant(AncientEventModel ancient) =>
        AncientEnchantController.CommitEnchant(ancient);

    public static async Task<bool> TryApplyCardEnchant(
        CardModel card,
        MCEnchantmentCandidate candidate,
        Func<bool>? validateOpportunity = null,
        Func<Task<bool>>? commitOpportunity = null,
        Func<Task>? afterEnchantApplied = null)
    {
        try
        {
            EnchantmentModel canonical =
                ModelDb.GetById<EnchantmentModel>(candidate.EnchantmentModelId);
            if (!canonical.CanEnchant(card))
            {
                MainFile.Logger.Warn(
                    $"Enchant confirmation rejected: card={card.Id}, " +
                    $"enchantment={candidate.EnchantmentModelId}, amount={candidate.Amount}. " +
                    "CanEnchant returned false.");
                return false;
            }

            if (validateOpportunity is not null && !validateOpportunity())
            {
                MainFile.Logger.Warn(
                    $"Enchant confirmation rejected because the opportunity is no longer available: " +
                    $"card={card.Id}, enchantment={candidate.EnchantmentModelId}, amount={candidate.Amount}.");
                return false;
            }

            EnchantmentModel mutable = canonical.ToMutable();
            EnchantmentModel? applied = CardCmd.Enchant(
                mutable,
                card,
                candidate.Amount);
            if (applied is null)
            {
                MainFile.Logger.Error(
                    $"CardCmd.Enchant returned null: card={card.Id}, " +
                    $"enchantment={candidate.EnchantmentModelId}, amount={candidate.Amount}.");
                return false;
            }

            if (commitOpportunity is not null && !await commitOpportunity())
            {
                MainFile.Logger.Error(
                    $"Enchant opportunity commit failed after CardCmd.Enchant: card={card.Id}, " +
                    $"enchantment={candidate.EnchantmentModelId}, amount={candidate.Amount}.");
                return false;
            }

            if (afterEnchantApplied is not null)
            {
                try
                {
                    await afterEnchantApplied();
                }
                catch (Exception exception)
                {
                    // An entry-specific secondary effect must not turn an
                    // already-applied enchantment into a gameplay failure.
                    MainFile.Logger.Warn(
                        "Post-enchant effect failed after CardCmd.Enchant succeeded: " + exception);
                }
            }

            PlayEnchantSuccessVfx(card);
            PlayEnchantSuccessSound();
            return true;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error(
                $"CardCmd.Enchant failed: card={card.Id}, " +
                $"enchantment={candidate.EnchantmentModelId}, amount={candidate.Amount}. " +
                exception);
            return false;
        }
    }

    private static void PlayEnchantSuccessVfx(CardModel card)
    {
        try
        {
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx is not null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
        catch (Exception exception)
        {
            // Visual feedback must not turn an already-applied enchantment into
            // an apparent gameplay failure.
            MainFile.Logger.Warn("Failed to play native enchant VFX: " + exception);
        }
    }

    private static void PlayEnchantSuccessSound()
    {
        try
        {
            NDebugAudioManager? audioManager = NDebugAudioManager.Instance;
            IReadOnlyList<string> soundPaths =
                MCEnchantingTableAssets.AudioAssets.EnchantConfirmSounds;
            if (audioManager is null || soundPaths.Count == 0)
            {
                return;
            }

            string path = soundPaths[Rng.Chaotic.NextInt(soundPaths.Count)];
            AudioStream? stream = ResourceLoader.Load<AudioStream>(path);
            if (stream is null)
            {
                MainFile.Logger.Warn($"Unable to load enchant success sound: {path}");
                return;
            }

            AudioStreamPlayer player = new()
            {
                Name = "MCEnchantingTable-EnchantSuccessSound",
                Stream = stream,
                Bus = new StringName("SFX"),
            };
            player.Finished += player.QueueFree;
            audioManager.AddChildSafely(player);
            player.Play();
        }
        catch (Exception exception)
        {
            // Audio feedback must never turn an already-applied enchantment into
            // an apparent gameplay failure.
            MainFile.Logger.Warn("Failed to play enchant success sound: " + exception);
        }
    }

}
