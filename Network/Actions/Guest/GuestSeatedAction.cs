using MemoryPack;
using NightScene.GuestManagementUtility;
using SgrYuki.Utils;

namespace MetaMystia;

[MemoryPackable]
[AutoLog]
public partial class GuestSeatedAction : NetAction
{
    public override ActionType Type => ActionType.GUEST_SEATED;

    public string GuestUniqId { get; set; }

    public int DeskId { get; set; }

    public int SeatId { get; set; }

    public bool FirstSpawn { get; set; }

    public override void LogActionSend(bool _onlyAction, string prefix)
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

        void onExecute()
        {
            var guestTargetDesk = GuestsManager.instance.GetInDeskGuest(DeskId);
            if (guestTargetDesk != null)
            {
                Log.LogWarning($"guestTargetDesk not null, will force set previous guest out: guid {GuestUniqId}, DeskId {DeskId}, SeatId {SeatId}");
                GuestsManager.instance.SetGuestOutDesk(guestTargetDesk);
            }
            var guest = NightGuestManager.GetGuest(GuestUniqId);

            NightGuestManager.SetGuestDeskcodeSeat(GuestUniqId, SeatId);

            GuestsManagerPatch.TrySendToSeat_Original(GuestsManager.instance, guest, FirstSpawn, DeskId, true);

            NightGuestManager.SetGuestStatus(GuestUniqId, NightGuestManager.Status.Seated);
            NightGuestManager.SetGuestDeskcode(GuestUniqId, guest.DeskCode);
        }
        CommandScheduler.Enqueue(
            executeWhen: () =>
            {
                if (NightGuestManager.CheckStatusOrThrow(GuestUniqId, NightGuestManager.Status.Generated))
                {
                    if(GuestsManager.instance.GetInDeskGuest(DeskId) == null)
                    {
                        return true;
                    }
                    if (!GuestsManager.instance.CheckGuestIsInDesk(DeskId))
                    {
                        return true;
                    }
                    var guestTargetDesk = NightGuestManager.GetGuestUUID(GuestsManager.instance.GetInDeskGuest(DeskId));
                    return NightGuestManager.GetGuestDeskcodeSeat(guestTargetDesk) != DeskId + 1;
                }
                return false;
            },
            executeInfo: $"Seated: guid {GuestUniqId}, DeskId {DeskId}, SeatId {SeatId}",
            execute: onExecute,
            timeoutSeconds: 30,
            onTimeout: () =>
            {
                Log.LogWarning($"Seated time-out, will force set previous guest out: guid {GuestUniqId}, DeskId {DeskId}, SeatId {SeatId}");
                GuestsManager.instance.SetGuestOutDesk(GuestsManager.instance.GetInDeskGuest(DeskId));
                onExecute();
            }
        );
    }

    public static void Send(string guestUniqId, int deskId, bool firstSpawn, int seatId)
    {
        NetPacket packet = new([new GuestSeatedAction
        {
            GuestUniqId = guestUniqId,
            DeskId = deskId,
            FirstSpawn = firstSpawn,
            SeatId = seatId
        }]);
        SendToPeer(packet);
    }
}

