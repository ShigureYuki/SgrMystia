using System.Collections.Generic;
using HarmonyLib;
using SgrMystia.Network;

namespace SgrMystia;


[HarmonyPatch(typeof(Common.UI.IzakayaSelectorPanel_New))]
[AutoLog]
public partial class IzakayaSelectorPanelPatch
{
    public static Common.UI.IzakayaSelectorPanel_New instanceRef = null;
    public static Dictionary<string, Common.UI.GlobalMap.IGuideMapSpot> cachedSpots = new Dictionary<string, Common.UI.GlobalMap.IGuideMapSpot>();

    [HarmonyPatch(nameof(Common.UI.IzakayaSelectorPanel_New.OnGuideMapInitialize))]
    [HarmonyPrefix]
    public static void OnGuideMapInitialize_Prefix(Common.UI.IzakayaSelectorPanel_New __instance)
    {
        instanceRef = __instance;
        Log.LogInfo($"OnGuideMapInitialize called");
    }

    [HarmonyPatch(nameof(Common.UI.IzakayaSelectorPanel_New._OnGuideMapInitialize_b__21_0))]
    [HarmonyPrefix]
    public static bool _OnGuideMapInitialize_b__21_0_Prefix(ref Common.UI.IzakayaSelectorPanel_New __instance)
    {
        if (!MpManager.IsConnected)
        {
            Log.LogWarning($"Not in multiplayer session, skipping patch");
            return true;
        }

        if (MpManager.PeerScene == Common.UI.Scene.IzakayaPrepScene || MpManager.PeerScene == Common.UI.Scene.WorkScene)
        {
            Log.Error($"peer already in prep scene, will disconnect");
            Notify.ShowOnMainThread(TextId.PeerAlreadyInScene.Get());
            MpManager.DisconnectPeer();
            return true;
        }

        // 参考 Common.UI.IzakayaLevel
        // public enum IzakayaLevel
        // {
        //     Level1 = 1,
        //     Level2 = 2,
        //     Level3 = 3,
        //     Null = 0
        // }

        var izakayaMapLabel = __instance.m_CurrentSelectedSpot.PrimaryName;
        var izakayaLevel = (int)__instance.m_CurrentSelectedIzakayaLevel;
        Log.LogWarning($"Selected Spot: {izakayaMapLabel}, Level: {izakayaLevel}");

        var mySelect = $"{Utils.GetMapLabelNameCN(izakayaMapLabel)} {Utils.GetMapLevelNameCN(izakayaLevel)}";
        if (PeerManager.IzakayaMapLabel == "" || PeerManager.IzakayaLevel == 0)
        {
            Log.LogWarning($"Kyouko has not selected an Izakaya yet -> send SELECT and skip");
            SelectAction.Send(izakayaMapLabel, izakayaLevel);
            Notify.ShowOnMainThread(TextId.SelectedIzakaya.Get(mySelect));
            return false;
        }

        if (izakayaMapLabel != PeerManager.IzakayaMapLabel || izakayaLevel != PeerManager.IzakayaLevel)
        {
            var peerSelect = $"{Utils.GetMapLabelNameCN(PeerManager.IzakayaMapLabel)} {Utils.GetMapLevelNameCN(PeerManager.IzakayaLevel)}";
            Log.LogWarning($"Selected Izakaya does not match Kyouko's selection -> show rejection dialog");
            Notify.ShowOnMainThread(TextId.SelectedIzakayaMismatch.Get(mySelect, peerSelect));
            return false;
        }

        Log.LogWarning($"Selected Izakaya matches Kyouko's selection -> send CONFIRM and show confirmation dialog");
        MapDecidedAction.Send(izakayaMapLabel, izakayaLevel);

        System.Action closePanelCallback = () =>
        {
            cachedSpots.TryGetValue(izakayaMapLabel, out var spot);
            instanceRef.m_CurrentSelectedSpot = spot;
            instanceRef.m_CurrentSelectedIzakayaLevel = (Common.UI.IzakayaLevel)izakayaLevel;
            SgrYuki.Utils.Panel.CloseActivePanelsBeforeSceneTransit();
            _OnGuideMapInitialize_b__21_0_Original(instanceRef);
        };
        Dialog.ShowConfirmDialog(izakayaMapLabel, closePanelCallback);
        return false;
    }

    [HarmonyPatch(nameof(Common.UI.IzakayaSelectorPanel_New._OnGuideMapInitialize_b__21_0))]
    [HarmonyReversePatch]
    public static void _OnGuideMapInitialize_b__21_0_Original(Common.UI.IzakayaSelectorPanel_New __instance)
    {
        throw new System.NotImplementedException();
    }

    [HarmonyPatch(nameof(Common.UI.IzakayaSelectorPanel_New.OnGuideMapSpotSelected))]
    [HarmonyPrefix]
    public static void OnGuideMapSpotSelected_Prefix(ref Common.UI.GlobalMap.IGuideMapSpot guideMapSpot)
    {
        if (guideMapSpot != null && !string.IsNullOrEmpty(guideMapSpot.PrimaryName))
        {
            cachedSpots[guideMapSpot.PrimaryName] = guideMapSpot;
        }

        Log.Info($"OnGuideMapSpotSelected called, guideMapSpot.PrimaryName: {guideMapSpot.PrimaryName}");
    }
}
