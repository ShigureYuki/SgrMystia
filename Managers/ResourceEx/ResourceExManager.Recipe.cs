using System.Collections.Generic;
using GameData.Core.Collections;
using GameData.CoreLanguage;
using GameData.CoreLanguage.Collections;
using UnityEngine;

using GameData.RunTime.Common;

namespace MetaMystia;

public static partial class ResourceExManager
{
    public static void BuildTestRecipe()
    {
        // var r = new Recipe(
        //     config.id,
        //     config.foodId,
        //     Cooker.CookerType.CuttingBoard, // todo
        //     config.cookTime,
        //     config.ingredients.ToArray()
        // );
        // DataBaseCore.Recipes.TryAdd(r.Id, r);
        // Log.Warning($"Built and Registered recipe: {r.Id}");

        Sprite sprite = Utils.GetArtWork("E:/Desktop/Test.jpg", new Vector2(0.5f, 0.5f)); // todo
        List<int> tags = new List<int> { 1, 2 };
        List<int> banTags = new List<int> { 3 };
        var additiveTags = new Il2CppSystem.Collections.Generic.List<int>(1);
        additiveTags.Add(4);
        var hasfood = DataBaseLanguage.Foods.TryAdd(9000,
            new ObjectLanguageBase("Test Food", "Test Food description", sprite));
        var hassell = DataBaseCore.Foods.TryAdd(9000,
            new Sellable(9000, 114514, 4, tags.ToArray(),
                banTags.ToArray(), Sellable.SellableType.Food,
                additiveTags, false));
        var hasrec = DataBaseCore.Recipes.TryAdd(9000,
            new Recipe(9000, 9000, Cooker.CookerType.CuttingBoard, 1,
                new int[] { 0, 1, 2 }));
        var hasdlcmap = DataBaseCore.RecipesMapping.TryAdd(9000, "Test") &&
                        DataBaseCore.FoodsMapping.TryAdd(9000, "Test");
        Log.Warning($"Built and Registered test recipe: hasfood: {hasfood}, hassell: {hassell}, hasrec: {hasrec}, hasdlcmap: {hasdlcmap}");
    }

}
