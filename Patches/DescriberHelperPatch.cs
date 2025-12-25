// using HarmonyLib;
// using Common.DialogUtility;
// using Il2CppSystem.Collections.Generic;
// using DEYU.AssetHandleUtility;
// using Common.UI;
// using UnityEngine.UI;
// using UnityEngine;
// using Cysharp.Threading.Tasks;
// using System.Threading;


// namespace MetaMystia;


// [HarmonyPatch(typeof(Common.UI.DescriberHelper))]
// public class DescriberHelperPatch : PatchBase<DescriberHelperPatch>
// {
    
//     // [HarmonyPatch(nameof(DialogPannel.GetSpeakerName))]
//     // [HarmonyPostfix]
//     // public static void GetSpeakerName_Postfix(ref string line, DialogMeta meta, string speakerName)
//     // {
//     //     Log.LogWarning($"{LOG_TAG}  with meta.SpeakerID: {meta.ToString()}, line: {line}, speakerName: {speakerName}");
//     // }

//     // prefix
//     [HarmonyPatch(nameof(DescriberHelper.AssignImageSpriteImpl))]
//     [HarmonyPrefix]
//     public static bool AssignImageSpriteImpl_Prefix(Image imageComponent, UniTask<IAssetHandle<Sprite>> spriteHandle, CancellationToken cancellationToken, Il2CppSystem.Action<Image> postProcessCallback)
//     {

        
//     }

// }