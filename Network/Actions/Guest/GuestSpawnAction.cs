using MemoryPack;
using SgrYuki.Utils;
using static MetaMystia.NightGuestManager;

namespace MetaMystia;


[MemoryPackable]
public partial class GuestSpawnAction : NetAction
{
    public override ActionType Type => ActionType.GUEST_SPAWN;

    [MemoryPackAllowSerialize]
    public GuestInfo GuestInfo;
    public string UUID { get; set; }

    public override void LogActionSend(bool onlyAction, string prefix)
    {
        LogActionSend(BepInEx.Logging.LogLevel.Info, false, prefix);
    }

    public override void OnReceived()
    {
        LogActionReceived();
        if (MpManager.LocalScene != Common.UI.Scene.WorkScene)
        {
            return;
        }

        CommandScheduler.Enqueue(
            executeWhen: () => !MpManager.InStory,
            executeInfo: $"Spawned: guid {UUID}, special {GuestInfo.IsSpecial}",
            execute: () =>
            {
                SetGuestStatus(UUID, Status.PendingGenerate);
                SpawnGuestGroup(GuestInfo, UUID);
            });
    }

    public static void Send(int guest, bool isSpecial, string uuid, int? guest1Visualid = null, int? guest2 = null, int? guest2Visualid = null)
    {
        NetPacket packet = new([new GuestSpawnAction
        {
            UUID = uuid,
            GuestInfo = new GuestInfo {
                Id = guest,
                VisualId = guest1Visualid,
                Id2 = guest2,
                VisualId2 = guest2Visualid,
                IsSpecial = isSpecial
            }
        }]);
        SendToPeer(packet);
    }

}

