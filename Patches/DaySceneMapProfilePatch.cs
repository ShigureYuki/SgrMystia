using HarmonyLib;

namespace MetaMystia;


[HarmonyPatch(typeof(GameData.Profile.DaySceneMapProfile))]
public class DaySceneMapProfilePatch : PatchBase<DaySceneMapProfilePatch>
{
    public static GameData.Profile.DaySceneMapProfile instanceRef = null;

    [HarmonyPatch(MethodType.Constructor)]
    public static void DaySceneMapProfile_Constructor_Postfix(GameData.Profile.DaySceneMapProfile __instance)
    {
        Log.LogWarning($"DaySceneMapProfile created: {__instance.name}");
        instanceRef = __instance;
    }
}