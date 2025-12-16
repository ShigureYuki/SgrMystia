using BepInEx.Logging;
using DEYU.Utils;
using Il2CppSystem.IO;
using NightScene.CookingUtility;
using MetaMystia;

public static class CookManager
{
    
    private static ManualLogSource Log => Plugin.Instance.Log;
    private static readonly string LOG_TAG = "[CookManager.cs]";

    public static CookController GetCookerControllerByIndex(int gridIndex)
    {
        // TODO: optimize
        Log.LogDebug($"{LOG_TAG} GetCookerControllerByIndex called");
        var AllCookerControllers = CookSystemManager.Instance.AllCookerControllers; // IEnumerable<CookController>
        foreach (var cookerController in AllCookerControllers.ToArray())
        {
            if (cookerController.GridIndex == gridIndex)
            {
                return cookerController;
            }
        }
        return null;
    }
}