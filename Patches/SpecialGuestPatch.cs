using HarmonyLib;
using Common.UI;
using GameData.Core.Collections.NightSceneUtility;

namespace MetaMystia;


[HarmonyPatch(typeof(GameData.Core.Collections.NightSceneUtility.SpecialGuest))]
[HarmonyPatch]
public class SpecialGuestPatch : PatchBase<SpecialGuestPatch>
{
    [HarmonyPatch(nameof(SpecialGuest.CharacterPortrayal), MethodType.Getter)]
    [HarmonyPrefix]
    public static void CharacterPortrayal_Getter_Prefix()
    {
        Log.LogWarning("[SpecialGuestPatch] CharacterPortrayal getter called.");
    }

    // [HarmonyPatch(nameof(SpecialGuest.CharacterDefaultPortrayal), MethodType.Getter)]
    // [HarmonyPrefix]
    // public static bool CharacterDefaultPortrayal_Getter_Prefix(SpecialGuest __instance, ref GameData.Profile.CharacterPortrayal __result)
    // {
    //     UnityEngine.AddressableAssets.AssetReferenceAtlasedSprite assetRef = new UnityEngine.AddressableAssets.AssetReferenceAtlasedSprite("guid??????????");
    //     var portrayal = UnityEngine.ScriptableObject.CreateInstance<GameData.Profile.CharacterPortrayal>();
    //     var portrayals = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.AddressableAssets.AssetReferenceAtlasedSprite>(1);
    //     portrayals[0] = assetRef;
    //     portrayal.m_VisualAssetAtlasReference = portrayals;
    //     __result = portrayal;
    //     return false;
    // }


}