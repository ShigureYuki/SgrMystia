using HarmonyLib;
using UnityEngine;
using Common.CharacterUtility;
using BepInEx.Logging;

namespace MetaMystia;

[HarmonyPatch(typeof(CharacterControllerInputGeneratorComponent))]
public class CharacterInputPatch
{
    private static ManualLogSource Log => Plugin.Instance.Log;

    [HarmonyPatch(nameof(CharacterControllerInputGeneratorComponent.UpdateInputDirection))]
    [HarmonyPrefix]
    public static void UpdateInputDirection_Prefix(CharacterControllerInputGeneratorComponent __instance, Vector2 inputDirection)
    {
        try
        {
            // 检查这是否是玩家角色的输入（Mystia）
            var playerInputGenerator = MystiaManager.Instance.GetInputGenerator();
            if (playerInputGenerator != null && __instance == playerInputGenerator)
            {
                // 这是玩家的输入，发送给对等端
                MultiplayerManager.Instance.SendMoveData(inputDirection);
            }
        }
        catch (System.Exception e)
        {
            Log.LogError($"Error in UpdateInputDirection_Prefix: {e.Message}");
        }
    }
}
