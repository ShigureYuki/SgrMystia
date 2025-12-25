using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using Common.DialogUtility;
using GameData.CoreLanguage.Collections;
using GameData.Core.Collections.NightSceneUtility;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using MetaMiku;
using DEYU.Utils;

namespace MetaMystia;
public class CharacterConfig
{
    public int id { get; set; }
    public string name { get; set; }
    public string label { get; set; }
    public List<string> descriptions { get; set; }
    public string type { get; set; }
    public List<PortraitConfig> portraits { get; set; }
    public GuestConfig guest { get; set; }
    public string ModRoot { get; set; }
}

public class GuestConfig
{
    public int fundRangeLower { get; set; }
    public int fundRangeUpper { get; set; }
    public List<string> evaluation { get; set; }
    public List<FoodRequestConfig> foodRequests { get; set; }
    public List<int> hateFoodTag { get; set; }
    public List<WeightedTagConfig> likeFoodTag { get; set; }
    public List<WeightedTagConfig> likeBevTag { get; set; }
}

public class FoodRequestConfig
{
    public int tagId { get; set; }
    public string request { get; set; }
}

public class WeightedTagConfig
{
    public int tagId { get; set; }
    public int weight { get; set; }
}

public class PortraitConfig
{
    public int pid { get; set; }
    public string path { get; set; }
}

public class ResourceConfig
{
    public List<CharacterConfig> characters { get; set; }
    public List<DialogPackageConfig> dialogPackages { get; set; }
}

public class DialogConfig
{
    public int characterId { get; set; }
    public string characterType { get; set; }
    public int pid { get; set; }
    public string position { get; set; }
    public string text { get; set; }
}

public class DialogPackageConfig
{
    public string name { get; set; }
    public List<DialogConfig> dialogList { get; set; }
}

public static class ResourceExManager
{
    private static ManualLogSource Log => Plugin.Instance.Log;
    private static readonly string LOG_TAG = "[ResourceExManager]";
    
    // Abstracted resource root path
    public static string ResourceRoot { get; set; } = Path.Combine(Paths.GameRootPath, "ResourceEx");
    
    private static Dictionary<(int id, string type), CharacterConfig> _characterConfigs = new Dictionary<(int id, string type), CharacterConfig>();
    private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
    private static Dictionary<string, CustomDialogList> _dialogPackages = new Dictionary<string, CustomDialogList>();
    public static readonly string DialogPackageNamePrefix = "_";

    public static void Initialize()
    {
        LoadConfigs();
    }

    private static void LoadConfigs()
    {
        if (!Directory.Exists(ResourceRoot))
        {
            Directory.CreateDirectory(ResourceRoot);
            Log.LogInfo($"{LOG_TAG} Created ResourceEx directory at {ResourceRoot}");
            return;
        }

        var modDirs = Directory.GetDirectories(ResourceRoot);
        foreach (var modDir in modDirs)
        {
            string modName = Path.GetFileName(modDir);
            string jsonPath = Path.Combine(modDir, "ResourceEx.json");

            if (!File.Exists(jsonPath))
            {
                continue;
            }

            Log.LogInfo($"{LOG_TAG} Loading mod: {modName} from {jsonPath}");
            try
            {
                string jsonString = File.ReadAllText(jsonPath);
                var config = JsonSerializer.Deserialize<ResourceConfig>(jsonString);
                
                if (config?.characters != null)
                {
                    foreach (var charConfig in config.characters)
                    {
                        charConfig.ModRoot = modDir;
                        _characterConfigs[(charConfig.id, charConfig.type)] = charConfig;
                        Log.LogInfo($"{LOG_TAG} [{modName}] Loaded config for character {charConfig.name} ({charConfig.id}, {charConfig.type})");
                    }
                }

                if (config?.dialogPackages != null)
                {
                    foreach (var pkgConfig in config.dialogPackages)
                    {
                        var dialogList = new CustomDialogList();
                        dialogList.packageName = DialogPackageNamePrefix + pkgConfig.name;
                        foreach (var d in pkgConfig.dialogList)
                        {
                            var speakerType = SpeakerIdentity.Identity.Unknown;
                            if (!string.IsNullOrEmpty(d.characterType) && System.Enum.TryParse<SpeakerIdentity.Identity>(d.characterType, true, out var st))
                            {
                                speakerType = st;
                            }

                            var position = Position.Left;
                            if (System.Enum.TryParse<Position>(d.position, out var pos))
                            {
                                position = pos;
                            }

                            dialogList.AddDialog(d.characterId, speakerType, d.pid, position, d.text);
                        }
                        _dialogPackages[pkgConfig.name] = dialogList;
                        Log.LogInfo($"{LOG_TAG} [{modName}] Loaded dialog package: {pkgConfig.name}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.LogError($"{LOG_TAG} Failed to load mod {modName}: {e.Message}");
            }
        }
    }

    public static CustomDialogList GetDialogPackage(string name)
    {
        if (_dialogPackages.TryGetValue(name, out var pkg))
        {
            return pkg;
        }
        return null;
    }

    public static Sprite GetSprite(string relativePath, string modRoot = null)
    {
        if (string.IsNullOrEmpty(relativePath)) 
        {
            Log.LogWarning($"{LOG_TAG} GetSprite called with empty relativePath.");
            return null;
        }

        string fullPath = string.IsNullOrEmpty(modRoot) 
            ? Path.Combine(ResourceRoot, relativePath) 
            : Path.Combine(modRoot, relativePath);

        Log.LogInfo($"{LOG_TAG} Loaded sprite at {fullPath}");
        if (_spriteCache.TryGetValue(fullPath, out var sprite))
        {
            return sprite;
        }

        sprite = Utils.GetArtWork(fullPath);
        if (sprite != null)
        {
            _spriteCache[fullPath] = sprite;
        }
        else
        {
            Log.LogWarning($"{LOG_TAG} Failed to load sprite at {fullPath}");
        }
        return sprite;
    }
    
    public static IEnumerable<CharacterConfig> GetAllCharacters()
    {
        return _characterConfigs.Values;
    }
    
    public static CharacterConfig GetCharacterConfig(int id, string type)
    {
        if (_characterConfigs.TryGetValue((id, type), out var config))
        {
            return config;
        }
        return null;
    }

    public static bool ExistsCharacterConfig(int id, string type = "Special")
    {
        return _characterConfigs.ContainsKey((id, type));
    }

    public static void TryInjectSpecialPortraits()
    {
        Log.LogInfo($"{LOG_TAG} Injecting Special Portraits from ResourceEx...");
        
        foreach (var charConfig in GetAllCharacters())
        {
            string desc1 = charConfig.descriptions.Count > 0 ? charConfig.descriptions[0] : "";
            string desc2 = charConfig.descriptions.Count > 1 ? charConfig.descriptions[1] : "";
            string desc3 = charConfig.descriptions.Count > 2 ? charConfig.descriptions[2] : "";

            var val = new Il2CppSystem.ValueTuple<string, string, string, string>(
                charConfig.name,
                desc1,
                desc2,
                desc3);

            var identity = SpeakerIdentity.Identity.Unknown;
            if (!string.IsNullOrEmpty(charConfig.type) && System.Enum.TryParse<SpeakerIdentity.Identity>(charConfig.type, true, out var idt))
            {
                identity = idt;
            }

            if (identity == SpeakerIdentity.Identity.Special)
            {
                DataBaseLanguage.SpecialGuest.ForceAddOrUpdateValueTuple(charConfig.id, val);
                Log.LogInfo($"{LOG_TAG} Injected Special character: {charConfig.name} ({charConfig.id})");
            }
            else if (identity == SpeakerIdentity.Identity.Normal)
            {
                Log.LogInfo($"{LOG_TAG} Normal character detected but injection not yet implemented: {charConfig.name} ({charConfig.id})");
            }
            else if (identity == SpeakerIdentity.Identity.Self)
            {
                Log.LogInfo($"{LOG_TAG} Self character detected: {charConfig.name} ({charConfig.id})");
            }
            else
            {
                Log.LogWarning($"{LOG_TAG} Unknown character type for {charConfig.name} ({charConfig.id}): {charConfig.type}");
            }
        }
    }

    public static void TryInjectAllSpecialGuests()
    {
        Log.LogInfo($"{LOG_TAG} Injecting Special Guests from ResourceEx...");
        foreach (var charConfig in GetAllCharacters())
        {
            if (charConfig.guest != null)
            {
                Log.LogInfo($"{LOG_TAG} Injecting Special Guest for character: {charConfig.id} ({charConfig.name})");
                TryInjectSpecialGuest(charConfig);
            }
        }
    }

    public static void TryInjectAllSpecialGuestEvaluations()
    {
        Log.LogInfo($"{LOG_TAG} Injecting Special Guest Evaluations from ResourceEx...");
        foreach (var charConfig in GetAllCharacters())
        {
            if (charConfig.guest != null)
            {
                Log.LogInfo($"{LOG_TAG} Injecting Special Guest Evaluation for character: {charConfig.id} ({charConfig.name})");
                TryInjectSpecialGuestEvaluation(charConfig);
            }
        }
    }

    public static void TryInjectSpecialGuest(CharacterConfig config)
    {
        if (config.guest == null) return;

        var specialGuests = GameData.Core.Collections.CharacterUtility.DataBaseCharacter.SpecialGuest;
        if (specialGuests == null || specialGuests.Count == 0)
        {
            Log.LogError($"{LOG_TAG} DataBaseCharacter.SpecialGuest is null or empty!");
            return;
        }

        // Prepare tags
        var likeFoodTag = new Il2CppReferenceArray<SpecialGuest.WeightedTag>(config.guest.likeFoodTag.Count);
        for (int i = 0; i < config.guest.likeFoodTag.Count; i++)
        {
            likeFoodTag[i] = new SpecialGuest.WeightedTag(config.guest.likeFoodTag[i].tagId, config.guest.likeFoodTag[i].weight);
        }

        var likeBevTag = new Il2CppReferenceArray<SpecialGuest.WeightedTag>(config.guest.likeBevTag.Count);
        for (int i = 0; i < config.guest.likeBevTag.Count; i++)
        {
            likeBevTag[i] = new SpecialGuest.WeightedTag(config.guest.likeBevTag[i].tagId, config.guest.likeBevTag[i].weight);
        }

        var hateTags = new int[config.guest.hateFoodTag.Count];
        for (int i = 0; i < config.guest.hateFoodTag.Count; i++)
        {
            hateTags[i] = config.guest.hateFoodTag[i];
        }

        // Template from ID 0 or first available
        var template = specialGuests[0];

        var specialGuest = new SpecialGuest(
            config.id,
            config.label,
            new Vector2Int(config.guest.fundRangeLower, config.guest.fundRangeUpper),
            hateTags,
            likeFoodTag,
            likeBevTag,
            template.Reaction, template.Destination, template.CommisionAreaLabel,
            template.characterKizunaLevel1Welcome, template.characterKizunaLevel2Welcome, template.characterKizunaLevel3Welcome, template.characterKizunaLevel4Welcome, template.characterKizunaLevel5Welcome,
            template.characterKizunaLevel1ChatData, template.characterKizunaLevel2ChatData, template.characterKizunaLevel3ChatData, template.characterKizunaLevel4ChatData, template.characterKizunaLevel5ChatData,
            template.characterKizunaLevel2InviteSucceed, template.characterKizunaLevel2InviteFailed, template.characterKizunaLevel3InviteSucceed, template.characterKizunaLevel3InviteFailed, template.characterKizunaLevel4InviteSucceed, template.characterKizunaLevel4InviteFailed, template.characterKizunaLevel5InviteSucceed,
            template.characterKizunaLevel3RequestIngerdient, template.characterKizunaLevel4RequestIngerdient, template.characterKizunaLevel5RequestIngerdient,
            template.characterKizunaLevel4RequestBeverage, template.characterKizunaLevel5RequestBeverage,
            template.characterKizunaLevel5Commision, template.characterKizunaLevel5CommisionFinish,
            template.hideInAlbum, template.IsParticular, template.IsCollabCharacter, template.SpawnType, template.unifiedSpawnExclusion, template.unifiedSpawnWhereAfterEventOrMission, template.unifiedSpawnProb, template.m_SpecialGuestExtraDialogDataAsset, template.doNotShowInNightByDefault, template.doNotShowInChallenge, template.guestFoodEasterEggData
        );
        

        specialGuest.stringId = config.label;
        specialGuests[config.id] = specialGuest;
        Log.LogInfo($"{LOG_TAG} Injected Special Guest: {config.name} ({config.id})");

        // Food Requests
        if (config.guest.foodRequests != null)
        {
            var foodRequests = new Il2CppSystem.Collections.Generic.Dictionary<int, string>();
            foreach (var req in config.guest.foodRequests)
            {
                foodRequests.Add(req.tagId, req.request);
            }

            if (DataBaseLanguage.SpecialGuestFoodRequest != null)
            {
                DataBaseLanguage.SpecialGuestFoodRequest[config.id] = foodRequests;
            }
        }
    }

    public static void TryInjectSpecialGuestEvaluation(CharacterConfig config)
    {
        if (config.guest == null || config.guest.evaluation == null) return;

        if (NightSceneLanguage.SpecialEvaluation == null)
        {
            Log.LogWarning($"{LOG_TAG} NightSceneLanguage.SpecialEvaluation is null. Skipping evaluation injection for {config.name}.");
            return;
        }

        var evaluationLines = new Il2CppStringArray(config.guest.evaluation.Count);
        for (int i = 0; i < config.guest.evaluation.Count; i++)
        {
            evaluationLines[i] = config.guest.evaluation[i];
        }

        NightSceneLanguage.SpecialEvaluation[config.id] = evaluationLines;
        Log.LogInfo($"{LOG_TAG} Injected Special Guest Evaluation: {config.name} ({config.id})");
    }

    public static Sprite GetPortraitSprite(int characterId, int pid = 0)
    {
        var config = GetCharacterConfig(characterId, "Special");
        if (config != null && config.portraits != null && config.portraits.Count > 0)
        {
            var portrait = config.portraits.Find(p => p.pid == pid) ?? config.portraits[0];

            if (portrait != null && !string.IsNullOrEmpty(portrait.path))
            {
                return GetSprite(portrait.path, config.ModRoot);
            }
        }
        return null;
    }
}