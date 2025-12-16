
using BepInEx.Logging;

namespace MetaMystia;

public class PatchBase<TChild>
{
    protected static ManualLogSource Log => Plugin.Instance.Log;
    protected static readonly string LOG_TAG = $"[{typeof(TChild).Name}]";
}