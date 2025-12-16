using HarmonyLib;
using Common.UI;
using StaffScene;

namespace MetaMystia;


[HarmonyPatch(typeof(StaffScene.SceneManager))]
[HarmonyPatch]
public class StaffSceneManagerPatch : PatchBase<StaffSceneManagerPatch>
{
    [HarmonyPatch(nameof(SceneManager.Awake))]
    [HarmonyPostfix]
    public static void StaffScene_Start_Postfix()
    {
        PluginManager.CurrentGameScene = Scene.StaffScene;
        Log.LogInfo($"{LOG_TAG} CurrentGameStage switched to StaffScene");
    }
}