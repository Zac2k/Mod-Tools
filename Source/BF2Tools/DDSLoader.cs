using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using static BF2FileManager;
using static CWModUtility;
using Debug = UnityEngine.Debug;

public static class DDSLoader
{
    private static Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private static Dictionary<string, Cubemap> CubemapCache = new Dictionary<string, Cubemap>();
    private static readonly string TexconvPath;

    static DDSLoader()
    {
        TexconvPath = FindTexconv();
        if (string.IsNullOrEmpty(TexconvPath))
        {
            Debug.LogError("texconv.exe not found in the working directory or its subfolders.");
        }
    }

    private static string FindTexconv()
    {
        string workingDirectory = "Assets/";
        string[] files = Directory.GetFiles(workingDirectory, "texconv.exe", SearchOption.AllDirectories);

        return files.Length > 0 ? files[0] : null;
    }

    public static Texture2D Fallback(string ddsFilePath, bool mipchain, bool linear = false)
    {
        if (!ddsFilePath.EndsWith("dds", StringComparison.OrdinalIgnoreCase)) ddsFilePath += ".dds";

        if (textureCache.TryGetValue(ddsFilePath, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        if (!FileManager.Exists(ddsFilePath))
        {
            Debug.LogError("DDS file not found at: " + ddsFilePath);
            return null;
        }

        byte[] ddsBytes = FileManager.ReadAllBytes(ddsFilePath);

        File.WriteAllBytes(Path.Combine(Application.temporaryCachePath, "TMPDDS.dds"), ddsBytes);
        ddsFilePath = Path.Combine(Application.temporaryCachePath, "TMPDDS.dds");

        if (string.IsNullOrEmpty(TexconvPath))
        {
            Debug.LogError("texconv.exe not found.");
            return null;
        }

        string outputPngPath = Path.ChangeExtension(ddsFilePath, ".png");

        // Convert DDS to PNG
        if (!ConvertDdsToPng(ddsFilePath, outputPngPath))
        {
            Debug.LogError($"Failed to convert DDS to PNG.     Path : {ddsFilePath}");
            return null;
        }

        // Load PNG texture into Unity
        byte[] pngBytes = File.ReadAllBytes(outputPngPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, mipchain, linear);
        texture.name = Path.GetFileNameWithoutExtension(ddsFilePath);

        if (!texture.LoadImage(pngBytes))
        {
            Debug.LogError($"Failed to load PNG texture.     Path : {ddsFilePath}");
            return null;
        }

        // Clean up the temporary PNG file
        File.Delete(ddsFilePath);
        File.Delete(outputPngPath);

        return texture;
    }

    private static bool ConvertDdsToPng(string ddsFilePath, string outputPngPath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = TexconvPath,
            Arguments = $"-ft PNG -o \"{Path.GetDirectoryName(outputPngPath)}\" \"{ddsFilePath}\"",
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

    public static Texture2D LoadDDSTexture(string ddsFilePath, bool compressed = true, bool linear = false)
    {
        if (!Directory.Exists("Assets/Cache/Textures/")) Directory.CreateDirectory("Assets/Cache/Textures/");
        bool UseFallback = false;
        if (!ddsFilePath.EndsWith("dds", StringComparison.OrdinalIgnoreCase)) ddsFilePath += ".dds";

        if (textureCache.TryGetValue(ddsFilePath, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }
        else
        {
            cachedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cache/Textures/" + ddsFilePath.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Texture2D");
            if (cachedTexture)
            {
                textureCache.Add(ddsFilePath, cachedTexture);
                return cachedTexture;
            }
        }

        if (!FileManager.Exists(ddsFilePath))
        {
            Debug.LogError("DDS file not found at: " + ddsFilePath);
            return null;
        }

        byte[] ddsBytes = FileManager.ReadAllBytes(ddsFilePath);
        if (ddsBytes == null || ddsBytes.Length < 128)
        {
            Debug.LogError($"Invalid DDS file     Path : {ddsFilePath}");
            return null;
        }

        byte ddsSizeCheck = ddsBytes[4];
        if (ddsSizeCheck != 124)
        {
            Debug.LogError($"Invalid DDS file size     Path : {ddsFilePath}");
            return null;
        }

        // Read header values
        int height = BitConverter.ToInt32(ddsBytes, 12);
        int width = BitConverter.ToInt32(ddsBytes, 16);
        int mipMapCount = BitConverter.ToInt32(ddsBytes, 28);
        int fourCC = BitConverter.ToInt32(ddsBytes, 84);

        Debug.Log($"Width: {width}, Height: {height}, MipMapCount: {mipMapCount}, FourCC: {fourCC}");

        TextureFormat textureFormat = TextureFormat.RGBA32;
        switch (fourCC)
        {
            case 0x31545844: // "DXT1" in ASCII
                textureFormat = TextureFormat.DXT1;
                break;
            case 0x33545844: // "DXT3" in ASCII
                textureFormat = TextureFormat.DXT5;
                break;
            case 0x35545844: // "DXT5" in ASCII
                textureFormat = TextureFormat.DXT5;
                break;
            default:
                Debug.LogError($"Unsupported DDS format     Path : {ddsFilePath}");
                UseFallback = true;
                break;
        }


        //Debug.Log($"Creating Texture2D with Width: {width}, Height: {height}, MipMapCount: {mipMapCount}, DataLength: {dataLength}");

        Texture2D texture = null;

        if (UseFallback)
        {
            texture = Fallback(ddsFilePath, true, linear);
        }
        else
        {
            texture = new Texture2D(width, height, textureFormat, mipMapCount > 1, linear);
            try
            {
                int dataStartIndex = 128; // DDS header is 128 bytes
                int dataLength = ddsBytes.Length - dataStartIndex;
                byte[] ddsData = new byte[dataLength];
                Buffer.BlockCopy(ddsBytes, dataStartIndex, ddsData, 0, dataLength);
                texture.LoadRawTextureData(ddsData);
            }
            catch
            {
                Debug.LogError($"Failed To Load RawDDS Data : Attempting To Load With Alternate     Path : {ddsFilePath}");
                try
                {
                    //texture.LoadRawTextureData(ddsBytes);
                    texture = Fallback(ddsFilePath, true, linear);
                }
                catch
                {
                    Debug.LogError($"Alternative Has Failed : Attempting To Use Fallback     Path : {ddsFilePath}");
                    //texture = Fallback(ddsFilePath,true,linear);
                }

            }
            texture.Apply();
            texture = FlipTexture(texture, Path.GetFileNameWithoutExtension(ddsFilePath));
        }
        if (compressed)
        {
            //texture.Compress(true);
            EditorUtility.CompressTexture(texture, MapLoader.Instance.TextureCompressionFormat, TextureCompressionQuality.Normal);
        }
        else texture = texture.GetReadable(width, height, linear);

        texture.name = Path.GetFileNameWithoutExtension(ddsFilePath);
        textureCache.Add(ddsFilePath, texture);
        AssetDatabase.CreateAsset(texture, "Assets/Cache/Textures/" + ddsFilePath.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Texture2D");
        texture.name = "Assets/Cache/Textures/" + ddsFilePath.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Texture2D";
        AssetDatabase.Refresh();
        return texture;
    }


    public static Cubemap LoadDDSCubemap(string ddsFilePath, bool compressed = true, bool linear = false)
    {
        if (!Directory.Exists("Assets/Cache/Textures/")) Directory.CreateDirectory("Assets/Cache/Textures/");
        if (!ddsFilePath.EndsWith("dds", StringComparison.OrdinalIgnoreCase)) ddsFilePath += ".dds";

        if (CubemapCache.TryGetValue(ddsFilePath, out Cubemap cachedTexture))
        {
            return cachedTexture;
        }
        else
        {
            cachedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Cache/Textures/" + ddsFilePath.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Cubemap");
            if (cachedTexture)
            {
                CubemapCache.Add(ddsFilePath, cachedTexture);
                return cachedTexture;
            }
        }

        if (!FileManager.Exists(ddsFilePath))
        {
            Debug.LogError("DDS file not found at: " + ddsFilePath);
            return null;
        }

        byte[] ddsBytes = FileManager.ReadAllBytes(ddsFilePath);
        if (ddsBytes == null || ddsBytes.Length < 128)
        {
            Debug.LogError($"Invalid DDS file     Path : {ddsFilePath}");
            return null;
        }


        Cubemap texture = null;
        string cachePath = "Assets/Cache/Textures/" + ddsFilePath.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Cubemap";
        string ddscachePath = "Assets/Cache/Textures/" + "DDS_" + ddsFilePath.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
        File.WriteAllBytes(ddscachePath, ddsBytes);
        AssetDatabase.Refresh();
        texture = AssetDatabase.LoadAssetAtPath<Cubemap>(ddscachePath);

        if (texture)
        {
            texture = texture.GetReadable();

            texture.name = Path.GetFileNameWithoutExtension(ddsFilePath);
            CubemapCache.Add(ddsFilePath, texture);
            AssetDatabase.CreateAsset(texture, cachePath);
            texture.name = cachePath;
            AssetDatabase.Refresh();

            SerializedObject cubemapObject = new SerializedObject(AssetDatabase.LoadAssetAtPath<Cubemap>(cachePath));

            // Access the m_ColorSpace property
            SerializedProperty colorSpaceProp = cubemapObject.FindProperty("m_ColorSpace");

            if (colorSpaceProp != null)
            {
                // Set m_ColorSpace to 1 (sRGB)
                colorSpaceProp.intValue = 1;
                cubemapObject.ApplyModifiedProperties();
                Debug.Log($"Cubemap '{texture.name}' color space set to sRGB.");
            }
            else
            {
                Debug.LogWarning("Could not find m_ColorSpace property on the cubemap.");
            }

            /*if (compressed)
            {
                //texture.Compress(true);
                EditorUtility.CompressCubemapTexture(texture, MapLoader.Instance.TextureCompressionFormat, TextureCompressionQuality.Normal);
            }
            else
                EditorUtility.CompressCubemapTexture(texture, TextureFormat.ARGB32, TextureCompressionQuality.Normal);
*/
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }
        else
        {
            Debug.LogError("Failed To Load DDS Cubemap");
        }
        return texture;
    }



    public static Texture2D FlipTexture(Texture2D sourceTexture, string texName)
    {
        Material BlendMat = new Material(Shader.Find("Hidden/FlipVertical"));
        BlendMat.SetTexture("_MainTex", sourceTexture);

        RenderTexture rt = RenderTexture.GetTemporary((int)sourceTexture.width, (int)sourceTexture.height);
        //rt.enableRandomWrite = true;
        rt.Create();

        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(null, rt, BlendMat);
        Texture2D nTex = new Texture2D((int)sourceTexture.width, (int)sourceTexture.height);
        nTex.name = texName;
        nTex.ReadPixels(new Rect(0, 0, (int)sourceTexture.width, (int)sourceTexture.height), 0, 0);
        nTex.filterMode = sourceTexture.filterMode;
        nTex.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        EditorUtility.CompressTexture(nTex, MapLoader.Instance.TextureCompressionFormat, TextureCompressionQuality.Fast);
        return nTex;
    }
}
