using BepInEx.Logging;
using Common.UI;

namespace MetaMystia;

[AutoLog]
public static partial class Notify
{

    // Note: should run on main thread
    public static void Show(string text)
    {
        ReceivedObjectDisplayerController.Instance.NotifyTextMessage(text);   
    }

    public static void ShowOnMainThread(string text)
    {
        PluginManager.Instance.RunOnMainThread(() => 
        {
            Show(text);
        });
    }
}