using HarmonyLib;
using GameData.Core.Collections.CharacterUtility;
using GameData.Profile;
using DEYU.Utils;
using System;
using UnityEngine;

namespace MetaMystia;


[HarmonyPatch(typeof(GameData.Core.Collections.CharacterUtility.DataBaseCharacter))]
public class DataBaseCharacterPatch : PatchBase<DataBaseCharacterPatch>
{
    [HarmonyPatch(nameof(DataBaseCharacter.RefNormalGuestVisual))]
    [HarmonyPrefix]
    public static bool RefNormalGuestVisual_Prefix(ref int id, ref GuestProfilePair __result)
    {
        if (!MpManager.IsConnected)
        {
            return true;
        }

        // var selectedPixel = DataBaseCharacter.NormalGuestVisual.Get(id, new[] {DataBaseCharacter.FallbackCompactPixel}).RandomSelectOne();

        var CharacterSpriteSetCompacts = DataBaseCharacter.NormalGuestVisual.Get(id, new[] {DataBaseCharacter.FallbackCompactPixel});

        var random = new System.Random();
        var k = random.Next(0, CharacterSpriteSetCompacts.Length);
        var selectedPixel = CharacterSpriteSetCompacts[k];
        // MpManager.SendXXX(k);

        var guestProfilePair = new GuestProfilePair(id, DataBaseCharacter.UnifiedNormalGuestBGColor, DataBaseCharacter.UnifiedNormalGuestTextColor, null, ScriptableObject.CreateInstance<CharacterSkinSets>());
        guestProfilePair.characterPixel.Initialize(selectedPixel, null, null);
        __result = guestProfilePair;

        return false;
    }


    [HarmonyPatch(nameof(DataBaseCharacter.GetNPCLabel))]
    [HarmonyPrefix]
    public static bool GetNPCLabel_Prefix(ref string __result, SchedulerNode.Character identity)
    {
        // Log.LogWarning($"{LOG_TAG} GetNPCLabel_Prefix called for identity: {identity} result: {__result}");
        
        var config = ResourceExManager.GetCharacterConfig(identity.characterId, identity.characterIdentity.ToString());
        if (config != null)
        {
            __result = config.label;
            return false;
        }
        
        return true;
    }
}