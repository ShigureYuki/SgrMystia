using HarmonyLib;
using Common.UI;
using PrepNightScene;

namespace MetaMystia;


[HarmonyPatch(typeof(PrepNightScene.SceneManager))]
public class PrepNightSceneManagerPatch : PatchBase<PrepNightSceneManagerPatch>
{
    
    [HarmonyPatch(nameof(SceneManager.Start))]
    [HarmonyPostfix]
    public static void PrepNightScene_Start_Postfix()
    {
        PluginManager.CurrentGameScene = Scene.IzakayaPrepScene;
        PrepSceneManager.init();
        Log.LogInfo($"{LOG_TAG} CurrentGameStage switched to IzakayaPrepScene");
        
    }
}