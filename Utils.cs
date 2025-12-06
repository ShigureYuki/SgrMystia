using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Common.DialogUtility;
using DEYU.AdpUISystem.Utils;
using GameData.Core.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace MetaMystia;

public static class Utils
{
    private static ManualLogSource Log => Plugin.Instance.Log;
    private static readonly string LOG_TAG = "[Utils.cs]";
    // public static Sellable[] sellables = Array.Empty<Sellable>();
    public static List<Sellable> beverages = new List<Sellable>();
    public static List<Sellable> sellables = new List<Sellable>();
    public static List<Recipe> recipes = new List<Recipe>();

    public static string GetMapNameCN(string mapLabel)
    {
        return mapLabel switch
        {
            "Home" => "夜雀小屋",
            "Basement" => "地下室",
            "BeastForest" => "妖怪兽道",
            "HumanVillage" => "人间之里",
            "HakureiShrine" => "博丽神社",
            "ScarletMansion" => "红魔馆",
            "BambooForest" => "迷途竹林",
            "PartyStage" => "舞台",
            "Hakugyokurou" => "白玉楼",
            "DLC1_MagicForest" => "魔法森林",
            "DLC1_YoukaiMountain" => "妖怪之山",
            "DLC2_FormerHell" => "旧地狱",
            "DLC2_EarthSpiritsPalace" => "地灵殿",
            "DLC3_MyourenTemple" => "命莲寺",
            "DLC3_DivineSpiritMausoleum" => "神灵庙",
            "DLC3_HakureiFestival" => "博丽大祭",
            "DLC4_GardenOfTheSun" => "太阳花田",
            "DLC4_ShiningNeedleCastle" => "辉针城",
            "DLC4_ScarletMansionBasement" => "红魔馆地下室",
            "DLC5_Makai" => "魔界",
            "DLC5_LunarCapital" => "月之都",
            _ => mapLabel
        };
    }

    public static void AnimationDialog()
    {
        var dialogList = new CustomDialogList();
        dialogList.AddDialog(14, SpeakerIdentity.Identity.Special, 0, Position.Right, "夜雀食堂的七大不可思议……");
        dialogList.AddDialog(14, SpeakerIdentity.Identity.Special, 0, Position.Right, "<i>（啊…）</i>");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "大家好！这里是神秘的探访频道！");
        dialogList.AddDialog(14, SpeakerIdentity.Identity.Special, 0, Position.Right, "<i>（呜…）</i>");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "这是我们的特别节目！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "我们将探访夜雀食堂不为人知的七大不可思议！");
        dialogList.AddDialog(14, SpeakerIdentity.Identity.Special, 0, Position.Right, "是……是哦！");
        dialogList.AddDialog(14, SpeakerIdentity.Identity.Special, 0, Position.Right, "这是满足游戏玩家的超级福利单元哦！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "今天我们要为玩家们解开的话题是——");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "夜雀食堂的食材并没有米！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "那么饭团到底是怎么做出来的！？");
        dialogList.AddDialog(-1, SpeakerIdentity.Identity.Self, 0, Position.Right, "<i>（嗯？）</i>");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "<i>（盯—）</i>");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "今天，在这里！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "请务必！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "务必向大家展示一下好吗？！");
        dialogList.AddDialog(-1, SpeakerIdentity.Identity.Self, 0, Position.Right, "诶~");
        dialogList.AddDialog(-1, SpeakerIdentity.Identity.Self, 0, Position.Right, "这，原来是这个意思啊—");
        dialogList.AddDialog(-1, SpeakerIdentity.Identity.Self, 0, Position.Right, "我，我知道啦");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "是吗？竟然真的打算解释吗！？");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "<i>不不不，我原本只是故意来凑个热闹</i>");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "<i>没想到真的打算说明吗？</i>");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "那可就太好了！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "帮了大忙啦！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "（嗯？）");
        dialogList.AddDialog(-1, SpeakerIdentity.Identity.Self, 0, Position.Right, "<i>（施法中…）</i>");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "（啊？）");
        dialogList.AddDialog(-1, SpeakerIdentity.Identity.Self, 0, Position.Right, "好啦~就是这样~！");
        dialogList.AddDialog(18, SpeakerIdentity.Identity.Special, 0, Position.Left, "你就扯吧！");
        DialogManager.BuildAndShow(dialogList);
    }

    
    public static void DumpSellableProfile()
    {
        try
        {
            var sellableProfileType = Il2CppType.Of<GameData.Profile.SellableProfile>();
            var foundAssets = Resources.FindObjectsOfTypeAll(sellableProfileType);

            if (foundAssets == null || foundAssets.Length == 0)
            {
                Log.LogWarning($"{LOG_TAG} No SellableProfile assets found in memory.");
                return;
            }

            Log.LogInfo($"{LOG_TAG} Found {foundAssets.Length} SellableProfile asset(s).");

            for (var assetIndex = 0; assetIndex < foundAssets.Length; assetIndex++)
            {
                var sellableProfile = foundAssets[assetIndex].TryCast<GameData.Profile.SellableProfile>();
                if (sellableProfile == null)
                {
                    Log.LogWarning($"{LOG_TAG} Object at index {assetIndex} could not be cast to SellableProfile.");
                    continue;
                }
                Log.LogInfo($"{LOG_TAG} Dumping contents of SellableProfile asset at index {assetIndex}:");
                foreach (var sellable in sellableProfile.sellables)
                {
                    sellables.Add(sellable);
                }
            }

        }
        catch (Exception e)
        {
            Log.LogError($"{LOG_TAG} Failed to dump SellableProfile contents: {e.Message}\n{e.StackTrace}");
        }
    }
    public static void DumpRecipeProfile()
    {
        try
        {
            var recipeProfileType = Il2CppType.Of<GameData.Profile.RecipeProfile>();
            var foundAssets = Resources.FindObjectsOfTypeAll(recipeProfileType);

            if (foundAssets == null || foundAssets.Length == 0)
            {
                Log.LogWarning($"{LOG_TAG} No RecipeProfile assets found in memory.");
                return;
            }

            Log.LogInfo($"{LOG_TAG} Found {foundAssets.Length} RecipeProfile asset(s).");

            for (var assetIndex = 0; assetIndex < foundAssets.Length; assetIndex++)
            {
                var recipeProfile = foundAssets[assetIndex].TryCast<GameData.Profile.RecipeProfile>();
                if (recipeProfile == null)
                {
                    Log.LogWarning($"{LOG_TAG} Object at index {assetIndex} could not be cast to RecipeProfile.");
                    continue;
                }
                Log.LogInfo($"{LOG_TAG} Dumping contents of RecipeProfile asset at index {assetIndex}:");
                foreach (var recipe in recipeProfile.recipes)
                {
                    recipes.Add(recipe);
                    Log.LogInfo($"{LOG_TAG} {recipe.id}");
                }
            }

        }
        catch (Exception e)
        {
            Log.LogError($"{LOG_TAG} Failed to dump SellableProfile contents: {e.Message}\n{e.StackTrace}");
        }
    }
};

