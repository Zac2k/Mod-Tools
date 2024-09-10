using System;
using System.Diagnostics;
using System.IO;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public static class Texture2DExtensions
{
    private static readonly string TexconvPath;

    static Texture2DExtensions()
    { 
        TexconvPath = FindTexconv();
        if (string.IsNullOrEmpty(TexconvPath))
        {
            Debug.LogError("texconv.exe not found in the working directory or its subfolders.");
        }
    }

    private static string FindTexconv()
    {
        string workingDirectory = Application.dataPath; // Changed to Application.dataPath for Unity
        string[] files = Directory.GetFiles(workingDirectory, "texconv.exe", SearchOption.AllDirectories);
        return files.Length > 0 ? files[0] : null;
    }

    public static bool LoadTGA(this Texture2D texture, byte[] data, bool markNonReadable = false)
    {
        if (!string.IsNullOrEmpty(TexconvPath))
        {
            // Create a temporary TGA file
            string tempTGAPath = Path.Combine(Application.temporaryCachePath, "TMP.tga");
            File.WriteAllBytes(tempTGAPath, data);

            // Convert TGA to PNG
            string outputPngPath = Path.ChangeExtension(tempTGAPath, ".png");
            if (!ConvertTgaToPng(tempTGAPath, outputPngPath))
            {
                Debug.LogError($"Failed to convert TGA to PNG. Path: {tempTGAPath}");
                return false;
            }

            // Load PNG texture into Unity
            byte[] pngBytes = File.ReadAllBytes(outputPngPath);
            if (!texture.LoadImage(pngBytes, markNonReadable))
            {
                Debug.LogError($"Failed to load PNG texture. Path: {outputPngPath}");
                return false;
            }

            // Clean up the temporary files
            File.Delete(tempTGAPath);
            File.Delete(outputPngPath);

            return true;
        }
        else
        {
            Debug.LogError("texconv.exe not found.");
            return false;
        }
    }

    private static bool ConvertTgaToPng(string tgaFilePath, string outputPngPath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = TexconvPath,
            Arguments = $"-ft PNG -o \"{Path.GetDirectoryName(outputPngPath)}\" \"{tgaFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = startInfo })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Debug.LogError($"texconv error: {error}");
                return false;
            }
        }

        return true;
    }
}
