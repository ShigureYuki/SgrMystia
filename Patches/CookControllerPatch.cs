using HarmonyLib;
using UnityEngine;
using NightScene.CookingUtility;
using Il2CppSystem.Runtime.InteropServices;
using GameData.Core.Collections;
using System;
using Il2CppMono;

namespace MetaMystia;

[HarmonyPatch(typeof(NightScene.CookingUtility.CookController))]
public class CookControllerPatch : PatchBase<CookControllerPatch>
{


    private static bool _skipPatchSetCook = false;
    public static void SetCook_DirectInvoke(CookController __instance, Sellable thisResult, Recipe recipe, bool thisCouldReturnIngredients)
    {
        _skipPatchSetCook = true;
        __instance.SetCook(thisResult, recipe, thisCouldReturnIngredients);
        _skipPatchSetCook = false;
    }

    // TODO: QTE
    [HarmonyPatch(nameof(CookController.SetCook))]
    [HarmonyPostfix]
    public static void SetCook_Postfix(CookController __instance, Sellable thisResult, Recipe recipe, bool thisCouldReturnIngredients)
    {
        Log.LogDebug($"{LOG_TAG} SetCook_Postfix called for food:\n {thisResult.ToString()}\n\nrecipe:\n {recipe.ToString()}\n\nthisCouldReturnIngredients: {thisCouldReturnIngredients}");
        if (_skipPatchSetCook)
        {
            return;
        }
        
        var gridIndex = __instance.GridIndex;
        var recipeId = thisResult.id;
        var modifier = thisResult.modifier;
        MpManager.Instance.SendCook(gridIndex, recipeId, modifier);
    }

    private static bool _skipPatchExtract = false;
    public static void Extract_DirectInvoke(CookController __instance, Il2CppSystem.Action<Sellable> targetAssignmentCallBack = null)
    {
        _skipPatchExtract = true;
        __instance.Extract(null);
        _skipPatchExtract = false;
    }

    [HarmonyPatch(nameof(CookController.Extract))]
    [HarmonyPrefix]
    public static void Extract_Prefix(CookController __instance)
    {
        if (_skipPatchExtract)
        {
            return;
        }

        var gridIndex = __instance.GridIndex;
        MpManager.Instance.SendExtract(gridIndex);
    }

}
