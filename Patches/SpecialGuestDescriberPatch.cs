using HarmonyLib;
using Common.UI;
using GameData.Core.Collections.NightSceneUtility;
using Il2CppSystem.Threading;

namespace MetaMystia;

// Hearts to ShigureYuki!
[HarmonyPatch(typeof(Common.UI.SpecialGuestDescriber))]
[AutoLog]
public partial class SpecialGuestDescriberPatch
{
    [HarmonyPatch(nameof(SpecialGuestDescriber.Describe))]
    [HarmonyPrefix]
    public static bool Describe_Prefix(SpecialGuestDescriber __instance, SpecialGuest detail, ref CancellationToken cancellationToken)
    {
        if (!ResourceExManager.TryGetSpecialGuestCustomPortrayal(detail.CharacterDefaultPortrayal, out var customPortrayal))
            return true;
        
        __instance.portrayal.sprite = customPortrayal[detail.CharacterDefaultPortrayal.faceInNoteBook];
        __instance.portrayal.enabled = true;
        return false;
    }
}