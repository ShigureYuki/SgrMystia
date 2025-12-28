using System.Linq;
using BepInEx.Logging;
using System.IO;
using UnityEngine;
using System;
using Il2CppInterop.Runtime;
using System.Numerics;

namespace MetaMystia;

public static class Utils
{
    private static ManualLogSource Log => Plugin.Instance.Log;
    private static readonly string LOG_TAG = "[Utils.cs]";

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

    public static void test()
    {
        var Ingredients = GameData.CoreLanguage.Collections.DataBaseLanguage.Ingredients;
        foreach (var kvp in Ingredients)
        {
            Log.LogInfo($"{LOG_TAG} Ingredient ID: {kvp.Key}, Name: {kvp.Value.ToString()}");
        }
    }

    // Note: only available for those functions are patched. Not patched functions will not appear in the stacktrace
    public static bool CheckStacktraceContains(string funcName)
    {
        var stack = new System.Diagnostics.StackTrace();
        // foreach (var frame in stack.GetFrames())
        // {
        //     var method = frame.GetMethod();
        //     Log.LogInfo($"{LOG_TAG} \t at {method.Name}");
        // }
        return stack.GetFrames().Any(frame => frame.GetMethod().Name.Contains(funcName));
    }
    
    public static Sprite GetArtWork(string filePath, UnityEngine.Vector2 pivot, int width = 0, int height = 0, int pixelOffsetX = 0, int pixelOffsetY = 0)
    {
        if (!File.Exists(filePath)) return null;

        var texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        
        byte[] fileData = File.ReadAllBytes(filePath);
        ImageConversion.LoadImage(texture2D, fileData);

        if (width > 0 && height > 0 && (texture2D.width != width || texture2D.height != height))
        {
            var newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;
            newTexture.SetPixels(colors);

            int x = (width - texture2D.width) / 2 + pixelOffsetX;
            int y = (height - texture2D.height) / 2 + pixelOffsetY;
            
            x = Mathf.Clamp(x, 0, width - texture2D.width);
            y = Mathf.Clamp(y, 0, height - texture2D.height);

            newTexture.SetPixels(x, y, texture2D.width, texture2D.height, texture2D.GetPixels());
            newTexture.Apply();
            texture2D = newTexture;
        }

        texture2D.filterMode = FilterMode.Point; 
        texture2D.wrapMode = TextureWrapMode.Clamp;
        
        var sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), pivot, 48f);
            
        sprite.name = Path.GetFileNameWithoutExtension(filePath);
        return sprite;
    }
    public static void FindAndProcessResources<T>(Action<T> action) where T : UnityEngine.Object
    {
        try
        {
            var type = Il2CppType.Of<T>();
            var foundAssets = Resources.FindObjectsOfTypeAll(type);

            if (foundAssets == null || foundAssets.Length == 0)
            {
                Log.LogWarning($"{LOG_TAG} No {typeof(T).Name} assets found in memory.");
                return;
            }

            Log.LogDebug($"{LOG_TAG} Found {foundAssets.Length} {typeof(T).Name} asset(s).");

            for (var i = 0; i < foundAssets.Length; i++)
            {
                var asset = foundAssets[i].TryCast<T>();
                if (asset == null) continue;
                action(asset);
            }
        }
        catch (Exception e)
        {
            Log.LogError($"{LOG_TAG} Failed to process {typeof(T).Name} contents: {e.Message}\n{e.StackTrace}");
        }
    }
};

