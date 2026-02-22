using DEYU.AdpUISystem.Utils;
using DEYU.Utils;
using GameData.Core.Collections.CharacterUtility;
using GameData.Profile;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SgrMystia;


[HarmonyPatch(typeof(GameData.Core.Collections.CharacterUtility.DataBaseCharacter))]
[AutoLog]
public partial class DataBaseCharacterPatch
{
    [HarmonyPatch(nameof(DataBaseCharacter.RefNormalGuestVisual))]
    [HarmonyPrefix]
    public static bool RefNormalGuestVisual_Prefix(ref GuestProfilePair __result, ref int id)
    {
        if (MpManager.IsConnectedClient && WorkSceneManager.normalGuestProfilePairIndexQueue.TryDequeue(out int index))
        {
            var compacts = DataBaseCharacter.NormalGuestVisual.Get(id, new[] { DataBaseCharacter.FallbackCompactPixel });
            var compact = compacts[index];
            Log.LogMessage($"RefNormalGuestVisual_Prefix called, get index {index} => {compact.ToString()}");
            var guestProfilePair = new GuestProfilePair(id, DataBaseCharacter.UnifiedNormalGuestBGColor, DataBaseCharacter.UnifiedNormalGuestTextColor, null, ScriptableObject.CreateInstance<CharacterSkinSets>());
            guestProfilePair.CharacterPixel.Initialize(compact, null, null);
            __result = guestProfilePair;
            return false;
        }
        return true;
    }
}
