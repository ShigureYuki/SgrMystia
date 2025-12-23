using MemoryPack;

namespace MetaMystia;

[MemoryPackable]
public partial class NightSyncAction : NetAction
{
    public override ActionType Type => ActionType.NIGHTSYNC;
    public float Vx {get; set; }
    public float Vy {get; set; }
    public float Px {get; set; }
    public float Py {get; set; }

    public override void LogActionSend(bool onlyAction, string prefix)
    {
        LogActionSend(BepInEx.Logging.LogLevel.Debug, onlyAction, prefix);
    }
    public override void OnReceived()
    {
        LogActionReceived(BepInEx.Logging.LogLevel.Debug);
        if (PluginManager.CurrentGameScene != Common.UI.Scene.WorkScene)
        {
            return;
        }
        PluginManager.Instance.RunOnMainThread(() =>
            KyoukoManager.NightSyncFromPeer(new UnityEngine.Vector2(Vx, Vy), new UnityEngine.Vector2(Px, Py)));
    }
}
