using Il2CppInterop.Runtime.Injection;
using MemoryPack;
using SgrYuki.Utils;

namespace MetaMystia;

[MemoryPackable]
[AutoLog]
public partial class IzakayaCloseAction : NetAction
{
    public override ActionType Type => ActionType.IZAKAYA_CLOSE;
    public override void OnReceived()
    {
        LogActionReceived();
        if (NightScene.GuestManagementUtility.GuestsManager.Instance != null && MpManager.LocalScene == Common.UI.Scene.WorkScene)
        {
            // Do not let client call TryCloseIzakaya, some problem will happen like the client may not be able to close izakaya successfully.
            static void onClient() { 
                if (NightGuestManager.WorkTimeLeft > 0)
                {
                    NightGuestManager.ModifyWorkTimeLeft(1); 
                }
            }
            // static void onServer() { NightGuestManager.ModifyWorkTimeLeft(0); GuestsManagerPatch.TryCloseIzakaya_Original(NightScene.GuestManagementUtility.GuestsManager.Instance); }
            static void onServer() => onClient();
            if (MpManager.IsConnected && !NightScene.GuestManagementUtility.GuestsManager.Instance.isIzakayaClosing)
            {
                Notify.ShowOnMainThread("对方已经打烊啦！");
                CommandScheduler.Enqueue(
                    executeWhen: () => true, 
                    executeInfo: "Peer izakaya closed",
                    execute: MpManager.IsClient ? onClient : onServer
                );
            } 
        }
    }

    public static void Send()
    {
        NetPacket packet = new([new IzakayaCloseAction()]);
        SendToPeer(packet);
    }
}
