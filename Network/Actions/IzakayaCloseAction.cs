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
            Notify.ShowOnMainThread("对方已经打烊啦！");
            if (MpManager.IsConnectedHost)
            {
                CommandScheduler.Enqueue(() => true, () =>
                {
                    GuestsManagerPatch.TryCloseIzakaya_Original(NightScene.GuestManagementUtility.GuestsManager.Instance);
                });
            } 
            // Do not let client call TryCloseIzakaya, some problem will happen like the client may not be able to close izakaya successfully.
        }
    }

    public static void Send()
    {
        NetPacket packet = new([new IzakayaCloseAction
        {
        }]);
        SendToPeer(packet);
    }
}
