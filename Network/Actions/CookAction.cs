using MemoryPack;
using GameData.Core.Collections;

namespace MetaMystia;

[MemoryPackable]
public partial class CookAction : NetAction
{
    public override ActionType Type => ActionType.COOK;
    public int GridIndex { get; set; }
    public int FoodId { get; set; }
    public int RecipeId {get; set; }
    public int[] ModifierIds { get; set; }
    public override void OnReceived()
    {
        Log.LogInfo($"Received COOK: CookerIndex={GridIndex}, FoodId={FoodId}, Modifiers=[{string.Join(",", ModifierIds)}]");
        PluginManager.Instance.RunOnMainThread(() =>
        {
            var recipe = RecipeId.RefRecipe();
            if (recipe == null)
            {
                Log.LogWarning($"Failed to create recipe");
                return;
            }

            var food = FoodId.AsNewFood();
            if (food == null)
            {
                Log.LogWarning($"Failed to create food");
                return;
            }
            food.modifier = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int>(ModifierIds);

            var cookerController = CookManager.GetCookerControllerByIndex(GridIndex);
            if (cookerController == null)
            {
                Log.LogWarning($"Failed to find CookerController with GridIndex={GridIndex}");
                return;
            }

            CookControllerPatch.SetCook_Original(cookerController, food, recipe, false);
        });
    }
}
