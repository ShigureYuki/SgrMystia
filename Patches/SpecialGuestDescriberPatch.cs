using HarmonyLib;
using Common.UI;
using GameData.Core.Collections.NightSceneUtility;

namespace MetaMystia;


[HarmonyPatch(typeof(Common.UI.SpecialGuestDescriber))]
[HarmonyPatch]
public class SpecialGuestDescriberPatch : PatchBase<SpecialGuestDescriberPatch>
{
    [HarmonyPatch(nameof(SpecialGuestDescriber.Describe))]
    [HarmonyPostfix]
    public static void Describe_Postfix(SpecialGuestDescriber __instance, SpecialGuest detail)
    {
        Log.LogWarning($"{LOG_TAG} Describe method called. with special guest ID {detail.Id}");
        if (ResourceExManager.ExistsCharacterConfig(detail.Id))
        {
            __instance.portrayal.sprite = Utils.GetArtWork(@"E:\Desktop\Touhou Mystia Izakaya\ResourceEx\MetaMystia\assets\Daiyousei_0.png", new UnityEngine.Vector2(0.5f, 0.5f));
            __instance.portrayal.sprite = ResourceExManager.GetPortraitSprite(detail.Id, 0);
            Log.LogWarning($"{LOG_TAG} Updated portrayal sprite for special guest ID {detail.Id}");
        }
    }


}