using HarmonyLib;
using MCEnchantingTable.MCEnchantingTableCode.Rules;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace MCEnchantingTable.MCEnchantingTableCode.Patches;

[HarmonyPatch]
internal static class BookRulesMultiplayerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
    private static void CaptureHostSettings()
    {
        BookRulesSession.PendingNewRunSnapshot = BookRulesSnapshot.FromGlobalSettings();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Serialize))]
    private static void WriteHostSettings(PacketWriter writer)
    {
        BookRulesSnapshot snapshot = BookRulesSession.PendingNewRunSnapshot
            ?? BookRulesSnapshot.FromGlobalSettings();
        BookRulesSession.PendingNewRunSnapshot = snapshot;
        snapshot.Write(writer);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Deserialize))]
    private static void ReadHostSettings(PacketReader reader)
    {
        BookRulesSession.PendingNewRunSnapshot = BookRulesSnapshot.Read(reader);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
    private static void ClearPendingSnapshot()
    {
        BookRulesSession.Clear();
    }
}
