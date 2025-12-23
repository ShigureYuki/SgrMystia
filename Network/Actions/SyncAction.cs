using MemoryPack;

namespace MetaMystia;

[MemoryPackable]
public partial class SyncAction : NetAction
{
    public override ActionType Type => ActionType.SYNC;
    public float Vx {get; set; }
    public float Vy {get; set; }
    public float Px {get; set; }
    public float Py {get; set; }
    public bool IsSprinting{get; set;}
    public string MapLabel {get; set; }

    public override void LogActionSend(bool onlyAction, string prefix)
    {
        LogActionSend(BepInEx.Logging.LogLevel.Debug, onlyAction, prefix);
    }

    public override void OnReceived()
    {
        LogActionReceived(BepInEx.Logging.LogLevel.Debug);
        if (PluginManager.CurrentGameScene != Common.UI.Scene.DayScene)
        {
            return;
        }
        PluginManager.Instance.RunOnMainThread(() =>
            KyoukoManager.SyncFromPeer(MapLabel, IsSprinting,
                new UnityEngine.Vector2(Vx, Vy), new UnityEngine.Vector2(Px, Py)));
    }
}
