using HarmonyLib;
using GameData.RunTime.Common;
using GameData.Core.Collections.CharacterUtility;
using System;
using DEYU.Utils;
using DayScene.TimelineExtensions;

namespace MetaMystia;

[HarmonyPatch(typeof(GameData.RunTime.Common.RunTimeAlbum))]
public class RunTimeAlbumPatch : PatchBase<RunTimeAlbumPatch>
{
    [HarmonyPatch(nameof(RunTimeAlbum.RefSpecialPixel))]
    [HarmonyPrefix]
    public static bool RefSpecialPixel_Prefix(ref CharacterSpriteSetCompact __result, int id, bool useDefaultSkin = false)
    {

        if (!ResourceExManager.ExistsCharacterConfig(id))
        {
            Log.LogWarning($"{LOG_TAG} RefSpecialPixel_Prefix: No custom CharacterConfig for id {id}, falling back to original.");
            return true;
        }

        __result = ResourceExManager.TryGetSpriteSetCompact(id);
        if (__result != null)
        {
            Log.LogInfo($"{LOG_TAG} RefSpecialPixel_Prefix: Found custom CharacterSpriteSetCompact for id {id}.");
            return false;
        }

        ResourceExManager.TryInjectAllSpriteSetCompact();

        __result = ResourceExManager.TryGetSpriteSetCompact(id);
        if (__result != null)
        {
            Log.LogInfo($"{LOG_TAG} RefSpecialPixel_Prefix: Found custom CharacterSpriteSetCompact for id {id}.");
            return false;
        }
        
        Log.LogWarning($"{LOG_TAG} RefSpecialPixel_Prefix: Custom CharacterSpriteSetCompact for id {id} not found, falling back to original.");
        return true;
    }
}