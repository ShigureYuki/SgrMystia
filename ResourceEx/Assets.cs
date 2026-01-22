using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace MetaMystia;
public static partial class ResourceExManager
{
    public static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    public static Sprite GetSprite(string relativePath, string modRoot = null, Vector2? pivot = null, int width = 0, int height = 0, int pixelOffsetX = 0, int pixelOffsetY = 0, bool useCache = true)
    {
        if (string.IsNullOrEmpty(relativePath)) 
        {
            Log.LogWarning($"GetSprite called with empty relativePath.");
            return null;
        }

        string zipPath = "";
        string internalPrefix = "";
        string locationKey = "";

        if (!string.IsNullOrEmpty(modRoot) && modRoot.Contains("|"))
        {
            var parts = modRoot.Split('|');
            zipPath = parts[0];
            if (parts.Length > 1) internalPrefix = parts[1];
            // Normalize path separators for zip entry lookup
            locationKey = $"{zipPath}|{internalPrefix}{relativePath}";
        }
        else
        {
            string root = string.IsNullOrEmpty(modRoot) ? ResourceRoot : modRoot;
            locationKey = Path.Combine(root, relativePath);
        }

        Vector2 actualPivot = pivot ?? new Vector2(0.5f, 0.5f);
        string cacheKey = $"{locationKey}_{actualPivot.x}_{actualPivot.y}_{width}_{height}_{pixelOffsetX}_{pixelOffsetY}";

        Sprite sprite = null;
        if (useCache && _spriteCache.TryGetValue(cacheKey, out sprite))
        {
            if (sprite != null && sprite.texture != null)
            {
                return sprite;
            }
            _spriteCache.Remove(cacheKey);
        }

        Log.LogInfo($"Loading sprite at {locationKey}");
        
        if (!string.IsNullOrEmpty(zipPath))
        {
            try
            {
                if (File.Exists(zipPath))
                {
                    using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                    {
                        string entryName = (internalPrefix + relativePath).Replace("\\", "/");
                        var entry = archive.GetEntry(entryName);
                        
                        if (entry == null)
                        {
                            foreach(var e in archive.Entries)
                            {
                                if (e.FullName.Equals(entryName, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    entry = e;
                                    break;
                                }
                            }
                        }

                        if (entry != null)
                        {
                            using (var stream = entry.Open())
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                sprite = Utils.GetArtWorkFromBytes(ms.ToArray(), actualPivot, width, height, pixelOffsetX, pixelOffsetY);
                                if (sprite != null) sprite.name = Path.GetFileNameWithoutExtension(relativePath);
                            }
                        }
                        else
                        {
                            Log.LogWarning($"Entry {entryName} not found in zip {zipPath}");
                        }
                    }
                }
                else
                {
                     Log.LogWarning($"Zip file not found: {zipPath}");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to load from zip: {ex.Message}");
            }
        }
        else
        {
            sprite = Utils.GetArtWork(locationKey, actualPivot, width, height, pixelOffsetX, pixelOffsetY);
        }
        
        if (sprite != null)
        {
            sprite.hideFlags = HideFlags.HideAndDontSave;
            if (sprite.texture != null)
            {
                sprite.texture.hideFlags = HideFlags.HideAndDontSave;
            }
            _spriteCache[cacheKey] = sprite;
        }
        else
        {
            Log.LogWarning($"Failed to load sprite at {locationKey}");
        }
        return sprite;
    }

    public static void PreloadAllImages()
    {
        Log.LogInfo($"Preloading all images...");
        foreach (var charConfig in GetAllCharacterConfigs())
        {
            // Preload Portraits
            if (charConfig.portraits != null)
            {
                foreach (var portrait in charConfig.portraits)
                {
                    if (!string.IsNullOrEmpty(portrait.path))
                    {
                        // Default params for portraits: pivot (0.5, 0.5), no resize
                        GetSprite(portrait.path, charConfig.ModRoot); 
                    }
                }
            }

            // Preload SpriteSetCompact
            if (charConfig.characterSpriteSetCompact != null)
            {
                var config = charConfig.characterSpriteSetCompact;
                if (config.mainSprite != null)
                {
                    foreach (var path in config.mainSprite)
                    {
                        if (!string.IsNullOrEmpty(path))
                            // Params for SpriteSetCompact: pivot (0.5, 0.0), 64x64 resize
                            GetSprite(path, charConfig.ModRoot, new Vector2(0.5f, 0.0f), 64, 64);
                    }
                }
                if (config.eyeSprite != null)
                {
                    foreach (var path in config.eyeSprite)
                    {
                        if (!string.IsNullOrEmpty(path))
                            // Params for SpriteSetCompact: pivot (0.5, 0.0), 64x64 resize
                            GetSprite(path, charConfig.ModRoot, new Vector2(0.5f, 0.0f), 64, 64);
                    }
                }
            }
        }
        Log.LogInfo($"Preloading complete. Cache size: {_spriteCache.Count}");
    }
}