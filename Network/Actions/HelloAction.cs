using MemoryPack;

namespace MetaMystia;

[MemoryPackable]
public partial class HelloAction : NetAction
{
    public override ActionType Type => ActionType.HELLO;
    public string PeerId { get; set; } = "";
    public override void OnReceived()
    {
        Plugin.Instance.Log.LogInfo($"Received HELLO: {PeerId}");
        MpManager.Instance.PeerId = PeerId;
    }
}
