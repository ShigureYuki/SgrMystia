using Common.UI;
using MemoryPack;

namespace MetaMystia;

[MemoryPackable]
public partial class HelloAction : NetAction
{
    public override ActionType Type => ActionType.HELLO;
    public string PeerId { get; set; } = "";
    public string Version { get; set; } = "";
    public Scene CurrentGameScene { get; set; }

    public override void OnReceived()
    {
        LogActionReceived();
        MpManager.PeerId = PeerId;

        if (Version != MyPluginInfo.PLUGIN_VERSION)
        {
            Plugin.Instance.Log.LogError($"[HelloAction] Version mismatch! Local: {MyPluginInfo.PLUGIN_VERSION}, Remote: {Version}");
            MpManager.DisconnectPeer();
            Notify.ShowOnMainThread("版本不匹配，连接已断开");
            return;
        }

        var bothInDay = CurrentGameScene == Scene.DayScene && MpManager.LocalScene == Scene.DayScene;
        var bothInMain = CurrentGameScene == Scene.MainScene && MpManager.LocalScene == Scene.MainScene;
        if (!bothInDay && !bothInMain)
        {
            Plugin.Instance.Log.LogError($"[HelloAction] Scene mismatch! Local: {MpManager.LocalScene}, Remote: {CurrentGameScene}");
            MpManager.DisconnectPeer();
            Notify.ShowOnMainThread("有玩家不处于白天或主界面，连接已断开");
            return;
        }
    }
}
