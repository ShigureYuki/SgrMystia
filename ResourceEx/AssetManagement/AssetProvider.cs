using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MetaMystia.ResourceEx.AssetManagement;

/// <summary>
/// Manages all loaded resource packages and provides unified asset access
/// </summary>
public class AssetProvider
{
    private readonly List<ResourcePackage> _packages = new List<ResourcePackage>();

    public void RegisterPackage(ResourcePackage package)
    {
        _packages.Add(package);
    }

    /// <summary>
    /// Load sprite from resource package or filesystem
    /// </summary>
    public Sprite GetSprite(string relativePath, string packageRoot, Vector2 pivot, int width = 0, int height = 0, int pixelOffsetX = 0, int pixelOffsetY = 0)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            // Log: GetSprite called with empty relativePath
            return null;
        }

        byte[] imageBytes = null;
        string debugPath = "";

        // Try to load from resource package
        if (!string.IsNullOrEmpty(packageRoot) && packageRoot.Contains("|"))
        {
            var parts = packageRoot.Split('|');
            var zipPath = parts[0];

            var package = _packages.Find(p => p.ZipPath == zipPath);
            if (package != null)
            {
                imageBytes = package.GetBytes(relativePath);
                debugPath = $"{packageRoot}{relativePath}";
            }
        }
        // Fallback to filesystem
        else
        {
            string root = string.IsNullOrEmpty(packageRoot) ? ResourceExManager.ResourceRoot : packageRoot;
            string fullPath = Path.Combine(root, relativePath);
            debugPath = fullPath;

            if (File.Exists(fullPath))
            {
                imageBytes = File.ReadAllBytes(fullPath);
            }
        }

        if (imageBytes == null)
        {
            // Log: Failed to load sprite
            return null;
        }

        // Convert bytes to sprite
        var sprite = Utils.GetArtWorkFromBytes(imageBytes, pivot, width, height, pixelOffsetX, pixelOffsetY);
        if (sprite != null)
        {
            sprite.name = Path.GetFileNameWithoutExtension(relativePath);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            if (sprite.texture != null)
            {
                sprite.texture.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        else
        {
            // Log: Failed to convert bytes to sprite
        }

        return sprite;
    }

    public void Dispose()
    {
        foreach (var package in _packages)
        {
            package.Dispose();
        }
        _packages.Clear();
    }
}
