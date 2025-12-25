using HarmonyLib;
using GameData.RunTime.Common;
using GameData.Core.Collections.CharacterUtility;
using System;

namespace MetaMystia;

[HarmonyPatch(typeof(GameData.RunTime.Common.RunTimeAlbum))]
public class RunTimeAlbumPatch : PatchBase<RunTimeAlbumPatch>
{
    [HarmonyPatch(nameof(RunTimeAlbum.RefSpecialPixel))]
    [HarmonyPrefix]
    public static bool RefSpecialPixel_Prefix(ref CharacterSpriteSetCompact __result, int id, bool useDefaultSkin = false)
    {

        // if (id == 9001)
        // {
        //     var key = "Koakuma";
        //     CharacterSpriteSetCompact KoakumaPixel = null;
        //     Action<CharacterSpriteSetCompact> process_ = (spriteSet) =>
        //     {
        //         if (spriteSet != null && spriteSet.name == "Koakuma")
        //         {
        //             KoakumaPixel = spriteSet;
        //         }
        //     };
        //     Utils.FindAndProcessResources<CharacterSpriteSetCompact>(process_);
        //     __result = KoakumaPixel;
        //     return false;
        // }

        if (id != 9000)
        {
            return true;
        } 
        CharacterSpriteSetCompact DaiyouseiPixel = null;
        Action<CharacterSpriteSetCompact> process = (spriteSet) =>
        {
            if (spriteSet != null && spriteSet.name == "Daiyousei")
            {
                DaiyouseiPixel = spriteSet;
            }
        }; 

        Utils.FindAndProcessResources<CharacterSpriteSetCompact>(process);

        __result = DaiyouseiPixel;
        return false;
    }

    // [HarmonyPatch(nameof(RunTimeAlbum.RefSpecialPortrayal))]
    // [HarmonyPrefix]
    // public static bool RefSpecialPortrayal_Prefix(this int id, bool useDefaultSkin = false, ref GameData.Profile.CharacterPortrayal __result)
    // {
    //     if (ResourceExManager.ExistsCharacterConfig(id))
    //     {
    //         __result = ResourceExManager.GetPortraitSprite(id);
    //         return false;
    //     }
    // }
}