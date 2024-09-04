using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class TextureSet
{
    public TerrainLayer[] terrainLayers = new TerrainLayer[0];
    public Texture2D splatmap; // Add the splatmap texture
}

public class CombineTerrainMap : MonoBehaviour
{
#if UNITY_EDITOR
    public TextureSet textureSet;
    public Terrain terrain;
    public int outputResolution = 1024;
    public string outputPath = "Assets/Textures/";

    [Button("Generate BaseMap")]
    void CombineTextures()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain  is not assigned ot is null.");
            return;
        }
        textureSet.splatmap=terrain.terrainData.alphamapTextures[0];
        textureSet.terrainLayers=terrain.terrainData.terrainLayers;

        Texture2D combinedTexture = new Texture2D(outputResolution, outputResolution);

        Color[] splatmapPixels = textureSet.splatmap.GetPixels();
        int splatmapWidth = textureSet.splatmap.width;

        TerrainLayer[] terrainLayers = terrain.terrainData.terrainLayers;

        for (int i = 0; i < textureSet.terrainLayers.Length; i++)
        {
            TerrainLayer layer = textureSet.terrainLayers[i];

            if (layer == null)
            {
                Debug.LogWarning("TerrainLayer at index " + i + " is not assigned.");
                continue;
            }

            int textureWidth = Mathf.FloorToInt(layer.tileSize.x * outputResolution);
            int textureHeight = Mathf.FloorToInt(layer.tileSize.y * outputResolution);

            Texture2D resizedTexture = ResizeTerrainLayerTexture(layer);

            // Combine the textures based on the splatmap
            float[] splatmapChannelPixels = new float[splatmapPixels.Length];
            for(int j=0; j<splatmapPixels.Length; j++)splatmapChannelPixels[j]=i==0?splatmapPixels[j].r:i==1?splatmapPixels[j].g:i==2?splatmapPixels[j].b:splatmapPixels[j].a;
        //System.IO.File.WriteAllBytes(outputPath + layer.name+".png", resizedTexture.EncodeToPNG());
            CombineTextures(combinedTexture, resizedTexture, splatmapChannelPixels, splatmapWidth);
        }

        // Apply changes and save the combined texture
        combinedTexture.Apply();

        byte[] bytes = combinedTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(outputPath + "BaseMap.png", bytes);

        Debug.Log("Combined texture exported to: " + outputPath + "CombinedTexture.png");
    }

Texture2D ResizeTerrainLayerTexture(TerrainLayer terrainLayer)
{

        Texture2D resultTexture = new Texture2D((int)outputResolution, (int)outputResolution);
        Color[] blendPixels = GetReadable(terrainLayer.diffuseTexture,outputResolution,outputResolution).GetPixels();
        Vector2 tileAmount = new Vector2(terrain.terrainData.size.x/terrainLayer.tileSize.x,terrain.terrainData.size.y/terrainLayer.tileSize.y);


        //System.IO.File.WriteAllBytes(outputPath + terrainLayer.name+"_Raw.png", GetReadable(terrainLayer.diffuseTexture,terrainLayer.diffuseTexture.width,terrainLayer.diffuseTexture.height).EncodeToPNG());

        for (int y = 0; y < resultTexture.height; y++)
        {
            for (int x = 0; x < resultTexture.width; x++)
            {
                float blendX = (x / (float)resultTexture.width) * (outputResolution * (terrain.terrainData.size.x/terrainLayer.tileSize.x));
                float blendY = (y / (float)resultTexture.height) * (outputResolution * (terrain.terrainData.size.y/terrainLayer.tileSize.y));
                int blendPixelIndex = (int)(Mathf.FloorToInt(blendY) * outputResolution + Mathf.FloorToInt(blendX));
                Color blendColor = blendPixels[blendPixelIndex % blendPixels.Length];

                resultTexture.SetPixel(x, y, blendColor);

            
            }
        }

        resultTexture.Apply();
        //resultTexture.Compress(true);

        // Save the result as a new image
        //byte[] bytes = resultTexture.EncodeToPNG();
       // System.IO.File.WriteAllBytes(outputPath, bytes);
        return resultTexture;
       // Debug.Log("Blended texture saved to: " + outputPath);
}


    void CombineTextures(Texture2D destination, Texture2D source, float[] splatmapChannelPixels, int splatmapWidth)
    {
        Color[] sourcePixels = source.GetPixels();
        Color[] combinedPixels = destination.GetPixels();

        for (int y = 0; y < destination.height; y++)
        {
            for (int x = 0; x < destination.width; x++)
            {
                int splatmapX = Mathf.FloorToInt(x * (splatmapWidth / (float)destination.width));
                int splatmapY = Mathf.FloorToInt(y * (splatmapWidth / (float)destination.height));
                int splatmapIndex =  splatmapY * splatmapWidth + splatmapX;

                // Assuming the splatmap is grayscale
                float weight = splatmapChannelPixels[splatmapIndex];

                Color sourcePixel = sourcePixels[y * source.width + x];
                Color combinedPixel = combinedPixels[y * destination.width + x];

                // Combine based on the splatmap weight
                combinedPixels[y * destination.width + x] = Color.Lerp(combinedPixel, sourcePixel, weight);
            }
        }

        destination.SetPixels(combinedPixels);
    }

    
            public static Texture2D GetReadable(Texture2D texture,int W,int H){
    texture.filterMode = FilterMode.Point;
		RenderTexture rt = RenderTexture.GetTemporary (W, H);
		rt.filterMode = FilterMode.Point;
		RenderTexture.active = rt;
    Graphics.Blit (texture, rt);
		Texture2D nTex = new Texture2D (W, H);
		nTex.ReadPixels (new Rect (0, 0, W, H), 0, 0);
		nTex.Apply ();
		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary (rt);
		return nTex;
        }
        #endif
}
