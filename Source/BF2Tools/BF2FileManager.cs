using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public class BF2FileManager
{
    public static BF2FileManager FileManager = new BF2FileManager();

    private readonly Dictionary<string, string> _mountPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ZipArchive> _openArchives = new Dictionary<string, ZipArchive>(StringComparer.OrdinalIgnoreCase);



    public void Mount(string actualPath, string mountPath)
    {
        //actualPath = actualPath.Replace('\\', '/').Replace("C:/", "/");
        //mountPath = mountPath.Replace('\\', '/').Replace("C:/", "/");

        if (_mountPaths.TryAdd(actualPath, mountPath))
        {
            Debug.Log($"BF2FileManager : Mounted {actualPath} <==> {mountPath}");
        }
    }

    public string ReadAllText(string path)
    {
        using var stream = GetFileStream(path);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public byte[] ReadAllBytes(string path)
    {
        using var stream = GetFileStream(path);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public string[] ReadAllLines(string path)
    {
        using var stream = GetFileStream(path);
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    public IEnumerable<string> ReadLines(string path)
    {
        using var stream = GetFileStream(path);
        using var reader = new StreamReader(stream);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }

    public bool Exists(string path)
    {
        path = path.Replace('\\', '/').Replace("C:/", "/").Replace("//", "/").Replace("\"", "");

        if (File.Exists(path))
        {
            Debug.LogWarning($"Direct Path Exists : {path}");
            return true;
        }

        Debug.LogWarning($"Checking Path: {path}");
        foreach (var mountPath in _mountPaths)
        {
            string modifiedPath = GetModifiedPath(path, mountPath);
            if (modifiedPath == null) continue;

            var parts = modifiedPath.Split(new[] { ".zip" }, 2, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                if (File.Exists(modifiedPath)) return true;
                continue;
            }
            Debug.LogWarning($"PostProcessed Path: {modifiedPath}");

            var zipPath = parts[0] + ".zip";
            var filePathInZip = parts[1].TrimStart(new char[] { '\\', '/' }).Replace('\\', '/');

            if (File.Exists(zipPath))
            {
                var archive = GetOrCreateArchive(zipPath);
                if (archive.Entries.Any(entry => entry.FullName.Equals(filePathInZip, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }
        return false;
    }


    public Stream Open(string path)
    {
        //return GetFileStream(path);
        path = path.Replace('\\', '/').Replace("C:/", "/").Replace("//", "/").Replace("\"", "");

        if (File.Exists(path)){
            Debug.LogWarning($"Direct Path Exists For Open : {path}");

            return File.OpenRead(path);
        }

        foreach (var mountPath in _mountPaths)
        {
            string modifiedPath = GetModifiedPath(path, mountPath);
            if (modifiedPath == null) continue;

            var parts = modifiedPath.Split(new[] { ".zip" }, 2, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                if (File.Exists(modifiedPath)) return File.OpenRead(modifiedPath);
                continue;
            }

            var zipPath = parts[0] + ".zip";
            var filePathInZip = parts[1].TrimStart(new char[] { '\\', '/' }).Replace('\\', '/');

            var archive = GetOrCreateArchive(zipPath);
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(filePathInZip, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                var memoryStream = new MemoryStream();
                using var entryStream = entry.Open();
                entryStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
        }
        throw new FileNotFoundException($"File {path} not found");
    }

    private Stream GetFileStream(string path)
    {
        path = path.Replace('\\', '/').Replace("C:/", "/").Replace("//", "/").Replace("\"", "");

        if (File.Exists(path)){
            Debug.LogWarning($"Direct Path Exists For FileStream : {path}");
            return File.OpenRead(path);
        }

        Debug.LogWarning($"Processing Path: {path}");
        foreach (var mountPath in _mountPaths)
        {
            string modifiedPath = GetModifiedPath(path, mountPath);
            if (modifiedPath == null) continue;

            var parts = modifiedPath.Split(new[] { ".zip" }, 2, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                if (File.Exists(modifiedPath)) return File.OpenRead(modifiedPath);
                continue;
            }
            Debug.LogWarning($"PostProcessed Path: {modifiedPath}");

            var zipPath = parts[0] + ".zip";
            var filePathInZip = parts[1].TrimStart(new char[] { '\\', '/' }).Replace('\\', '/');

            var archive = GetOrCreateArchive(zipPath);
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(filePathInZip, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                var memoryStream = new MemoryStream();
                using var entryStream = entry.Open();
                entryStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
        }
        throw new FileNotFoundException($"File {path} not found");
    }

    private ZipArchive GetOrCreateArchive(string zipPath)
    {
        if (!_openArchives.TryGetValue(zipPath, out var archive))
        {
            archive = ZipFile.OpenRead(zipPath);
            _openArchives[zipPath] = archive;
        }
        return archive;
    }

    public void CloseAllArchives()
    {
        foreach (var archive in _openArchives.Values)
        {
            archive.Dispose();
        }
        _openArchives.Clear();
    }

    private string GetModifiedPath(string path, KeyValuePair<string, string> mountPath)
    {
        string modifiedPath = path;
        if (modifiedPath.StartsWith(mountPath.Value, StringComparison.OrdinalIgnoreCase))
        {
            modifiedPath = mountPath.Value == ""
                ? Path.Combine(mountPath.Key, modifiedPath)
                : modifiedPath.ReplaceFirst(mountPath.Value, mountPath.Key, StringComparison.OrdinalIgnoreCase);
        }
        else if (modifiedPath.StartsWith("/" + mountPath.Value, StringComparison.OrdinalIgnoreCase))
        {
            modifiedPath = mountPath.Value == "/"
                ? Path.Combine(mountPath.Key, modifiedPath)
                : modifiedPath.ReplaceFirst("/" + mountPath.Value, mountPath.Key, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            modifiedPath = null;
        }
        return modifiedPath;
    }
}
