using UnityEngine;
using MessagePack;
using System;
using System.Collections.Generic;
using static CWModUtility;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.AI;
using System.IO;
using Unity.AI.Navigation;
using UnityEngine.Rendering.PostProcessing;

public static class Extensions
{
    public static string ReplaceFirst(this string text, string search, string replace, StringComparison comparison = StringComparison.Ordinal)
    {
        int pos = text.IndexOf(search, comparison);
        if (pos < 0)
        {
            return text;
        }
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    public static void LoadRawHeightmap(this TerrainData terrainData, byte[] rawHeightmapBytes, int resolution, int depth)
    {
        float[,] heights = new float[resolution, resolution];

        int index = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                if (depth == 16)
                {
                    ushort height = System.BitConverter.ToUInt16(rawHeightmapBytes, index);
                    heights[y, x] = height / (float)ushort.MaxValue;
                    index += 2;
                }
                else if (depth == 8)
                {
                    byte height = rawHeightmapBytes[index];
                    heights[y, x] = height / (float)byte.MaxValue;
                    index += 1;
                }
            }
        }

        terrainData.heightmapResolution = resolution;
        terrainData.SetHeights(0, 0, heights);
    }
}


[Serializable]
[MessagePackObject]
public class CWMap
{
    public static CWMap Instance;
    [Key(0)] public string name = "";
    [Key(1)] public string root = "";
    [Key(2)] public Dictionary<string, CWGameObject> GameObjectsOnStage = new Dictionary<string, CWGameObject>();
    [Key(3)] public Dictionary<string, byte[]> MeshesOnStage = new Dictionary<string, byte[]>();
    [Key(4)] public Dictionary<string, byte[]> AudiosOnStage = new Dictionary<string, byte[]>();
    [Key(5)] public Dictionary<string, CWMaterial> MaterialsOnStage = new Dictionary<string, CWMaterial>();
    [Key(6)] public Dictionary<string, byte[]> ShadersOnStage = new Dictionary<string, byte[]>();
    [Key(7)] public Dictionary<string, byte[]> TexturesOnStage = new Dictionary<string, byte[]>();
    [Key(8)] public Dictionary<string, byte[]> TerrainsOnStage = new Dictionary<string, byte[]>();
    [Key(9)] public CWLightmapData[] lightmapDatas;
    [Key(10)] public LightmapsMode lightmapsModes;
    [Key(11)] public byte[] lightProbesData;
    [Key(12)] public CWRenderSettings renderSettings;
    [Key(13)] public MapInfo Info;
    [Key(14)] public Bounds PlayableArea = new Bounds(Vector3.zero, new Vector3(10, 10, 10));
    [Key(15)] public Vector3[] SpawnPoints;
    [Key(16)] public Vector3[] TeamBasePoints;
    [Key(17)] public Vector3[] TeamFlagsPoints;
    [Key(18)] public Vector3[] WeaponSpawnPoints;
    [Key(19)] public float MiniMapScale;
    [Key(20)] public Vector3[] BotsReferencePoints;

    [Key(21)] public float ConquestRadius = 2;
    [Key(22)] public float RespawnRadius = 10;
    [Key(23)] public List<string> ConquestOnlyObjects = new List<string>();
    [Key(24)] public List<Vector3> ConquestPoints = new List<Vector3>();

    [Key(25)] public List<string> PatrolOnlyObjects = new List<string>();
    [Key(26)] public List<Vector3[]> PatrolPoints = new List<Vector3[]>();


    [Key(27)] public List<string> SabotageOnlyObjects = new List<string>();
    [Key(28)] public List<Vector3> BombPoints = new List<Vector3>();
    [Key(29)] public float SabotageRadius = 2.5f;
    [Key(30)] public byte[] NavMeshData;
    [Key(31)] public byte[] PostProcessingProfile;
    [Key(32)] public float WaterHeight;
    [Key(33)] public CWTransform CamTrans;
    [Key(34)] public CWSurfaceManager SurfaceManager;
    [Key(35)] public Dictionary<string, byte[]> PrefabsOnStage = new Dictionary<string, byte[]>();
    [Key(36)] public byte[] VNavMeshData;



    [IgnoreMember][System.NonSerialized] public Dictionary<string, GameObject> LoadedPrefabs = new Dictionary<string, GameObject>();
    [IgnoreMember][System.NonSerialized] public Dictionary<string, Mesh> LoadedMeshes = new Dictionary<string, Mesh>();
    [IgnoreMember][System.NonSerialized] public Dictionary<string, Material> LoadedMaterials = new Dictionary<string, Material>();
    [IgnoreMember][System.NonSerialized] public Dictionary<string, Shader> LoadedShaders = new Dictionary<string, Shader>();
    [IgnoreMember][System.NonSerialized] public Dictionary<string, Texture> LoadedTextures = new Dictionary<string, Texture>();
    [IgnoreMember][System.NonSerialized] public Dictionary<string, TerrainData> LoadedTerrains = new Dictionary<string, TerrainData>();
    [IgnoreMember][System.NonSerialized] public Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
    [IgnoreMember][System.NonSerialized] public Dictionary<string, AudioClip> LoadedAudios = new Dictionary<string, AudioClip>();


    public void SaveMapConfig(ModMapManager MMM)
    {
        PlayableArea = MMM.PlayableArea;
        SpawnPoints = MMM.SpawnPoints;
        WeaponSpawnPoints = MMM.WeaponSpawnPoints;
        MiniMapScale = MMM.MiniMapScale;
        BotsReferencePoints = MMM.BotsReferencePoints;
        ConquestRadius = MMM.ConquestRadius;
        RespawnRadius = MMM.RespawnRadius;

        foreach (Transform t in MMM.transform)
        {
            if (t.name.ToLower().Contains("water")) WaterHeight = t.position.y;
        }
        CamTrans = new CWTransform(Camera.main.transform);

        if (MonoBehaviour.FindObjectOfType<SurfaceManager>()) SurfaceManager = new CWSurfaceManager(MonoBehaviour.FindObjectOfType<SurfaceManager>());

        TeamBasePoints = new Vector3[2]; TeamBasePoints[0] = MMM.Team1.BasePosition; TeamBasePoints[1] = MMM.Team2.BasePosition;
        TeamFlagsPoints = new Vector3[2]; TeamFlagsPoints[0] = MMM.Team1.FlagPosition; TeamFlagsPoints[1] = MMM.Team2.FlagPosition;

        foreach (GameObject GO in MMM.ConquestOnlyObjects) { if (!GO || !GO.activeInHierarchy) continue; ConquestOnlyObjects.Add(GetTransformChecksum(GO.transform)); }
        foreach (ModMapManager.ConquestPoint CP in MMM.ConquestPoints) { ConquestPoints.Add(CP.Position); }

        foreach (GameObject GO in MMM.PatrolOnlyObjects) { if (!GO || !GO.activeInHierarchy) continue; PatrolOnlyObjects.Add(GetTransformChecksum(GO.transform)); }
        foreach (ModMapManager.PatrolPoint PP in MMM.PatrolPoints) { PatrolPoints.Add(PP.Points); }

        foreach (GameObject GO in MMM.SabotageOnlyObjects) { if (!GO || !GO.activeInHierarchy) continue; SabotageOnlyObjects.Add(GetTransformChecksum(GO.transform)); }
        foreach (ModMapManager.BombPoint BP in MMM.BombPoints) { BombPoints.Add(BP.Position); }
        SabotageRadius = MMM.SabotageRadius;
    }

    public void LoadMapConfig(ModMapManager MMM)
    {
        MMM.PlayableArea = PlayableArea;
        MMM.SpawnPoints = SpawnPoints;
        MMM.WeaponSpawnPoints = WeaponSpawnPoints;
        MMM.MiniMapScale = MiniMapScale;
        MMM.BotsReferencePoints = BotsReferencePoints;
        MMM.ConquestRadius = ConquestRadius;
        MMM.RespawnRadius = RespawnRadius;

        MMM.CamTrans = CamTrans;

        MMM.WaterHeight = WaterHeight;

        MMM.Team1.BasePosition = TeamBasePoints[0]; MMM.Team2.BasePosition = TeamBasePoints[1];
        MMM.Team1.FlagPosition = TeamFlagsPoints[0]; MMM.Team2.FlagPosition = TeamFlagsPoints[1];

        MMM.ConquestPoints.Clear();
        MMM.ConquestOnlyObjects.Clear();
        foreach (string str in ConquestOnlyObjects) { if (GameObjectsOnStage.TryGetValue(str, out CWGameObject CWGO)) MMM.ConquestOnlyObjects.Add(CWGO.gameobject); }
        foreach (Vector3 V3 in ConquestPoints) { MMM.ConquestPoints.Add(new ModMapManager.ConquestPoint() { Position = V3 }); }

        MMM.PatrolPoints.Clear();
        MMM.PatrolOnlyObjects.Clear();
        foreach (string str in PatrolOnlyObjects) { if (GameObjectsOnStage.TryGetValue(str, out CWGameObject CWGO)) MMM.PatrolOnlyObjects.Add(CWGO.gameobject); }
        foreach (Vector3[] V3s in PatrolPoints) { MMM.PatrolPoints.Add(new ModMapManager.PatrolPoint() { Points = V3s }); }

        MMM.BombPoints.Clear();
        MMM.SabotageOnlyObjects.Clear();
        foreach (string str in SabotageOnlyObjects) { if (GameObjectsOnStage.TryGetValue(str, out CWGameObject CWGO)) MMM.SabotageOnlyObjects.Add(CWGO.gameobject); }
        foreach (Vector3 V3 in BombPoints) { MMM.BombPoints.Add(new ModMapManager.BombPoint() { Position = V3 }); }
        MMM.SabotageRadius = SabotageRadius;


    }


    public string RegisterPrefab(GameObject GO)
    {
#if UNITY_EDITOR
        if (GO == null) return null;
        // Get the path to the prefab asset
        string prefabPath = AssetDatabase.GetAssetPath(GO);

        // Get the GUID of the prefab
        string GOID = AssetDatabase.AssetPathToGUID(prefabPath);

        BuildPipeline.BuildAssetBundle(LightmapSettings.lightProbes, null, "MapBuildChace/AssetBundles/TmpPrefab.bundle", BuildAssetBundleOptions.None, ModMapManager.Instance.buildTarget);
        byte[] BundleData = File.ReadAllBytes("MapBuildChace/AssetBundles/TmpPrefab.bundle");
        PrefabsOnStage.Add(GOID, BundleData);
        return GOID;
#else
        return null;
#endif
    }

    [IgnoreMember] public GameObject PrefabsRoot;
    public GameObject GetPrefab(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        if (PrefabsRoot == null) { PrefabsRoot = new GameObject("ModMapPrefabsRoot"); PrefabsRoot.SetActive(false); }
        GameObject GO;
        if (!LoadedPrefabs.TryGetValue(checksum, out GO))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "Prefabs/" + checksum + ".bundle");

            if (assetBundle != null)
            {
                if (assetBundle.mainAsset) GO = assetBundle.mainAsset as GameObject;
                // Unload the AssetBundle
                assetBundle.Unload(false);
            }
            else
            {
                Debug.LogError("Failed to load AssetBundle.");
                return null;
            }
            GO.transform.parent = PrefabsRoot.transform;
            LoadedPrefabs.Add(checksum, GO);
        }
        return GO;
    }

    public Mesh GetMesh(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        Mesh mesh;
        if (!LoadedMeshes.TryGetValue(checksum, out mesh))
        {
            mesh = DeserializeMesh(File.ReadAllBytes(root + "Meshes/" + checksum + ".umesh"));
            LoadedMeshes.Add(checksum, mesh);
        }
        return mesh;
    }

    public AudioClip GetAudio(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        AudioClip clip;
        if (!LoadedAudios.TryGetValue(checksum, out clip))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "Audios/" + checksum + ".bundle");

            if (assetBundle != null)
            {
                if (assetBundle.mainAsset) clip = assetBundle.mainAsset as AudioClip;
                // Unload the AssetBundle
                assetBundle.Unload(false);
            }
            else
            {
                Debug.LogError("Failed to load AssetBundle.");
                return null;
            }
            LoadedAudios.Add(checksum, clip);
        }
        return clip;
    }

    public PostProcessProfile GetPostEffect()
    {
        PostProcessProfile PPP = null;

        AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "Shaders/PostFX.bundle");

        if (assetBundle != null)
        {
            if (assetBundle.mainAsset) PPP = assetBundle.mainAsset as PostProcessProfile;
            // Unload the AssetBundle
            assetBundle.Unload(false);
        }
        else
        {
            Debug.LogError("Failed to load AssetBundle.");
            return null;
        }
        return PPP;

    }

    public void ApplyLightProbes()
    {
        AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "LightSettings/LightProbes.bundle");

        if (assetBundle != null)
        {
            // Load the texture from the AssetBundle
            if (assetBundle.mainAsset) LightmapSettings.lightProbes = assetBundle.mainAsset as LightProbes;
            // Unload the AssetBundle
            assetBundle.Unload(false);
        }
        else
        {
            Debug.LogError("Failed to load LightProbes Bundle.");
        }
    }

    public NavMeshData GetNavMesh()
    {
        AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "AI/NavMesh.bundle");
        NavMeshData NMD = null;
        if (assetBundle != null)
        {
            // Load the texture from the AssetBundle
            if (assetBundle.mainAsset) NMD = assetBundle.mainAsset as NavMeshData;
            // Unload the AssetBundle
            assetBundle.Unload(false);
        }
        else
        {
            Debug.LogError("Failed to load NavMesh Bundle.");
        }
        return NMD;
    }

    public NavMeshData GetVNavMesh()
    {
        AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "AI/VNavMesh.bundle");
        NavMeshData NMD = null;
        if (assetBundle != null)
        {
            // Load the texture from the AssetBundle
            if (assetBundle.mainAsset) NMD = assetBundle.mainAsset as NavMeshData;
            // Unload the AssetBundle
            assetBundle.Unload(false);
        }
        else
        {
            Debug.LogError("Failed to load VNavMesh Bundle.");
        }
        return NMD;
    }

    public Texture GetTexture(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        Texture tex;
        if (!LoadedTextures.TryGetValue(checksum, out tex))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "Textures/" + checksum + ".bundle");

            if (assetBundle != null)
            {
                // Load the texture from the AssetBundle
                if (assetBundle.mainAsset) tex = assetBundle.mainAsset as Texture;
                // Unload the AssetBundle
                assetBundle.Unload(false);
            }
            else
            {
                Debug.LogError("Failed to load AssetBundle.");
                return null;
            }
            LoadedTextures.Add(checksum, tex);
        }
        return tex;
    }

    public TerrainData GetTerraindata(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        TerrainData terr;
        if (!LoadedTerrains.TryGetValue(checksum, out terr))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "Terrains/" + checksum + ".bundle");

            if (assetBundle != null)
            {
                // Load the texture from the AssetBundle
                if (assetBundle.mainAsset) terr = assetBundle.mainAsset as TerrainData;
                // Unload the AssetBundle
                assetBundle.Unload(false);
            }
            else
            {
                Debug.LogError("Failed to load AssetBundle.");
                return null;
            }
            LoadedTerrains.Add(checksum, terr);
        }
        return terr;
    }

    public Sprite GetSprite(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        Sprite spr;
        if (!LoadedSprites.TryGetValue(checksum, out spr))
        {
            Texture2D tex = (Texture2D)GetTexture(checksum);
            spr = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            LoadedSprites.Add(checksum, spr);
        }
        return spr;
    }

    public Material GetMaterial(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        Material mat;
        if (!LoadedMaterials.TryGetValue(checksum, out mat))
        {
            CWMaterial cWMat = MaterialsOnStage[checksum];
            Shader shader = GetShader(cWMat.shaderName);
            Debug.Log("Decoded Shader Name : " + SimpleDecode(cWMat.shaderName));
            mat = new Material(shader);
            mat.name = cWMat.Name;
            foreach (CWMaterial.MatColor MC in cWMat.Colors) mat.SetColor(MC.propertyName, MC.color);
            foreach (CWMaterial.MatFloat MF in cWMat.floats) mat.SetFloat(MF.propertyName, MF.value);
            foreach (CWMaterial.MatInt MI in cWMat.ints) mat.SetInteger(MI.propertyName, MI.value);
            foreach (CWMaterial.MatVector MV in cWMat.vectors) mat.SetVector(MV.propertyName, MV.value);
            foreach (CWMaterial.MatTexture MT in cWMat.textures) mat.SetTexture(MT.propertyName, GetTexture(MT.textureChecksum));

            LoadedMaterials.Add(checksum, mat);
        }
        return mat;
    }
    public Shader GetShader(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return null;
        Shader shader = Shader.Find(SimpleDecode(checksum));
        if (shader != null) return shader;
        if (!LoadedShaders.TryGetValue(checksum, out shader))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(root + "Shaders/" + checksum + ".bundle");

            if (assetBundle != null)
            {
                // Load the texture from the AssetBundle
                UnityEngine.Object[] objs = assetBundle.LoadAllAssets();
                if (objs.Length > 0) shader = objs[0] as Shader;
                // Unload the AssetBundle
                assetBundle.Unload(false);
            }
            else
            {
                Debug.LogError("Failed to load AssetBundle.");
                return Shader.Find("Legacy Shaders/Diffuse");
            }
            LoadedShaders.Add(checksum, shader);
        }
        return shader;
    }



}



[MessagePackObject]
[Serializable]
public class CWMaterial
{
    [Key(0)] public string Name;
    [Key(1)] public string shaderName;
    [Key(2)] public List<MatColor> Colors = new List<MatColor>();
    [Key(3)] public List<MatTexture> textures = new List<MatTexture>();
    [Key(4)] public List<MatFloat> floats = new List<MatFloat>();
    [Key(5)] public List<MatVector> vectors = new List<MatVector>();
    [Key(6)] public List<MatInt> ints = new List<MatInt>();
    // Add other property types as needed

    [MessagePackObject]
    [Serializable]
    public class MatColor
    {
        [Key(0)] public string propertyName;
        [Key(1)] public Color color;

    }

    [MessagePackObject]
    [Serializable]
    public struct MatTexture
    {
        [Key(0)] public string propertyName;
        [Key(1)] public string textureChecksum;
        [Key(2)] public Vector2 textureScale;
        [Key(3)] public Vector2 textureOffset;
    }

    [MessagePackObject]
    [Serializable]
    public struct MatFloat
    {
        [Key(0)] public string propertyName;
        [Key(1)] public float value;
    }

    [MessagePackObject]
    [Serializable]
    public struct MatInt
    {
        [Key(0)] public string propertyName;
        [Key(1)] public int value;
    }

    [MessagePackObject]
    [Serializable]
    public struct MatVector
    {
        [Key(0)] public string propertyName;
        [Key(1)] public Vector4 value;
    }

    [MessagePackObject]
    [Serializable]
    public struct MatMatrix
    {
        [Key(0)] public string propertyName;
        [Key(1)] public Matrix4x4 value;
    }
    public CWMaterial(Material mat)
    {
#if UNITY_EDITOR
        Name = mat.name;
        if (mat.shader == null) shaderName = null;
        else
            shaderName = SimpleEncode(mat.shader.name);

        int propertyCount = ShaderUtil.GetPropertyCount(mat.shader);

        Colors.Clear(); textures.Clear(); floats.Clear(); ints.Clear();
        for (int i = 0; i < propertyCount; i++)
        {
            string pName = ShaderUtil.GetPropertyName(mat.shader, i);
            ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(mat.shader, i);

            if (propertyType == ShaderUtil.ShaderPropertyType.Float)
            {
                floats.Add(new MatFloat() { propertyName = pName, value = mat.GetFloat(pName) });
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.Int)
            {
                ints.Add(new MatInt() { propertyName = pName, value = mat.GetInteger(pName) });
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.Color)
            {
                Colors.Add(new MatColor() { propertyName = pName, color = mat.GetColor(pName) });
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.Vector)
            {
                vectors.Add(new MatVector() { propertyName = pName, value = mat.GetVector(pName) });
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.Range)
            {
                floats.Add(new MatFloat() { propertyName = pName, value = mat.GetFloat(pName) });
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                Texture tx = mat.GetTexture(pName);
                if (tx != null)
                    textures.Add(new MatTexture() { propertyName = pName, textureChecksum = GetTextureChecksum(tx) });
            }
        }
#endif
    }
    public CWMaterial() { }


}

[MessagePackObject]
[Serializable]
public class CWMeshRenderer : CWRenderer
{
    [Key(19)] public string meshChecksum;
    [Key(20)] public Vector4[] UVRegion;
    //[Key(21)]public bool stitchLightmapSeams;
    //[Key(22)]public ReceiveGI receiveGI;
    public CWMeshRenderer() { }
    public CWMeshRenderer(MeshRenderer MR, MeshFilter MF, bool Atlas)
    {
#if UNITY_EDITOR
        base.Serialize(MR);
        if (MF) meshChecksum = GetMeshChecksum(MF.sharedMesh);
        if (Atlas && MF) UVRegion = new Vector4[MaterialsCRC.Length];
        for (int i = 0; i < MaterialsCRC.Length; i++)
        {
            if (MR.sharedMaterials[i] == null) continue;
            if (Atlas && MF)
            {
                MaterialsCRC[i] = GetMaterialChecksum(TextureAtlaser.Instance.GetMat(StandardShaderConverter.GetConvertedMat(MR.sharedMaterials[i])));
                UVRegion[i] = TextureAtlaser.Instance.GetUVOffset(StandardShaderConverter.GetConvertedMat(MR.sharedMaterials[i]), MF.sharedMesh, i);
            }
        }
        //scaleInLightmap=MR.scaleInLightmap;
        //stitchLightmapSeams=MR.stitchLightmapSeams;
        //receiveGI=MR.receiveGI;

#endif
    }

}

[MessagePackObject]
[Serializable]
public class CWTextMesh
{
    [Key(0)] public string text;
    //public Font font;
    [Key(1)] public int fontSize;
    [Key(2)] public FontStyle fontStyle;
    [Key(3)] public float offsetZ;
    [Key(4)] public TextAlignment alignment;
    [Key(5)] public TextAnchor anchor;
    [Key(6)] public float characterSize;
    [Key(7)] public float lineSpacing;
    [Key(8)] public float tabSize;
    [Key(9)] public bool richText;
    [Key(10)] public Color color;

    public CWTextMesh() { }
    public CWTextMesh(TextMesh TM)
    {
        text = TM.text;
        // font=TM.font;
        fontSize = TM.fontSize;
        fontStyle = TM.fontStyle;
        offsetZ = TM.offsetZ;
        alignment = TM.alignment;
        anchor = TM.anchor;
        characterSize = TM.characterSize;
        lineSpacing = TM.lineSpacing;
        tabSize = TM.tabSize;
        richText = TM.richText;
        color = TM.color;
    }

    public void Load(TextMesh TM)
    {
        TM.text = text;
        // TM.font=font;
        TM.fontSize = fontSize;
        TM.fontStyle = fontStyle;
        TM.offsetZ = offsetZ;
        TM.alignment = alignment;
        TM.anchor = anchor;
        TM.characterSize = characterSize;
        TM.lineSpacing = lineSpacing;
        TM.tabSize = tabSize;
        TM.richText = richText;
        TM.color = color;
    }
}

[MessagePackObject]
[Serializable]
public class CWSpriteRenderer : CWRenderer
{

    [Key(19)] public string spriteChecksum;
    [Key(20)] public SpriteDrawMode drawMode;
    [Key(21)] public Vector2 size;
    [Key(22)] public float adaptiveModeThreshold;
    [Key(23)] public SpriteTileMode tileMode;
    [Key(24)] public Color color;
    [Key(25)] public SpriteMaskInteraction maskInteraction;
    [Key(26)] public bool flipX;
    [Key(27)] public bool flipY;
    [Key(28)] public SpriteSortPoint spriteSortPoint;
    public CWSpriteRenderer() { }
    public CWSpriteRenderer(SpriteRenderer SR)
    {
        base.Serialize(SR);
        spriteChecksum = SR.sprite == null || SR.sprite.texture == null ? null : GetTextureChecksum(SR.sprite.texture);
        drawMode = SR.drawMode;
        size = SR.size;
        adaptiveModeThreshold = SR.adaptiveModeThreshold;
        tileMode = SR.tileMode;
        color = SR.color;
        maskInteraction = SR.maskInteraction;
        flipX = SR.flipX;
        flipY = SR.flipY;
        spriteSortPoint = SR.spriteSortPoint;

    }


}

[MessagePackObject]
[Serializable]
public class CWSurfaceManager
{
    [MessagePackObject]
    [Serializable]
    public class CWSurfaceInfo
    {

        [Key(0)] public string Name;
        [Key(1)] public string Decal;
        [Key(2)] public string[] ShellSounds;
        [Key(3)] public string[] FootstepSounds;
        [Key(4)] public string[] CrawlSounds;
        [Key(5)] public string[] JumpSounds;
        [Key(6)] public string[] LandSounds;
        [Key(7)] public string[] SlideSounds;

        [Key(8)] public float Thickness = 1;
        [Key(9)] public float VisDist = 100;

        public CWSurfaceInfo() { }
        public CWSurfaceInfo(SurfaceInfo SI)
        {
            Name = SI.Name;
            Decal = CWMap.Instance.RegisterPrefab(SI.Decal);
            ShellSounds = new string[SI.ShellSounds.Length]; for (int i = 0; i < ShellSounds.Length; i++) ShellSounds[i] = GetAudioChecksum(SI.ShellSounds[i]);
            FootstepSounds = new string[SI.FootstepSounds.Length]; for (int i = 0; i < FootstepSounds.Length; i++) FootstepSounds[i] = GetAudioChecksum(SI.FootstepSounds[i]);
            CrawlSounds = new string[SI.CrawlSounds.Length]; for (int i = 0; i < CrawlSounds.Length; i++) CrawlSounds[i] = GetAudioChecksum(SI.CrawlSounds[i]);
            JumpSounds = new string[SI.JumpSounds.Length]; for (int i = 0; i < JumpSounds.Length; i++) JumpSounds[i] = GetAudioChecksum(SI.JumpSounds[i]);
            LandSounds = new string[SI.LandSounds.Length]; for (int i = 0; i < LandSounds.Length; i++) LandSounds[i] = GetAudioChecksum(SI.LandSounds[i]);
            SlideSounds = new string[SI.SlideSounds.Length]; for (int i = 0; i < SlideSounds.Length; i++) SlideSounds[i] = GetAudioChecksum(SI.SlideSounds[i]);

            Thickness = SI.Thickness;
            VisDist = SI.VisDist;
        }

        public SurfaceInfo ToSurfaceInfo()
        {
            SurfaceInfo SI = new SurfaceInfo();
            SI.Name = Name;
            SI.Decal = CWMap.Instance.GetPrefab(Decal);
            SI.ShellSounds = new AudioClip[ShellSounds.Length]; for (int i = 0; i < ShellSounds.Length; i++) SI.ShellSounds[i] = CWMap.Instance.GetAudio(ShellSounds[i]);
            SI.FootstepSounds = new AudioClip[FootstepSounds.Length]; for (int i = 0; i < FootstepSounds.Length; i++) SI.FootstepSounds[i] = CWMap.Instance.GetAudio(FootstepSounds[i]);
            SI.CrawlSounds = new AudioClip[CrawlSounds.Length]; for (int i = 0; i < CrawlSounds.Length; i++) SI.CrawlSounds[i] = CWMap.Instance.GetAudio(CrawlSounds[i]);
            SI.JumpSounds = new AudioClip[JumpSounds.Length]; for (int i = 0; i < JumpSounds.Length; i++) SI.JumpSounds[i] = CWMap.Instance.GetAudio(JumpSounds[i]);
            SI.LandSounds = new AudioClip[LandSounds.Length]; for (int i = 0; i < LandSounds.Length; i++) SI.LandSounds[i] = CWMap.Instance.GetAudio(LandSounds[i]);
            SI.SlideSounds = new AudioClip[SlideSounds.Length]; for (int i = 0; i < SlideSounds.Length; i++) SI.SlideSounds[i] = CWMap.Instance.GetAudio(SlideSounds[i]);

            SI.Thickness = Thickness;
            SI.VisDist = VisDist;

            return SI;
        }
    }

    [Key(0)] public CWSurfaceInfo[] surfaceInfos;
    public CWSurfaceManager() { }
    public CWSurfaceManager(SurfaceManager SM)
    {
        surfaceInfos = new CWSurfaceInfo[SM.surfaceInfos.Length];
        for (int i = 0; i < surfaceInfos.Length; i++) { surfaceInfos[i] = new CWSurfaceInfo(SM.surfaceInfos[i]); }
    }

    public void Deserialize(SurfaceManager SM)
    {
        SM.HasModSurface = true;
        SM.ModSurfaceInfos = new SurfaceInfo[surfaceInfos.Length];
        for (int i = 0; i < surfaceInfos.Length; i++) { SM.ModSurfaceInfos[i] = surfaceInfos[i].ToSurfaceInfo(); }
    }

}

[MessagePackObject]
[Serializable]
public class CWRenderer
{
    [Key(0)] public string[] MaterialsCRC;
    [Key(1)] public ShadowCastingMode CastShadow;
    [Key(2)] public bool ReceiveShadows;
    [Key(3)] public LightProbeUsage lightProbeUsage;
    [Key(4)] public ReflectionProbeUsage reflectionProbeUsage;
    [Key(5)] public MotionVectorGenerationMode motionVectorGenerationMode;
    [Key(6)] public bool allowOcclusionWhenDynamic;
    [Key(7)] public Bounds bounds;
    [Key(8)] public HideFlags hideFlags;
    [Key(9)] public int lightmapIndex;
    [Key(10)] public Vector4 lightmapScaleOffset;
    [Key(11)] public int realtimeLightmapIndex;
    [Key(12)] public Vector4 realtimeLightmapScaleOffset;
    [Key(13)] public int rendererPriority;
    [Key(14)] public uint renderingLayerMask;
    [Key(15)] public int sortingLayerID;
    [Key(16)] public string sortingLayerName;
    [Key(17)] public int sortingOrder;
    [Key(18)] public bool staticShadowCaster;
    public CWRenderer() { }
    public void Serialize(Renderer R)
    {
        MaterialsCRC = new string[R.sharedMaterials.Length];
        for (int i = 0; i < MaterialsCRC.Length; i++)
        {
            if (R.sharedMaterials[i] == null) continue;
            MaterialsCRC[i] = GetMaterialChecksum(R.sharedMaterials[i]);
        }
        CastShadow = R.shadowCastingMode;
        ReceiveShadows = R.receiveShadows;
        lightProbeUsage = R.lightProbeUsage;
        reflectionProbeUsage = R.reflectionProbeUsage;
        motionVectorGenerationMode = R.motionVectorGenerationMode;
        bounds = R.bounds;
        allowOcclusionWhenDynamic = R.allowOcclusionWhenDynamic;
        hideFlags = R.hideFlags;
        lightmapIndex = R.lightmapIndex;
        lightmapScaleOffset = R.lightmapScaleOffset;
        realtimeLightmapIndex = R.realtimeLightmapIndex;
        realtimeLightmapScaleOffset = R.realtimeLightmapScaleOffset;
        rendererPriority = R.rendererPriority;
        renderingLayerMask = R.renderingLayerMask;
        sortingLayerID = R.sortingLayerID;
        sortingLayerName = R.sortingLayerName;
        sortingOrder = R.sortingOrder;
        staticShadowCaster = R.staticShadowCaster;

    }

    public void Deserialize(Renderer R)
    {
        R.shadowCastingMode = CastShadow;
        R.receiveShadows = ReceiveShadows;
        R.lightProbeUsage = lightProbeUsage;
        R.reflectionProbeUsage = reflectionProbeUsage;
        R.motionVectorGenerationMode = motionVectorGenerationMode;
        R.bounds = bounds;
        R.allowOcclusionWhenDynamic = allowOcclusionWhenDynamic;
        R.hideFlags = hideFlags;
        R.lightmapIndex = lightmapIndex;
        R.lightmapScaleOffset = lightmapScaleOffset;
        R.realtimeLightmapIndex = realtimeLightmapIndex;
        R.realtimeLightmapScaleOffset = realtimeLightmapScaleOffset;
        R.rendererPriority = rendererPriority;
        R.renderingLayerMask = renderingLayerMask;
        R.sortingLayerID = sortingLayerID;
        R.sortingLayerName = sortingLayerName;
        R.sortingOrder = sortingOrder;
        R.staticShadowCaster = staticShadowCaster;

    }


}

[MessagePackObject]
[Serializable]
public class CWTransform
{

    [Key(0)] public Vector3 position;
    [Key(1)] public Vector3 rotation;
    [Key(2)] public Vector3 scale;

    public CWTransform() { }
    public CWTransform(Transform trans)
    {
        position = trans.position;
        rotation = trans.eulerAngles;
        scale = trans.lossyScale;
    }

    public void ToTransform(Transform trans)
    {
        trans.position = position;
        trans.eulerAngles = rotation;
        //trans.localScale = scale;
    }
}


[MessagePackObject]
[Serializable]
public class CWNavMeshLink
{

    [Key(0)] public int agentTypeID;
    [Key(1)] public Vector3 startPoint;
    [Key(2)] public Vector3 endPoint;
    [Key(3)] public float width;
    [Key(4)] public int costModifier;
    [Key(5)] public bool bidirectional;
    [Key(6)] public bool autoUpdate;
    [Key(7)] public int area;

    public CWNavMeshLink() { }
    public CWNavMeshLink(NavMeshLink Link)
    {
        agentTypeID = Link.agentTypeID;
        startPoint = Link.startPoint;
        endPoint = Link.endPoint;
        width = Link.width;
        costModifier = Link.costModifier;
        bidirectional = Link.bidirectional;
        autoUpdate = Link.autoUpdate;
        area = Link.area;
    }

    public void Load(NavMeshLink Link)
    {
        Link.agentTypeID = agentTypeID;
        Link.startPoint = startPoint;
        Link.endPoint = endPoint;
        Link.width = width;
        Link.costModifier = costModifier;
        Link.bidirectional = bidirectional;
        Link.autoUpdate = autoUpdate;
        Link.area = area;
    }


}


[MessagePackObject]
[Serializable]
public class CWPlaceHolder
{

    [Key(0)] public byte ID = 0;
    [Key(1)] public PlaceHolderType Type = PlaceHolderType.Vehichle;

    public CWPlaceHolder() { }
    public CWPlaceHolder(PlaceHolder PH)
    {
        ID = PH.ID;
        Type = PH.Type;
    }

    public void Load(PlaceHolder PH)
    {
        PH.ID = ID;
        PH.Type = Type;
    }


}


[MessagePackObject]
[Serializable]
public class CWLodGroup
{

    [MessagePackObject]
    [Serializable]
    public class CWLod
    {
        [Key(0)] public string[] IDs;
        [Key(1)] public float screenRelativeTransitionHeight;
    }
    [Key(0)] public CWLod[] Lods;
    [Key(1)] public Vector3 localReferencePoint;
    [Key(2)] public float size;

    public CWLodGroup() { }
    public CWLodGroup(LODGroup lodG)
    {
        Lods = new CWLod[lodG.lodCount];
        LOD[] LODS = lodG.GetLODs();
        for (int i = 0; i < lodG.lodCount; i++)
        {
            Lods[i] = new CWLod();
            Lods[i].IDs = new string[LODS[i].renderers.Length];
            for (int j = 0; j < Lods[i].IDs.Length; j++) { if (LODS[i].renderers[j] != null) Lods[i].IDs[j] = GetTransformChecksum(LODS[i].renderers[j].transform); }
            Lods[i].screenRelativeTransitionHeight = LODS[i].screenRelativeTransitionHeight;
        }
        localReferencePoint = lodG.localReferencePoint;
        size = lodG.size;
    }
}


[MessagePackObject]
[Serializable]
public class CWBoxCollider
{
    [Key(0)] public Vector3 center;
    [Key(1)] public Vector3 size;
    [Key(2)] public float contactOffset;
    [Key(3)] public bool hasModifiableContacts;
    [Key(4)] public HideFlags hideFlags;
    [Key(5)] public bool isTrigger;

    public CWBoxCollider() { }
    public CWBoxCollider(BoxCollider C)
    {
        center = C.center;
        size = C.size;
        contactOffset = C.contactOffset;
        hasModifiableContacts = C.hasModifiableContacts;
        hideFlags = C.hideFlags;
        isTrigger = C.isTrigger;
    }
}


[MessagePackObject]
[Serializable]
public class CWSphereCollider
{
    [Key(0)] public Vector3 center;
    [Key(1)] public float radius;
    [Key(2)] public float contactOffset;
    [Key(3)] public bool hasModifiableContacts;
    [Key(4)] public HideFlags hideFlags;
    [Key(5)] public bool isTrigger;

    public CWSphereCollider() { }
    public CWSphereCollider(SphereCollider C)
    {
        center = C.center;
        radius = C.radius;
        contactOffset = C.contactOffset;
        hasModifiableContacts = C.hasModifiableContacts;
        hideFlags = C.hideFlags;
        isTrigger = C.isTrigger;
    }
}

[MessagePackObject]
[Serializable]
public class CWCapsuleCollider
{
    [Key(0)] public Vector3 center;
    [Key(1)] public float radius;
    [Key(2)] public float height;
    [Key(3)] public float contactOffset;
    [Key(4)] public bool hasModifiableContacts;
    [Key(5)] public HideFlags hideFlags;
    [Key(6)] public bool isTrigger;

    public CWCapsuleCollider() { }
    public CWCapsuleCollider(CapsuleCollider C)
    {
        center = C.center;
        radius = C.radius;
        height = C.height;
        contactOffset = C.contactOffset;
        hasModifiableContacts = C.hasModifiableContacts;
        hideFlags = C.hideFlags;
        isTrigger = C.isTrigger;
    }
}


[MessagePackObject]
[Serializable]
public class CWMeshCollider
{
    [Key(0)] public string meshChecksum;
    [Key(1)] public float contactOffset;
    [Key(2)] public bool hasModifiableContacts;
    [Key(3)] public HideFlags hideFlags;
    [Key(4)] public bool isTrigger;
    [Key(5)] public bool convex;
    [Key(6)] public MeshColliderCookingOptions cookingOptions;

    public CWMeshCollider() { }
    public CWMeshCollider(MeshCollider C)
    {
        meshChecksum = GetMeshChecksum(C.sharedMesh);
        contactOffset = C.contactOffset;
        hasModifiableContacts = C.hasModifiableContacts;
        hideFlags = C.hideFlags;
        isTrigger = C.isTrigger;
        convex = C.convex;
        cookingOptions = C.cookingOptions;
    }
}

[MessagePackObject]
[Serializable]
public class CWAudioSource
{
    [Key(0)] public string clipChecksum;
    [Key(1)] public bool bypassEffects;
    [Key(2)] public bool bypassListenerEffects;
    [Key(3)] public bool bypassReverbZones;
    [Key(4)] public float dopplerLevel;
    //[Key(5)]public GamepadSpeakerOutputType  gamepadSpeakerOutputType;
    [Key(6)] public HideFlags hideFlags;
    [Key(7)] public bool loop;
    [Key(8)] public float maxDistance;
    [Key(9)] public float minDistance;
    [Key(10)] public bool mute;
    [Key(11)] public float panStereo;
    [Key(12)] public float pitch;
    [Key(13)] public bool playOnAwake;
    [Key(14)] public int priority;
    [Key(15)] public float reverbZoneMix;
    [Key(16)] public AudioRolloffMode rolloffMode;
    [Key(17)] public float spatialBlend;
    [Key(18)] public float spread;
    [Key(19)] public float volume;
    [Key(20)] public AnimationCurve CustomRolloff;
    [Key(21)] public AnimationCurve ReverbZoneMix;
    [Key(22)] public AnimationCurve SpatialBlend;
    [Key(23)] public AnimationCurve Spread;

    public CWAudioSource() { }
    public CWAudioSource(AudioSource AS)
    {
        clipChecksum = GetAudioChecksum(AS.clip);
        bypassEffects = AS.bypassEffects;
        bypassListenerEffects = AS.bypassListenerEffects;
        bypassReverbZones = AS.bypassReverbZones;
        dopplerLevel = AS.dopplerLevel;
        //gamepadSpeakerOutputType=AS.gamepadSpeakerOutputType;
        hideFlags = AS.hideFlags;
        loop = AS.loop;
        maxDistance = AS.maxDistance;
        minDistance = AS.minDistance;
        mute = AS.mute;
        panStereo = AS.panStereo;
        pitch = AS.pitch;
        playOnAwake = AS.playOnAwake;
        priority = AS.priority;
        reverbZoneMix = AS.reverbZoneMix;
        rolloffMode = AS.rolloffMode;
        spatialBlend = AS.spatialBlend;
        spread = AS.spread;
        volume = AS.volume;
        CustomRolloff = AS.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
        ReverbZoneMix = AS.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix);
        SpatialBlend = AS.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
        Spread = AS.GetCustomCurve(AudioSourceCurveType.Spread);
        //Spread.
    }
}




[MessagePackObject]
[Serializable]
public class CWLight
{

    [MessagePackObject]
    [Serializable]
    public class CWLightBakingOutput
    {
        [Key(0)]
        public int probeOcclusionLightIndex;

        [Key(1)]
        public int occlusionMaskChannel;

        [Key(2)]
        public LightmapBakeType lightmapBakeType;

        [Key(3)]
        public MixedLightingMode mixedLightingMode;

        [Key(4)]
        public bool isBaked;

        public void Save(LightBakingOutput value)
        {
            probeOcclusionLightIndex = value.probeOcclusionLightIndex;
            occlusionMaskChannel = value.occlusionMaskChannel;
            lightmapBakeType = value.lightmapBakeType;
            mixedLightingMode = value.mixedLightingMode;
            isBaked = value.isBaked;
        }

        public void load(Light li)
        {
            LightBakingOutput value;
            value.probeOcclusionLightIndex = probeOcclusionLightIndex;
            value.occlusionMaskChannel = occlusionMaskChannel;
            value.lightmapBakeType = lightmapBakeType;
            value.mixedLightingMode = mixedLightingMode;
            value.isBaked = isBaked;
            li.bakingOutput = value;
        }
    }
    [Key(0)] public bool useShadowMatrixOverride;
    //public Flare flare;
    [Key(1)] public CWLightBakingOutput bakingOutput = new CWLightBakingOutput();
    [Key(2)] public int cullingMask;
    [Key(3)] public int renderingLayerMask;
    [Key(4)] public LightShadowCasterMode lightShadowCasterMode;
    //[Key(5)]public float shadowRadius;
    //[Key(6)]public float shadowAngle;
    [Key(7)] public LightShadows shadows;
    [Key(8)] public float shadowStrength;
    [Key(9)] public LightShadowResolution shadowResolution;
    [Key(10)] public float[] layerShadowCullDistances;
    [Key(11)] public float cookieSize;
    // public Texture cookie;
    [Key(12)] public LightRenderMode renderMode;
    //[Key(13)]public Vector2 areaSize;
    //[Key(14)]public LightmapBakeType lightmapBakeType;
    [Key(15)] public float range;
    [Key(16)] public Matrix4x4 shadowMatrixOverride;
    [Key(17)] public float shadowNearPlane;
    [Key(18)] public LightType type;
    [Key(19)] public LightShape shape;
    [Key(20)] public float innerSpotAngle;
    [Key(21)] public Color color;
    [Key(22)] public float colorTemperature;
    [Key(23)] public bool useColorTemperature;
    [Key(24)] public float spotAngle;
    [Key(25)] public float bounceIntensity;
    [Key(26)] public bool useBoundingSphereOverride;
    [Key(27)] public Vector4 boundingSphereOverride;
    [Key(28)] public bool useViewFrustumForShadowCasterCull;
    [Key(29)] public int shadowCustomResolution;
    [Key(30)] public float shadowBias;
    [Key(31)] public float shadowNormalBias;
    [Key(32)] public float intensity;
    [Key(33)] public byte[] FlareData;

    public CWLight() { }
    public CWLight(Light light)
    {
        useShadowMatrixOverride = light.useShadowMatrixOverride;
        // flare=light.flare;
        bakingOutput.Save(light.bakingOutput);
        cullingMask = light.cullingMask;
        renderingLayerMask = light.renderingLayerMask;
        lightShadowCasterMode = light.lightShadowCasterMode;
        //shadowRadius=light.shadowRadius;
        //shadowAngle=light.shadowAngle;
        shadows = light.shadows;
        shadowStrength = light.shadowStrength;
        shadowResolution = light.shadowResolution;
        layerShadowCullDistances = light.layerShadowCullDistances;
        cookieSize = light.cookieSize;
        //cookie=light.cookie;
        renderMode = light.renderMode;
        //areaSize=light.areaSize;
        //lightmapBakeType=light.lightmapBakeType;
        range = light.range;
        shadowMatrixOverride = light.shadowMatrixOverride;
        shadowNearPlane = light.shadowNearPlane;
        type = light.type;
        shape = light.shape;
        innerSpotAngle = light.innerSpotAngle;
        color = light.color;
        colorTemperature = light.colorTemperature;
        useColorTemperature = light.useColorTemperature;
        spotAngle = light.spotAngle;
        bounceIntensity = light.bounceIntensity;
        useBoundingSphereOverride = light.useBoundingSphereOverride;
        boundingSphereOverride = light.boundingSphereOverride;
        useViewFrustumForShadowCasterCull = light.useViewFrustumForShadowCasterCull;
        shadowCustomResolution = light.shadowCustomResolution;
        shadowBias = light.shadowBias;
        shadowNormalBias = light.shadowNormalBias;
        intensity = light.intensity;
#if UNITY_EDITOR
        if (light.flare)
        {
            BuildPipeline.BuildAssetBundle(light.flare, null, "MapBuildChace/AssetBundles/TmpFlare.bundle", BuildAssetBundleOptions.CollectDependencies, ModMapManager.Instance.buildTarget);
            FlareData = File.ReadAllBytes("MapBuildChace/AssetBundles/TmpFlare.bundle");
        }
#endif
    }

    public void Load(Light light)
    {
        light.useShadowMatrixOverride = useShadowMatrixOverride;
        //light.flare=flare;
        light.cullingMask = cullingMask;
        light.renderingLayerMask = renderingLayerMask;
        light.lightShadowCasterMode = lightShadowCasterMode;
        //light.shadowRadius=shadowRadius;
        //light.shadowAngle=shadowAngle;
        light.shadows = shadows;
        light.shadowStrength = shadowStrength;
        light.shadowResolution = shadowResolution;
        light.layerShadowCullDistances = layerShadowCullDistances;
        light.cookieSize = cookieSize;
        //light.cookie=cookie;
        light.renderMode = renderMode;
        //light.areaSize=areaSize;
        //light.lightmapBakeType=lightmapBakeType;
        light.range = range;
        light.shadowMatrixOverride = shadowMatrixOverride;
        light.shadowNearPlane = shadowNearPlane;
        light.type = type;
        light.shape = shape;
        light.innerSpotAngle = innerSpotAngle;
        light.color = color;
        light.colorTemperature = colorTemperature;
        light.useColorTemperature = useColorTemperature;
        light.spotAngle = spotAngle;
        light.bounceIntensity = bounceIntensity;
        light.useBoundingSphereOverride = useBoundingSphereOverride;
        light.boundingSphereOverride = boundingSphereOverride;
        light.useViewFrustumForShadowCasterCull = useViewFrustumForShadowCasterCull;
        light.shadowCustomResolution = shadowCustomResolution;
        light.shadowBias = shadowBias;
        light.shadowNormalBias = shadowNormalBias;
        light.intensity = intensity;
        bakingOutput.load(light);
        if (FlareData != null)
        {
            string mapPath = CreateDirectory(Application.persistentDataPath + "/Maps/" + CWMap.Instance.name + "/");
            string FlarePath = CreateDirectory(mapPath + "Flares/");
            if (!File.Exists(FlarePath + "MainFlare.bundle")) File.WriteAllBytes(FlarePath + "MainFlare.bundle", FlareData);

            Flare flare = null;
            AssetBundle assetBundle = AssetBundle.LoadFromFile(FlarePath + "MainFlare.bundle");

            if (assetBundle != null)
            {
                if (assetBundle.mainAsset) flare = assetBundle.mainAsset as Flare;
                // Unload the AssetBundle
                assetBundle.Unload(false);
                light.flare = flare;
            }
        }
    }
}


[MessagePackObject]
[Serializable]
public class CWLightmapData
{
    [Key(0)] public string lightmapColorChecksum = null;
    [Key(1)] public string lightmapDirChecksum = null;
    [Key(2)] public string shadowMaskChecksum = null;
    public CWLightmapData() { }

    public CWLightmapData(LightmapData LMD)
    {
        lightmapColorChecksum = LMD.lightmapColor == null ? null : GetTextureChecksum(LMD.lightmapColor);
        lightmapDirChecksum = LMD.lightmapDir == null ? null : GetTextureChecksum(LMD.lightmapDir);
        shadowMaskChecksum = LMD.shadowMask == null ? null : GetTextureChecksum(LMD.shadowMask);

    }

}

[MessagePackObject]
[Serializable]
public class CWSphericalHarmonicsL2
{
    [Key(0)] public float[] coefficients = new float[27];

    public CWSphericalHarmonicsL2() { }
    public CWSphericalHarmonicsL2(SphericalHarmonicsL2 SHL2)
    {

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                coefficients[i * 9 + j] = SHL2[i, j];
            }
        }
    }

    public SphericalHarmonicsL2 ToProbe()
    {
        SphericalHarmonicsL2 SHL2 = new SphericalHarmonicsL2();

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                SHL2[i, j] = coefficients[i * 9 + j];
            }
        }
        return SHL2;
    }
}


[MessagePackObject]
[Serializable]
public class CWTerrain
{
    [Key(0)] public string TerrainDataChecksum;
    [Key(1)] public bool drawInstanced;
    [Key(2)] public int groupingID;
    [Key(3)] public bool allowAutoConnect;
    [Key(4)] public bool drawHeightmap;
    [Key(5)] public string materialTemplateChecksum;
    [Key(6)] public bool drawTreesAndFoliage;
    [Key(7)] public bool preserveTreePrototypeLayers;
    [Key(8)] public float treeLODBiasMultiplier;
    [Key(9)] public bool collectDetailPatches;
    [Key(10)] public TerrainRenderFlags editorRenderFlags;
    //[Key(11)]public bool bakeLightProbesForTrees;
    //[Key(12)]public bool deringLightProbesForTrees;
    [Key(13)] public ReflectionProbeUsage reflectionProbeUsage;
    [Key(14)] public Vector3 patchBoundsMultiplier;
    [Key(15)] public ShadowCastingMode shadowCastingMode;
    [Key(16)] public bool keepUnusedRenderingResources;
    [Key(17)] public uint renderingLayerMask;
    [Key(18)] public float treeDistance;
    [Key(19)] public float treeBillboardDistance;
    [Key(20)] public float treeCrossFadeLength;
    [Key(21)] public float detailObjectDistance;
    [Key(22)] public Vector4 realtimeLightmapScaleOffset;
    [Key(23)] public Vector4 lightmapScaleOffset;
    [Key(24)] public int realtimeLightmapIndex;
    [Key(25)] public int lightmapIndex;
    [Key(26)] public int treeMaximumFullLODCount;
    [Key(27)] public int heightmapMaximumLOD;
    [Key(28)] public float basemapDistance;
    [Key(29)] public float heightmapPixelError;
    [Key(30)] public float detailObjectDensity;

    public CWTerrain() { }
    public CWTerrain(Terrain T)
    {
#if UNITY_EDITOR
        TerrainDataChecksum = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(T.terrainData));
#endif
        drawInstanced = T.drawInstanced;
        groupingID = T.groupingID;
        allowAutoConnect = T.allowAutoConnect;
        drawHeightmap = T.drawHeightmap;
        if (T.materialTemplate != null && T.drawHeightmap) materialTemplateChecksum = GetMaterialChecksum(T.materialTemplate);
        drawTreesAndFoliage = T.drawTreesAndFoliage;
        preserveTreePrototypeLayers = T.preserveTreePrototypeLayers;
        treeLODBiasMultiplier = T.treeLODBiasMultiplier;
        collectDetailPatches = T.collectDetailPatches;
        editorRenderFlags = T.editorRenderFlags;
        //bakeLightProbesForTrees=T.bakeLightProbesForTrees;
        //deringLightProbesForTrees=T.deringLightProbesForTrees;
        reflectionProbeUsage = T.reflectionProbeUsage;
        patchBoundsMultiplier = T.patchBoundsMultiplier;
        shadowCastingMode = T.shadowCastingMode;
        keepUnusedRenderingResources = T.keepUnusedRenderingResources;
        renderingLayerMask = T.renderingLayerMask;
        treeDistance = T.treeDistance;
        treeBillboardDistance = T.treeBillboardDistance;
        treeCrossFadeLength = T.treeCrossFadeLength;
        detailObjectDistance = T.detailObjectDistance;
        realtimeLightmapScaleOffset = T.realtimeLightmapScaleOffset;
        lightmapScaleOffset = T.lightmapScaleOffset;
        realtimeLightmapIndex = T.realtimeLightmapIndex;
        lightmapIndex = T.lightmapIndex;
        treeMaximumFullLODCount = T.treeMaximumFullLODCount;
        heightmapMaximumLOD = T.heightmapMaximumLOD;
        basemapDistance = T.basemapDistance;
        heightmapPixelError = T.heightmapPixelError;
        detailObjectDensity = T.detailObjectDensity;
    }

    public void Load(Terrain T)
    {
        T.terrainData = CWMap.Instance.GetTerraindata(TerrainDataChecksum);
        T.drawInstanced = drawInstanced;
        T.groupingID = groupingID;
        T.allowAutoConnect = allowAutoConnect;
        T.drawHeightmap = drawHeightmap;
        T.materialTemplate = CWMap.Instance.GetMaterial(materialTemplateChecksum);
        T.drawTreesAndFoliage = drawTreesAndFoliage;
        T.preserveTreePrototypeLayers = preserveTreePrototypeLayers;
        T.treeLODBiasMultiplier = treeLODBiasMultiplier;
        T.collectDetailPatches = collectDetailPatches;
        T.editorRenderFlags = editorRenderFlags;
        //T.bakeLightProbesForTrees=bakeLightProbesForTrees;
        //T.deringLightProbesForTrees=deringLightProbesForTrees;
        T.reflectionProbeUsage = reflectionProbeUsage;
        T.patchBoundsMultiplier = patchBoundsMultiplier;
        T.shadowCastingMode = shadowCastingMode;
        T.keepUnusedRenderingResources = keepUnusedRenderingResources;
        T.renderingLayerMask = renderingLayerMask;
        T.treeDistance = treeDistance;
        T.treeBillboardDistance = treeBillboardDistance;
        T.treeCrossFadeLength = treeCrossFadeLength;
        T.detailObjectDistance = detailObjectDistance;
        T.realtimeLightmapScaleOffset = realtimeLightmapScaleOffset;
        T.lightmapScaleOffset = lightmapScaleOffset;
        T.realtimeLightmapIndex = realtimeLightmapIndex;
        T.lightmapIndex = lightmapIndex;
        T.treeMaximumFullLODCount = treeMaximumFullLODCount;
        T.heightmapMaximumLOD = heightmapMaximumLOD;
        T.basemapDistance = basemapDistance;
        T.heightmapPixelError = heightmapPixelError;
        T.detailObjectDensity = detailObjectDensity;
    }
}

[MessagePackObject]
[Serializable]
public class CWTerrainCollider
{
    [Key(0)] public string TerrainDataChecksum;

    public CWTerrainCollider() { }
    public CWTerrainCollider(TerrainCollider TC)
    {
#if UNITY_EDITOR
        TerrainDataChecksum = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TC.terrainData));
#endif
    }

    public void Load(TerrainCollider TC)
    {
        TC.terrainData = CWMap.Instance.GetTerraindata(TerrainDataChecksum);
    }
}



[MessagePackObject]
[Serializable]
public class CWRenderSettings
{
    [Key(0)] public float haloStrength;
    [Key(1)] public int defaultReflectionResolution;
    [Key(2)] public DefaultReflectionMode defaultReflectionMode;
    [Key(3)] public int reflectionBounces;
    [Key(4)] public float reflectionIntensity;
    [Key(5)] public string customReflectionChecksum;
    [Key(6)] public CWSphericalHarmonicsL2 ambientProbe;
    [Key(7)] public string sunGOChecksum;
    [Key(8)] public string skyboxMatChecksum;
    [Key(9)] public Color subtractiveShadowColor;
    [Key(10)] public float flareStrength;
    [Key(11)] public Color ambientLight;
    [Key(12)] public Color ambientGroundColor;
    [Key(13)] public Color ambientEquatorColor;
    [Key(14)] public Color ambientSkyColor;
    [Key(15)] public AmbientMode ambientMode;
    [Key(16)] public float fogDensity;
    [Key(17)] public Color fogColor;
    [Key(18)] public FogMode fogMode;
    [Key(19)] public float fogEndDistance;
    [Key(20)] public float fogStartDistance;
    [Key(21)] public bool fog;
    [Key(22)] public float ambientIntensity;
    [Key(23)] public float flareFadeSpeed;
    public CWRenderSettings() { }

    public CWRenderSettings(RenderSettings RS)
    {
        haloStrength = RenderSettings.haloStrength;
        defaultReflectionResolution = RenderSettings.defaultReflectionResolution;
        defaultReflectionMode = RenderSettings.defaultReflectionMode;
        reflectionBounces = RenderSettings.reflectionBounces;
        reflectionIntensity = RenderSettings.reflectionIntensity;
        if (RenderSettings.customReflection && RenderSettings.defaultReflectionMode == DefaultReflectionMode.Custom) customReflectionChecksum = GetTextureChecksum(RenderSettings.customReflection);
        ambientProbe = new CWSphericalHarmonicsL2(RenderSettings.ambientProbe);
        if (RenderSettings.sun) sunGOChecksum = GetTransformChecksum(RenderSettings.sun.transform);
        if (RenderSettings.skybox && RenderSettings.ambientMode == AmbientMode.Skybox) skyboxMatChecksum = GetMaterialChecksum(RenderSettings.skybox);
        subtractiveShadowColor = RenderSettings.subtractiveShadowColor;
        flareStrength = RenderSettings.flareStrength;
        ambientLight = RenderSettings.ambientLight;
        ambientGroundColor = RenderSettings.ambientGroundColor;
        ambientEquatorColor = RenderSettings.ambientEquatorColor;
        ambientSkyColor = RenderSettings.ambientSkyColor;
        ambientMode = RenderSettings.ambientMode;
        fogDensity = RenderSettings.fogDensity;
        fogColor = RenderSettings.fogColor;
        fogMode = RenderSettings.fogMode;
        fogEndDistance = RenderSettings.fogEndDistance;
        fogStartDistance = RenderSettings.fogStartDistance;
        fog = RenderSettings.fog;
        ambientIntensity = RenderSettings.ambientIntensity;
        flareFadeSpeed = RenderSettings.flareFadeSpeed;
    }

    public void ApplyRenderSettings()
    {
        RenderSettings.haloStrength = haloStrength;
        RenderSettings.defaultReflectionResolution = defaultReflectionResolution;
        RenderSettings.defaultReflectionMode = defaultReflectionMode;
        RenderSettings.reflectionBounces = reflectionBounces;
        RenderSettings.reflectionIntensity = reflectionIntensity;

        if (defaultReflectionMode == DefaultReflectionMode.Skybox)
        {
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            // Create a temporary reflection probe
            GameObject probeGO = new GameObject("TempReflectionProbe");
            ReflectionProbe reflectionProbe = probeGO.AddComponent<ReflectionProbe>();

            // Configure the probe to render only the skybox
            reflectionProbe.mode = ReflectionProbeMode.Custom;
            reflectionProbe.clearFlags = ReflectionProbeClearFlags.Skybox;
            reflectionProbe.cullingMask = 0;

            // Render the skybox to the probe's cubemap
            reflectionProbe.RenderProbe();

            // Assign the cubemap to custom reflection
            RenderSettings.customReflection = reflectionProbe.customBakedTexture;

            // Clean up the temporary reflection probe
            MonoBehaviour.Destroy(probeGO);
        }
        else
        {
            RenderSettings.customReflection = CWMap.Instance.GetTexture(customReflectionChecksum);
        }

        RenderSettings.ambientProbe = ambientProbe.ToProbe();
        if (!string.IsNullOrEmpty(sunGOChecksum))
        {
            foreach (CWGameObject CWGO in CWMap.Instance.GameObjectsOnStage.Values)
            {
                if (CWGO.ID.CompareTo(sunGOChecksum) == 0)
                {
                    RenderSettings.sun = CWGO.gameobject.GetComponent<Light>();
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(skyboxMatChecksum))
            RenderSettings.skybox = CWMap.Instance.GetMaterial(skyboxMatChecksum);

        RenderSettings.subtractiveShadowColor = subtractiveShadowColor;
        RenderSettings.flareStrength = flareStrength;
        RenderSettings.ambientLight = ambientLight;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogEndDistance = fogEndDistance;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fog = fog;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.flareFadeSpeed = flareFadeSpeed;
    }


    /*public void ApplyRenderSettings()
    {
        RenderSettings.haloStrength = haloStrength;
        RenderSettings.defaultReflectionResolution = defaultReflectionResolution;
        RenderSettings.defaultReflectionMode = defaultReflectionMode;
        RenderSettings.reflectionBounces = reflectionBounces;
        RenderSettings.reflectionIntensity = reflectionIntensity;
        RenderSettings.customReflection = CWMap.Instance.GetTexture(customReflectionChecksum);
        RenderSettings.ambientProbe = ambientProbe.ToProbe();
        if (!string.IsNullOrEmpty(sunGOChecksum)) foreach (CWGameObject CWGO in CWMap.Instance.GameObjectsOnStage.Values) if (CWGO.ID.CompareTo(sunGOChecksum) == 0) RenderSettings.sun = CWGO.gameobject.GetComponent<Light>();
        if (!string.IsNullOrEmpty(skyboxMatChecksum)) RenderSettings.skybox = CWMap.Instance.GetMaterial(skyboxMatChecksum);
        RenderSettings.subtractiveShadowColor = subtractiveShadowColor;
        RenderSettings.flareStrength = flareStrength;
        RenderSettings.ambientLight = ambientLight;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogEndDistance = fogEndDistance;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fog = fog;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.flareFadeSpeed = flareFadeSpeed;
    }*/

}


[MessagePackObject]
[Serializable]
public class CWReflectionProbe
{

    [Key(0)] public ReflectionProbeClearFlags clearFlags;
    [Key(1)] public bool boxProjection;
    [Key(2)] public ReflectionProbeMode mode;
    [Key(3)] public int importance;
    [Key(4)] public ReflectionProbeRefreshMode refreshMode;
    [Key(5)] public ReflectionProbeTimeSlicingMode timeSlicingMode;
    [Key(6)] public string customBakedTextureChecksum;
    [Key(7)] public float blendDistance;
    [Key(8)] public Color backgroundColor;
    [Key(9)] public int cullingMask;
    [Key(10)] public Vector3 center;
    [Key(11)] public int resolution;
    [Key(12)] public float shadowDistance;
    [Key(13)] public bool renderDynamicObjects;
    [Key(14)] public bool hdr;
    [Key(15)] public float intensity;
    [Key(16)] public float farClipPlane;
    [Key(17)] public float nearClipPlane;
    [Key(18)] public Vector3 size;


    public CWReflectionProbe(ReflectionProbe RP)
    {
        clearFlags = RP.clearFlags;
        boxProjection = RP.boxProjection;
        mode = RP.mode == ReflectionProbeMode.Realtime ? RP.mode : ReflectionProbeMode.Custom;
        importance = RP.importance;
        refreshMode = RP.refreshMode;
        timeSlicingMode = RP.timeSlicingMode;
        if (RP.customBakedTexture) customBakedTextureChecksum = GetTextureChecksum(RP.customBakedTexture);
        else
        if (RP.bakedTexture) customBakedTextureChecksum = GetTextureChecksum(RP.bakedTexture);
        blendDistance = RP.blendDistance;
        backgroundColor = RP.backgroundColor;
        cullingMask = RP.cullingMask;
        center = RP.center;
        resolution = RP.resolution;
        shadowDistance = RP.shadowDistance;
        renderDynamicObjects = RP.renderDynamicObjects;
        hdr = RP.hdr;
        intensity = RP.intensity;
        farClipPlane = RP.farClipPlane;
        nearClipPlane = RP.nearClipPlane;
        size = RP.size;
    }
    public void Load(ReflectionProbe RP)
    {
        RP.clearFlags = clearFlags;
        RP.boxProjection = boxProjection;
        RP.mode = mode;
        RP.importance = importance;
        RP.refreshMode = refreshMode;
        RP.timeSlicingMode = timeSlicingMode;
        RP.customBakedTexture = CWMap.Instance.GetTexture(customBakedTextureChecksum);
        RP.bakedTexture = CWMap.Instance.GetTexture(customBakedTextureChecksum);
        RP.blendDistance = blendDistance;
        RP.backgroundColor = backgroundColor;
        RP.cullingMask = cullingMask;
        RP.center = center;
        RP.resolution = resolution;
        RP.shadowDistance = shadowDistance;
        RP.renderDynamicObjects = renderDynamicObjects;
        RP.hdr = hdr;
        RP.intensity = intensity;
        RP.farClipPlane = farClipPlane;
        RP.nearClipPlane = nearClipPlane;
        RP.size = size;
    }

    public CWReflectionProbe() { }



}

[MessagePackObject]
[Serializable]
public class CWGameObject
{
    [Key(0)] public string Name;
    [Key(1)] public string Tag;
    [Key(2)] public int layer;
    [Key(3)] public bool StaticFlags;
    [Key(4)] public string ID;
    [Key(5)] public string PID;
    [Key(6)] public CWTransform transform;
    [Key(7)] public CWMeshRenderer meshRenderer = null;
    [Key(8)] public CWBoxCollider[] boxColliders = null;
    [Key(9)] public CWSphereCollider[] sphereColliders = null;
    [Key(10)] public CWCapsuleCollider[] capsuleColliders = null;
    [Key(11)] public CWMeshCollider[] meshColliders = null;
    [Key(12)] public CWAudioSource[] audioSources = null;
    [Key(13)] public CWLight light = null;
    [Key(14)] public CWLodGroup lodGroup = null;
    [Key(15)] public CWSpriteRenderer spriteRenderer = null;
    [Key(16)] public CWTextMesh textMesh = null;
    [Key(17)] public CWReflectionProbe reflectionProbe = null;
    [Key(18)] public CWTerrain terrain = null;
    [Key(19)] public CWTerrainCollider terrainCollider = null;
    [Key(20)] public CWNavMeshLink link = null;
    [Key(21)] public CWPlaceHolder placeHolder = null;

    [IgnoreMember][NonSerialized] public GameObject gameobject = null;
    [IgnoreMember][NonSerialized] public MeshFilter Filter = null;
    [IgnoreMember][NonSerialized] public MeshRenderer MRenderer = null;
    [IgnoreMember][NonSerialized] public TextMesh TMesh = null;
    [IgnoreMember][NonSerialized] public SpriteRenderer SPRenderer = null;
    [IgnoreMember][NonSerialized] public LODGroup LODG = null;
    [IgnoreMember][NonSerialized] public ReflectionProbe RProbe = null;
    [IgnoreMember][NonSerialized] public NavMeshLink Link = null;

    public CWGameObject() { }
    public CWGameObject(GameObject GO, bool Atlas = false)
    {
        Name = GO.name;
        Tag = GO.tag;
        layer = GO.layer;
        StaticFlags = GO.isStatic;
        transform = new CWTransform(GO.transform);
        ID = GetTransformChecksum(GO.transform);
        PID = GO.transform.parent == null ? null : GetTransformChecksum(GO.transform.parent);


        MeshFilter MF = GO.GetComponent<MeshFilter>();
        MeshRenderer MR = GO.GetComponent<MeshRenderer>();
        TextMesh TM = GO.GetComponent<TextMesh>();
        if (MR && MR.sharedMaterials.Length > 0 && MR.sharedMaterials[0] != null && MR.enabled)
        {
            if ((MF && MF.sharedMesh != null) || TM)
            {
                meshRenderer = new CWMeshRenderer(MR, MF, Atlas);
                if (!MF && TM) textMesh = new CWTextMesh(TM);
            }

        }

        BoxCollider[] BCs = GO.GetComponents<BoxCollider>();
        boxColliders = new CWBoxCollider[BCs.Length];
        for (int i = 0; i < BCs.Length; i++) { if (BCs[i].enabled) boxColliders[i] = new CWBoxCollider(BCs[i]); }

        SphereCollider[] SCs = GO.GetComponents<SphereCollider>();
        sphereColliders = new CWSphereCollider[SCs.Length];
        for (int i = 0; i < SCs.Length; i++) { if (SCs[i].enabled) sphereColliders[i] = new CWSphereCollider(SCs[i]); }

        CapsuleCollider[] CCs = GO.GetComponents<CapsuleCollider>();
        capsuleColliders = new CWCapsuleCollider[CCs.Length];
        for (int i = 0; i < CCs.Length; i++) { if (CCs[i].enabled) capsuleColliders[i] = new CWCapsuleCollider(CCs[i]); }

        MeshCollider[] MCs = GO.GetComponents<MeshCollider>();
        meshColliders = new CWMeshCollider[MCs.Length];
        for (int i = 0; i < MCs.Length; i++) { if (MCs[i].sharedMesh != null && MCs[i].enabled) meshColliders[i] = new CWMeshCollider(MCs[i]); }



        AudioSource[] ASs = GO.GetComponents<AudioSource>();
        audioSources = new CWAudioSource[ASs.Length];
        for (int i = 0; i < ASs.Length; i++) { if (ASs[i].clip != null && ASs[i].enabled) audioSources[i] = new CWAudioSource(ASs[i]); }

        LODGroup LG = GO.GetComponent<LODGroup>();
        if (LG && LG.lodCount > 0) lodGroup = new CWLodGroup(LG);

        SpriteRenderer SR = GO.GetComponent<SpriteRenderer>();
        if (SR && SR.sprite && SR.sprite.texture && SR.enabled) spriteRenderer = new CWSpriteRenderer(SR);

        Light l = GO.GetComponent<Light>();
        if (l != null && l.enabled) light = new CWLight(l);

        ReflectionProbe RP = GO.GetComponent<ReflectionProbe>();
        if (RP != null && RP.enabled) reflectionProbe = new CWReflectionProbe(RP);

        Terrain T = GO.GetComponent<Terrain>();
        if (T != null && T.terrainData != null && T.enabled) terrain = new CWTerrain(T);

        TerrainCollider TC = GO.GetComponent<TerrainCollider>();
        if (TC != null && TC.enabled && TC.terrainData != null) terrainCollider = new CWTerrainCollider(TC);

        NavMeshLink NML = GO.GetComponent<NavMeshLink>();
        if (NML != null && NML.enabled) link = new CWNavMeshLink(NML);

        PlaceHolder PH = GO.GetComponent<PlaceHolder>();
        if (PH != null && PH.enabled) placeHolder = new CWPlaceHolder(PH);



    }

    public void Load()
    {
        gameobject = new GameObject(Name);

        gameobject.tag = Tag;
        ModMapManager.Instance.TaggedObjects.TryAdd(gameobject, Tag);
        ModMapManager.Instance.TaggedTransforms.TryAdd(gameobject.transform, Tag);
        gameobject.layer = layer;
        gameobject.isStatic = StaticFlags;
        gameobject.isStatic = true;
        gameobject.transform.position = transform.position;
        gameobject.transform.eulerAngles = transform.rotation;
        gameobject.transform.localScale = transform.scale;

        if (meshRenderer != null)
        {
            MRenderer = gameobject.AddComponent<MeshRenderer>();

            meshRenderer.Deserialize(MRenderer);
            // MRenderer.scaleInLightmap=meshRenderer.scaleInLightmap;
            // MRenderer.stitchLightmapSeams=meshRenderer.stitchLightmapSeams;
            // MRenderer.receiveGI=meshRenderer.receiveGI;
            if (textMesh == null) Filter = gameobject.AddComponent<MeshFilter>(); else textMesh.Load(gameobject.AddComponent<TextMesh>());
        }

        if (spriteRenderer != null)
        {
            SPRenderer = gameobject.AddComponent<SpriteRenderer>();

            spriteRenderer.Deserialize(SPRenderer);
            //SPRenderer.spriteChecksum=SR.sprite==null||SR.sprite.texture==null?null:GetTextureChecksum(SR.sprite.texture);
            SPRenderer.drawMode = spriteRenderer.drawMode;
            SPRenderer.size = spriteRenderer.size;
            SPRenderer.adaptiveModeThreshold = spriteRenderer.adaptiveModeThreshold;
            SPRenderer.tileMode = spriteRenderer.tileMode;
            SPRenderer.color = spriteRenderer.color;
            SPRenderer.maskInteraction = spriteRenderer.maskInteraction;
            SPRenderer.flipX = spriteRenderer.flipX;
            SPRenderer.flipY = spriteRenderer.flipY;
            SPRenderer.spriteSortPoint = spriteRenderer.spriteSortPoint;
        }

        if (boxColliders != null && boxColliders.Length > 0)
        {
            foreach (CWBoxCollider CWBC in boxColliders)
            {
                BoxCollider BC = gameobject.AddComponent<BoxCollider>();
                BC.center = CWBC.center;
                BC.size = CWBC.size;
                BC.contactOffset = CWBC.contactOffset;
                BC.hasModifiableContacts = CWBC.hasModifiableContacts;
                BC.hideFlags = CWBC.hideFlags;
                BC.isTrigger = CWBC.isTrigger;
            }
        }

        if (sphereColliders != null && sphereColliders.Length > 0)
        {
            foreach (CWSphereCollider CWSC in sphereColliders)
            {
                SphereCollider SC = gameobject.AddComponent<SphereCollider>();
                SC.center = CWSC.center;
                SC.radius = CWSC.radius;
                SC.contactOffset = CWSC.contactOffset;
                SC.hasModifiableContacts = CWSC.hasModifiableContacts;
                SC.hideFlags = CWSC.hideFlags;
                SC.isTrigger = CWSC.isTrigger;
            }
        }

        if (capsuleColliders != null && capsuleColliders.Length > 0)
        {
            foreach (CWCapsuleCollider CWBC in capsuleColliders)
            {
                CapsuleCollider CC = gameobject.AddComponent<CapsuleCollider>();
                CC.center = CWBC.center;
                CC.radius = CWBC.radius;
                CC.height = CWBC.height;
                CC.contactOffset = CWBC.contactOffset;
                CC.hasModifiableContacts = CWBC.hasModifiableContacts;
                CC.hideFlags = CWBC.hideFlags;
                CC.isTrigger = CWBC.isTrigger;
            }
        }

        if (lodGroup != null) LODG = gameobject.AddComponent<LODGroup>();

        if (light != null) light.Load(gameobject.AddComponent<Light>());

        if (reflectionProbe != null) { reflectionProbe.Load(gameobject.AddComponent<ReflectionProbe>()); }

        if (terrain != null) { terrain.Load(gameobject.AddComponent<Terrain>()); }

        if (terrainCollider != null) { terrainCollider.Load(gameobject.AddComponent<TerrainCollider>()); }

        if (link != null) link.Load(gameobject.AddComponent<NavMeshLink>());

        if (placeHolder != null) placeHolder.Load(gameobject.AddComponent<PlaceHolder>());

    }
}





