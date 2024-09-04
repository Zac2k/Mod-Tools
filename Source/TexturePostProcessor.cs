using UnityEngine;
using UnityEditor;

/*public class LimitedMipmapsTexturePostprocessor : AssetPostprocessor
{
    void OnPostprocessTexture(Texture2D texture)
    {
        if (!IsTextureImportedAutomatically())
            return;

        // Customize the parameters based on your needs
        int mipmapCount = 4;
        TextureFormat format = TextureFormat.RGBA32;
        bool isLinear = true;
        int anisoLevel = 2;
        FilterMode filterMode = FilterMode.Bilinear;

        Texture2D limitedMips = CreateLimitedMipmapsTexture(texture, format, mipmapCount, isLinear, anisoLevel, filterMode);

        // Replace the imported texture with the modified one
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(limitedMips, assetPath);
        AssetDatabase.Refresh();
    }

    bool IsTextureImportedAutomatically()
    {
        // Check if the texture is imported automatically by Unity (not manually dragged into the project)
        return assetImporter.importSettingsMissing;
    }

        public static Texture2D CreateLimitedMipmapsTexture(Texture2D sourceTexture, TextureFormat format, int mipmapCount, bool isLinear, int anisoLevel, FilterMode filterMode)
        {
            var texturePath = AssetDatabase.GetAssetPath(sourceTexture);
         
            var limitedMips = new Texture2D(sourceTexture.width, sourceTexture.width, format, mipmapCount, isLinear);
            limitedMips.anisoLevel = anisoLevel;
            limitedMips.filterMode = filterMode;
         
            for (var i = 0; i < mipmapCount; i++)
            {
                Graphics.CopyTexture(sourceTexture, 0, i, limitedMips, 0, i);
            }
         
            var outputPath = $"{texturePath.Replace($"{sourceTexture.name}.png", $"{sourceTexture.name}__LimitedMipCount-{mipmapCount}.asset")}";
         
            AssetDatabase.CreateAsset(limitedMips, outputPath);
            AssetDatabase.SaveAssets();
         
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }
}
*/