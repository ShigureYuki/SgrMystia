using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace MetaMystia.ResourceEx.AssetManagement;

/// <summary>
/// Represents a loaded resource package with cached ZIP content in memory
/// </summary>
public class ResourcePackage : IDisposable
{
    public string ZipPath { get; }
    public string InternalPrefix { get; }
    public string PackageRoot => $"{ZipPath}|{InternalPrefix}";

    private readonly MemoryStream _zipMemoryStream;
    private readonly ZipArchive _zipArchive;
    private readonly Dictionary<string, ZipArchiveEntry> _entryIndex;

    /// <summary>
    /// Creates a resource package from a ZIP file path (reads from disk)
    /// </summary>
    public ResourcePackage(string zipPath, string internalPrefix)
        : this(zipPath, internalPrefix, File.ReadAllBytes(zipPath))
    {
    }

    /// <summary>
    /// Creates a resource package from pre-loaded ZIP bytes (optimized, no disk IO)
    /// </summary>
    public ResourcePackage(string zipPath, string internalPrefix, byte[] zipBytes)
    {
        ZipPath = zipPath;
        InternalPrefix = internalPrefix;

        // Load ZIP from memory
        _zipMemoryStream = new MemoryStream(zipBytes);
        _zipArchive = new ZipArchive(_zipMemoryStream, ZipArchiveMode.Read, leaveOpen: false);

        // Build case-insensitive entry index for O(1) lookup
        _entryIndex = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in _zipArchive.Entries)
        {
            _entryIndex[entry.FullName] = entry;
        }
    }

    /// <summary>
    /// Get raw bytes from a file in the ZIP
    /// </summary>
    public byte[] GetBytes(string relativePath)
    {
        var entryName = (InternalPrefix + relativePath).Replace("\\", "/");

        if (_entryIndex.TryGetValue(entryName, out var entry))
        {
            using (var stream = entry.Open())
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a file exists in the ZIP
    /// </summary>
    public bool FileExists(string relativePath)
    {
        var entryName = (InternalPrefix + relativePath).Replace("\\", "/");
        return _entryIndex.ContainsKey(entryName);
    }

    public void Dispose()
    {
        _zipArchive?.Dispose();
        _zipMemoryStream?.Dispose();
    }
}
