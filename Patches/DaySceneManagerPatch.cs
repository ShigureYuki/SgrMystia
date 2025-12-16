using HarmonyLib;
using Common.UI;

namespace MetaMystia;


[HarmonyPatch(typeof(DayScene.SceneManager))]
public class DaySceneManagerPatch : PatchBase<DaySceneManagerPatch>
{
    [HarmonyPatch(nameof(DayScene.SceneManager.Awake))]
    [HarmonyPostfix]
    public static void Awake_Postfix()
    {
        PluginManager.CurrentGameScene = Scene.DayScene;
        MystiaManager.Instance.Initialize();
        KyoukoManager.Initialize();
        Log.LogInfo($"{LOG_TAG} CurrentGameStage switched to DayScene");
    }

    public static bool _skipPatchOnDayOver = false;

    [HarmonyPatch(nameof(DayScene.SceneManager.OnDayOver))]
    [HarmonyPrefix]
    public static bool OnDayOver_Prefix()
    {
        if (_skipPatchOnDayOver)
        {
            Log.LogDebug($"{LOG_TAG} skipPatch is true, skipping prefix");
            return true;
        }
        if (!MpManager.Instance.IsConnected)
        {
            Log.LogDebug($"{LOG_TAG} Not in multiplayer session, skipping prefix");
            return true; // Not in multiplayer session, proceed as normal
        }
        /*
            [MetaMiku 注]
            状态机: 
                由 01 表示 Mystia 未准备好，而 Kyouko 已准备好
                
                                  00
                               ↙    ↘
                             10        01
                               ↘    ↙
                                  11
    
                00->10, 01->11 为游戏原生触发
                    00->10: 需要跳过 OnDayOver 而显示「准备未完成」对话框
                    01->11: 需要先跳过 OnDayOver 后在显示「准备完成」对话框的回调中执行 OnDayOver
                00->01, 10->11 为 MpManager 触发
                    00->01: 正常更新状态，不触发 OnDayOver_Prefix 也不执行 OnDayOver
                    10->11: 需要先显示「准备完成」对话框再在其回调中执行 OnDayOver
            
            BUG & TODO: 解决当游戏原有结束剧情时的错误处理
        */

        Log.LogDebug($"{LOG_TAG} Day over detected");

        // 00 -> 10
        if (!MystiaManager.IsReady && !KyoukoManager.IsReady)
        {
            Log.LogInfo($"{LOG_TAG} Both Mystia and Kyouko are not ready -> Mystia is ready => show **not ready** dialog");
            MpManager.Instance.SendReady();
            MystiaManager.IsReady = true;
            DialogManager.ShowReadyDialog(false);
            return false;
        }

        // 01 -> 11
        if (!MystiaManager.IsReady && KyoukoManager.IsReady) 
        {
            Log.LogInfo($"{LOG_TAG} Mystia is not ready but Kyouko is ready -> both are ready => show **ready** dialog");
            MpManager.Instance.SendReady();
            MystiaManager.IsReady = true;
            DialogManager.ShowReadyDialog(true, () => 
            {
                _skipPatchOnDayOver = true;
                DayScene.SceneManager.Instance.OnDayOver();
                _skipPatchOnDayOver = false;
            });
            return false;
        }

        // 00 -> 01: Not here -> MultiplayerManager handles it

        // 10 -> 11: Not here -> MultiplayerManager handles it

        // 11: 回调中直接执行 OnDayOver
        return true;
    }
}