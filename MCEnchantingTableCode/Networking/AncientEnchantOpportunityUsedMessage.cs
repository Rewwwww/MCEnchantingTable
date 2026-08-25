using BaseLib.Abstracts;
using MCEnchantingTable.MCEnchantingTableCode.Ancient;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace MCEnchantingTable.MCEnchantingTableCode.Networking;

public sealed class AncientEnchantOpportunityUsedMessage : ICustomTargetedMessage
{
    public string EncounterKey { get; set; } = string.Empty;

    public string AncientId { get; set; } = string.Empty;

    public RunLocation LocationValue { get; set; }

    public bool IsRewardMessage => false;

    public RunLocation Location => LocationValue;

    public bool ShouldBroadcast => true;

    public void HandleMessage(ulong senderId)
    {
        AncientEnchantController.ApplyRemoteUse(senderId, AncientId, EncounterKey);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(EncounterKey);
        writer.WriteString(AncientId);
        writer.Write(LocationValue);
    }

    public void Deserialize(PacketReader reader)
    {
        EncounterKey = reader.ReadString();
        AncientId = reader.ReadString();
        LocationValue = reader.Read<RunLocation>();
    }
}
