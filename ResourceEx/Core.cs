using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using BepInEx;
using Common.DialogUtility;
using GameData.Profile;

using MetaMystia.ResourceEx.Models;
using MetaMystia.ResourceEx.AssetManagement;

namespace MetaMystia;


[AutoLog]
public static partial class ResourceExManager
{
    // Abstracted resource root path
    public static string ResourceRoot { get; set; } = Path.Combine(Paths.GameRootPath, "ResourceEx");

    // Asset provider for efficient resource package access
    private static readonly AssetProvider _assetProvider = new AssetProvider();

    private static Dictionary<(int id, string type), CharacterConfig> _characterConfigs = new Dictionary<(int id, string type), CharacterConfig>();
    private static Dictionary<string, CustomDialogList> _dialogPackageConfigs = new Dictionary<string, CustomDialogList>();
    private static Dictionary<string, DialogPackage> _builtDialogPackages = new Dictionary<string, DialogPackage>();

    private static Dictionary<int, IngredientConfig> IngredientConfigs = new Dictionary<int, IngredientConfig>();
    private static Dictionary<int, FoodConfig> FoodConfigs = new Dictionary<int, FoodConfig>();
    private static Dictionary<int, BeverageConfig> BeverageConfigs = new Dictionary<int, BeverageConfig>();
    private static Dictionary<int, RecipeConfig> RecipeConfigs = new Dictionary<int, RecipeConfig>();
    private static List<MissionNodeConfig> MissionNodeConfigs = new List<MissionNodeConfig>();
    private static List<EventNodeConfig> EventNodeConfigs = new List<EventNodeConfig>();

    private static readonly string DialogPackageNamePrefix = "";

    public static void Initialize()
    {
        LoadConfigs();
        PreloadAllImages();
    }

    // 加载逻辑
    // DataBaseCore -> DataBaseScheduler -> DataBaseCharacter -> DataBaseLanguage -> DataBaseDay

    public static void OnDataBaseCoreInitialized()
    {
        RegisterAllSpawnConfigs();
        RegisterAllIngredients();
        RegisterAllBeverages();
        RegisterAllRecipes();
        RegisterAllFoods();
    }
    public static void OnDataBaseDayInitialized()
    {
        RegisterNPCs();
    }
    public static void OnDataBaseLanguageInitialized()
    {
        RegisterAllFoodRequests();
        RegisterAllBevRequests();
        RegisterSpecialPortraits();
        RegisterAllIngredientLanguages();
        RegisterAllBeverageLanguages();
        RegisterAllFoodLanguages();
        RegisterAllMissionNodeLanguages();
    }

    public static void OnDataBaseCharacterInitialized()
    {
        BuildAllDialogPackages();
        RegisterAllSpecialGuestPairs();
        RegisterAllSpecialGuests();

        RegisterAllMissionNodes(); // 依赖 Dialog
        RegisterAllEventNodes(); // 依赖 Dialog
    }

    public static void OnDataBaseAchievementInitialized()
    {
        // Currently no actions needed here
    }
    public static void OnDataBaseSchedulerInitialized()
    {
        // RegisterAllMissionNodes(); // 依赖 Dialog
        // RegisterAllEventNodes(); // 依赖 Dialog
    }
    public static void OnNightSceneLanguageInitialized()
    {
        RegisterAllConversations();
        RegisterAllEvaluations();
    }

    public static void OnDaySceneLanguageInitialized()
    {
        // Currently no actions needed here
    }

    public static void OnDaySceneAwake()
    {
        InitializeAllDaySpawnConfigs();
        ActivateAllKizunaEventNodes();
    }

    private class PackCandidate
    {
        public string ZipPath;
        public string PackageName;
        public ResourceConfig Config;
        public string InternalPrefix;
    }

    private static System.Version GetVersion(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return new System.Version(0, 0, 0);
        if (System.Version.TryParse(v, out var result)) return result;
        return new System.Version(0, 0, 0);
    }

    private static void LoadConfigs()
    {
        if (!Directory.Exists(ResourceRoot))
        {
            Directory.CreateDirectory(ResourceRoot);
            Log.LogInfo($"Created ResourceEx directory at {ResourceRoot}");
            return;
        }

        var zipFiles = Directory.GetFiles(ResourceRoot, "*.zip");
        var candidates = new List<PackCandidate>();

        foreach (var zipPath in zipFiles)
        {
            string packageName = Path.GetFileNameWithoutExtension(zipPath);
            Log.LogInfo($"Scanning resource package: {packageName} from {zipPath}");

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    ZipArchiveEntry configEntry = null;
                    string internalPrefix = "";

                    // Find ResourceEx.json
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith("ResourceEx.json", System.StringComparison.OrdinalIgnoreCase))
                        {
                            // Prefer shorter path (root level)
                            if (configEntry == null || entry.FullName.Length < configEntry.FullName.Length)
                            {
                                configEntry = entry;
                            }
                        }
                    }

                    if (configEntry == null)
                    {
                        Log.LogWarning($"[{packageName}] ResourceEx.json not found in zip.");
                        continue;
                    }

                    string entryName = configEntry.FullName;
                    if (entryName.EndsWith("ResourceEx.json", System.StringComparison.OrdinalIgnoreCase))
                    {
                        internalPrefix = entryName.Substring(0, entryName.Length - "ResourceEx.json".Length);
                    }

                    // Log.LogInfo($"[{modName}] Found config at {configEntry.FullName}, Prefix: '{internalPrefix}'");

                    string jsonString;
                    using (var stream = configEntry.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        jsonString = reader.ReadToEnd();
                    }

                    var options = new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true
                    };
                    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                    var config = JsonSerializer.Deserialize<ResourceConfig>(jsonString, options);

                    candidates.Add(new PackCandidate
                    {
                        ZipPath = zipPath,
                        PackageName = packageName,
                        Config = config,
                        InternalPrefix = internalPrefix
                    });
                }
            }
            catch (System.Exception e)
            {
                Log.LogError($"Failed to load resource package {packageName}: {e.Message}");
            }
        }

        var finalPacks = candidates
            .Where(c => string.IsNullOrEmpty(c.Config?.packInfo?.label))
            .ToList();

        var resolvedPacks = candidates
            .Where(c => !string.IsNullOrEmpty(c.Config?.packInfo?.label))
            .GroupBy(c => c.Config.packInfo.label)
            .Select(group =>
            {
                var sorted = group
                    .Select(c => new { Candidate = c, Version = GetVersion(c.Config.packInfo.version) })
                    .OrderByDescending(x => x.Version)
                    .ToList();

                var winner = sorted.First();

                if (sorted.Count > 1)
                {
                    Log.LogWarning($"[ResourceEx] Label Conflict: '{group.Key}'. Selected '{winner.Candidate.PackageName}' (v{winner.Version}) over others: {string.Join(", ", sorted.Skip(1).Select(s => s.Candidate.PackageName))}");
                }

                return winner.Candidate;
            });

        finalPacks.AddRange(resolvedPacks);

        foreach (var pack in finalPacks)
        {
            if (pack.Config?.packInfo != null)
            {
                var info = pack.Config.packInfo;
                string authors = info.authors != null ? string.Join(", ", info.authors) : "Unknown";
                Log.LogMessage($"Loaded Resource Pack: {info.name} [{info.label}] v{info.version} by {authors}");
            }
            else
            {
                Log.LogInfo($"Loading resource package: {pack.PackageName} from {pack.ZipPath}");
            }

            ApplyConfig(pack);
        }
    }

    private static void ApplyConfig(PackCandidate pack)
    {
        var config = pack.Config;
        string packageName = pack.PackageName;
        string packageRootInfo = $"{pack.ZipPath}|{pack.InternalPrefix}";

        // Register resource package to asset provider for memory-cached access
        try
        {
            var resourcePackage = new ResourcePackage(pack.ZipPath, pack.InternalPrefix);
            _assetProvider.RegisterPackage(resourcePackage);
            Log.LogInfo($"[{packageName}] Loaded ZIP into memory, size: {new FileInfo(pack.ZipPath).Length / 1024} KB");
        }
        catch (System.Exception ex)
        {
            Log.LogError($"[{packageName}] Failed to load ZIP into memory: {ex.Message}");
        }

        if (config?.characters != null)
        {
            foreach (var charConfig in config.characters)
            {
                charConfig.PackageRoot = packageRootInfo;
                _characterConfigs[(charConfig.id, charConfig.type)] = charConfig;
                Log.LogInfo($"[{packageName}] Loaded config for character {charConfig.name} ({charConfig.id}, {charConfig.type})");
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
                _dialogPackageConfigs[pkgConfig.name] = dialogList;
                Log.LogInfo($"[{packageName}] Loaded dialog package: {pkgConfig.name}");
            }
        }

        if (config?.ingredients != null)
        {
            foreach (var ingredientConfig in config.ingredients)
            {
                ingredientConfig.PackageRoot = packageRootInfo;
                IngredientConfigs[ingredientConfig.id] = ingredientConfig;
                Log.LogInfo($"[{packageName}] Loaded config for ingredient {ingredientConfig.id}");
            }
        }

        if (config?.foods != null)
        {
            foreach (var foodConfig in config.foods)
            {
                foodConfig.PackageRoot = packageRootInfo;
                FoodConfigs[foodConfig.id] = foodConfig;
                Log.LogInfo($"[{packageName}] Loaded config for food {foodConfig.name} ({foodConfig.id})");
            }
        }

        if (config?.beverages != null)
        {
            foreach (var beverageConfig in config.beverages)
            {
                beverageConfig.PackageRoot = packageRootInfo;
                BeverageConfigs[beverageConfig.id] = beverageConfig;
                Log.LogInfo($"[{packageName}] Loaded config for beverage {beverageConfig.name} ({beverageConfig.id})");
            }
        }
        if (config?.recipes != null)
        {
            foreach (var recipeConfig in config.recipes)
            {
                RecipeConfigs[recipeConfig.id] = recipeConfig;
                Log.LogInfo($"[{packageName}] Loaded config for recipe {recipeConfig.id}");
            }
        }

        if (config?.missionNodes != null)
        {
            foreach (var missionNodeConfig in config.missionNodes)
            {
                // missionNodeConfig.PackageRoot = packageRootInfo;
                MissionNodeConfigs.Add(missionNodeConfig);
                Log.LogInfo($"[{packageName}] Loaded config for mission node {missionNodeConfig.title}");
            }
        }

        if (config?.eventNodes != null)
        {
            foreach (var eventNodeConfig in config.eventNodes)
            {
                // eventNodeConfig.PackageRoot = packageRootInfo;
                EventNodeConfigs.Add(eventNodeConfig);
                Log.LogInfo($"[{packageName}] Loaded config for event node {eventNodeConfig.debugLabel}");
            }
        }
    }
}
