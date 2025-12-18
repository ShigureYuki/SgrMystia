using MemoryPack;
using Common.UI;

namespace MetaMystia;

[MemoryPackable]
public partial class SelectAction : NetAction
{
    public override ActionType Type => ActionType.SELECT;
    public string MapLabel { get; set; } = "";
    public int Level { get; set; } = 0;
    public override void OnReceived()
    {
        // 接收 SELECT 包，缓存并展示「提示」对话
        Plugin.Instance.Log.LogInfo($"Received SELECT: {MapLabel}, {Level}");
        PluginManager.Instance.RunOnMainThread(() =>
        {
            KyoukoManager.IzakayaMapLabel = MapLabel;
            KyoukoManager.IzakayaLevel = Level;

            Dialog.ShowInformDialog(KyoukoManager.IzakayaMapLabel, null);
        });
    }
}
