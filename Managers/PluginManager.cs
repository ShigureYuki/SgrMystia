using BepInEx.Logging;
using UnityEngine;
using System;
using Il2CppInterop.Runtime;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Common.UI;
using GameData.Core.Collections.NightSceneUtility;
using UnityEngine.Rendering;
using HarmonyLib;
using System.Linq;
using Cysharp.Threading.Tasks;
using DEYU.AssetHandleUtility;
using DEYU.Utils;
using System.Threading.Tasks;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace MetaMystia;
public class PluginManager : MonoBehaviour
{
    public static PluginManager Instance { get; private set; }
    private static ManualLogSource Log => Plugin.Instance.Log;
    private static readonly string LOG_TAG = "[PluginManager.cs]";
    private readonly string label = $"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded";
    public static InGameConsole Console { get; private set; }
    private bool isTextVisible = true;
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
    private readonly List<(Action action, Func<bool> condition)> _conditionalActions = new List<(Action, Func<bool>)>();
    public static Scene CurrentGameScene { get; set; } = Scene.MainScene;
    public static bool test = false;

    public PluginManager(IntPtr ptr) : base(ptr)
    {
        if (Instance != null)
        {
            Log.LogWarning($"{LOG_TAG} Another instance of PluginManager already exists! Destroying this one.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    internal static GameObject Create(string name)
    {
        var gameObject = new GameObject(name);
        DontDestroyOnLoad(gameObject);

        gameObject.AddComponent(Il2CppType.Of<PluginManager>());

        return gameObject;
    }

    private void Awake()
    {
        Console = new InGameConsole();
        // MpManager.Start();
    }

    private void OnGUI()
    {
        Console?.OnGUI();

        if (isTextVisible)
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine(label);
            info.AppendLine(MpManager.GetBriefStatus());
            GUI.Label(new Rect(10, Screen.height - 50, 600, 50), info.ToString());
        }
    }

    private void Update()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action();
                // Log.LogDebug($"{LOG_TAG} Successfully executed action on main thread");
            }
            catch (Exception e)
            {
                Log.LogError($"{LOG_TAG} Error executing on main thread: {e.Message}\n{e.StackTrace}");
            }
        }

        Console?.Update();

        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            Log.LogInfo($"\n");
        }
        if (Input.GetKeyDown(KeyCode.Backslash)) 
        {
            isTextVisible = !isTextVisible;
            Log.LogMessage($"{LOG_TAG} Toggled text visibility: " + isTextVisible);
        }

        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            MpManager.Start();
        }
        if (Input.GetKeyDown(KeyCode.KeypadDivide))
        {
            MpManager.ConnectToPeer("TX_VOLCANO", 40815); // 这是 MetaMiku 的 PC，测试用
        }



        if (Input.GetKeyDown(KeyCode.F1) && false)
        {
            var specialGuests = GameData.Core.Collections.CharacterUtility.DataBaseCharacter.SpecialGuest;
            if (specialGuests == null || specialGuests.Count == 0)
            {
                Log.LogError($"{LOG_TAG} DataBaseCharacter.SpecialGuest is null or empty!");
                return;
            }
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SpecialGuest.WeightedTag> likeFoodTag = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SpecialGuest.WeightedTag>(5);
            likeFoodTag[0] = new SpecialGuest.WeightedTag(7, 1);    // 清淡
            likeFoodTag[1] = new SpecialGuest.WeightedTag(17, 1);   // 甜
            likeFoodTag[2] = new SpecialGuest.WeightedTag(21, 1);   // 凉爽
            likeFoodTag[3] = new SpecialGuest.WeightedTag(28, 1);   // 小巧
            likeFoodTag[4] = new SpecialGuest.WeightedTag(31, 1);   // 果味
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SpecialGuest.WeightedTag> likeBevTag = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SpecialGuest.WeightedTag>(6);
            likeBevTag[0] = new SpecialGuest.WeightedTag(-1, 1);    // 无酒精
            likeBevTag[1] = new SpecialGuest.WeightedTag(0, 1);     // 低酒精
            likeBevTag[2] = new SpecialGuest.WeightedTag(3, 1);     // 可加冰
            likeBevTag[3] = new SpecialGuest.WeightedTag(11, 1);    // 直饮
            likeBevTag[4] = new SpecialGuest.WeightedTag(13, 1);    // 甘
            likeBevTag[5] = new SpecialGuest.WeightedTag(16, 1);    // 气泡 
            var specialGuest = new GameData.Core.Collections.NightSceneUtility.SpecialGuest(
                9000,
                "Daiyousei",
                new Vector2Int(300, 500),
                new int[] {22, 24}, // 灼热 猎奇 
                likeFoodTag,
                likeBevTag,
                specialGuests[0].Reaction, specialGuests[0].Destination, specialGuests[0].CommisionAreaLabel, specialGuests[0].characterKizunaLevel1Welcome, specialGuests[0].characterKizunaLevel2Welcome, specialGuests[0].characterKizunaLevel3Welcome, specialGuests[0].characterKizunaLevel4Welcome, specialGuests[0].characterKizunaLevel5Welcome, specialGuests[0].characterKizunaLevel1ChatData, specialGuests[0].characterKizunaLevel2ChatData, specialGuests[0].characterKizunaLevel3ChatData, specialGuests[0].characterKizunaLevel4ChatData, specialGuests[0].characterKizunaLevel5ChatData, specialGuests[0].characterKizunaLevel2InviteSucceed, specialGuests[0].characterKizunaLevel2InviteFailed, specialGuests[0].characterKizunaLevel3InviteSucceed, specialGuests[0].characterKizunaLevel3InviteFailed, specialGuests[0].characterKizunaLevel4InviteSucceed, specialGuests[0].characterKizunaLevel4InviteFailed, specialGuests[0].characterKizunaLevel5InviteSucceed, specialGuests[0].characterKizunaLevel3RequestIngerdient, specialGuests[0].characterKizunaLevel4RequestIngerdient, specialGuests[0].characterKizunaLevel5RequestIngerdient, specialGuests[0].characterKizunaLevel4RequestBeverage, specialGuests[0].characterKizunaLevel5RequestBeverage, specialGuests[0].characterKizunaLevel5Commision, specialGuests[0].characterKizunaLevel5CommisionFinish, specialGuests[0].hideInAlbum, specialGuests[0].IsParticular, specialGuests[0].IsCollabCharacter, specialGuests[0].SpawnType, specialGuests[0].unifiedSpawnExclusion, specialGuests[0].unifiedSpawnWhereAfterEventOrMission, specialGuests[0].unifiedSpawnProb, specialGuests[0].m_SpecialGuestExtraDialogDataAsset, specialGuests[0].doNotShowInNightByDefault, specialGuests[0].doNotShowInChallenge, specialGuests[0].guestFoodEasterEggData);
            specialGuest.stringId = "Daiyousei";
            specialGuests[9000] = specialGuest;
            Log.LogWarning($"{LOG_TAG} Added/Updated special guest Daiyousei with ID 9000");
            Il2CppSystem.Collections.Generic.Dictionary<int, string> foodRequests = new Il2CppSystem.Collections.Generic.Dictionary<int, string>();
            foodRequests.Add(7, "那些我最爱的东西——水和风都是没有味道的。");    // 清淡
            foodRequests.Add(17, "甜味是这个世界上最本质、最美好的味道。");     // 甜 
            foodRequests.Add(21, "我最喜欢湖面微凉的清风流过的感觉了。");       // 凉爽
            foodRequests.Add(28, "整个世界的话，大概…只会比雾之湖大一点吧？");  // 小巧
            foodRequests.Add(31, "我们妖精偶尔会去湖畔果树林摘点果子吃。");     // 果味
            if (GameData.CoreLanguage.Collections.DataBaseLanguage.SpecialGuestFoodRequest == null)
            {
                Log.LogError($"{LOG_TAG} DataBaseLanguage.SpecialGuestFoodRequest is null!");
            }
            else
            {
                GameData.CoreLanguage.Collections.DataBaseLanguage.SpecialGuestFoodRequest[9000] = foodRequests;
            }
        }
        if (Input.GetKeyDown(KeyCode.F2) && false)
        {
            if (GameData.CoreLanguage.Collections.NightSceneLanguage.SpecialEvaluation == null)
            {
                Log.LogError($"{LOG_TAG} NightSceneLanguage.SpecialEvaluation is null! Please enter a night scene first.");
            }
            else
            {
                var evaluationLines = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(9);
                evaluationLines[0] = "如果只有这样的话……"; // 黑评
                evaluationLines[1] = "呃，一会去找朋友要些果子垫肚子好了。"; // 紫评
                evaluationLines[2] = "不过，老板娘你一定不止于此对吧。"; // 绿评
                evaluationLines[3] = "好吃耶，老板娘你的手艺真好！"; // 橙评
                evaluationLines[4] = "哇塞，下次我一定要带大家来分享！"; // 粉评
                evaluationLines[5] = "喂！这完全就是黑店吧！！"; // 大额爆预算
                evaluationLines[6] = "抱歉……我身上只有这些了……"; // 小额爆预算
                evaluationLines[7] = "诶诶，老板娘，你干什么？"; // 被赶
                evaluationLines[8] = "还是去找朋友们要点果子吃吧……"; // 看到大量其他客人被赶
                
                GameData.CoreLanguage.Collections.NightSceneLanguage.SpecialEvaluation[9000] = evaluationLines;
                Log.LogWarning($"{LOG_TAG} Updated special evaluation lines for special guest ID 9000");
            }
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ResourceExManager.TryInjectAllSpriteSetCompact();
        }
    }

    public void RunOnMainThread(Action action)
    {
        // Log.LogDebug($"{LOG_TAG} Enqueue action to run on main thread");
        _mainThreadQueue.Enqueue(action);
    }

    public void RunOnMainThread(Action action, Func<bool> condition)
    {
        if (condition == null)
        {
            RunOnMainThread(action);
            return;
        }

        lock (_conditionalActions)
        {
            _conditionalActions.Add((action, condition));
        }
    }

    private void FixedUpdate()
    {
        lock (_conditionalActions)
        {
            for (int i = _conditionalActions.Count - 1; i >= 0; i--)
            {
                var (action, condition) = _conditionalActions[i];
                bool shouldRun = false;
                try
                {
                    shouldRun = condition();
                }
                catch (Exception e)
                {
                    Log.LogError($"{LOG_TAG} Error checking condition: {e.Message}");
                    _conditionalActions.RemoveAt(i);
                    continue;
                }

                if (shouldRun)
                {
                    RunOnMainThread(action);
                    _conditionalActions.RemoveAt(i);
                }
            }
        }

        switch (CurrentGameScene)
        {
            case Scene.DayScene:
                KyoukoManager.OnFixedUpdate();
                break;
            case Scene.WorkScene:
                KyoukoManager.OnFixedUpdate();
                break;
            default:
                break;
        }
    }

    private void OnDestroy()
    {
    }
}
