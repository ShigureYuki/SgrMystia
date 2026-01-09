using GameData.CoreLanguage;
using GameData.CoreLanguage.Collections;
using UnityEngine;
using GameData.Core.Collections;

using MetaMystia.ResourceEx.Models;
using System.Linq;

namespace MetaMystia.ResourceEx;

[AutoLog]
public static partial class IngredientEx
{
    public static void RegisterAllLanguages()
    {
        ResourceExManager.IngredientConfigs.Values.ToList().ForEach(RegisterLanguage);
    }

    private static void RegisterLanguage(IngredientConfig config)
    {
        var lang = new ObjectLanguageBase(
            name: config.name,
            Description: config.description,
            ResourceExManager.GetSprite(config.spritePath, config.ModRoot)
        );
        // var success = DataBaseLanguage.Ingredients.TryAdd(config.id, lang);
        DataBaseLanguage.Ingredients[config.id] = lang;
        Log.Warning($"Registered language for ingredient {config.id}: {config.name}");
    }

    public static void RegisterAllObjects()
    {
        ResourceExManager.IngredientConfigs.Values.ToList().ForEach(RegisterObject);
    }

    private static void RegisterObject(IngredientConfig config)
    {
        var ingredient = new Ingredient(
            id: config.id,
            baseValue: config.baseValue,
            level: config.level,
            prefix: config.prefix,
            tags: config.tags.ToArray()
        );
        var success = DataBaseCore.Ingredients.TryAdd(ingredient.Id, ingredient);
        Log.Warning($"Registered ingredient object {config.id}: {config.name}, success={success}");
    }


}