using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using static CWModUtility;
using static BF2FileManager;
using System;
using Random = UnityEngine.Random;

public class GrowthLoader : MonoBehaviour
{
    public Terrain terrain; // Reference to the Terrain object
    public List<TextureAtlasInfo> textures;
    public UndergrowthConfig UndergrowthConfigInfo;
    public OvergrowthConfig OvergrowthConfigInfo;

    public string UndergrowthtRawFilePath; // Path to the .raw file
    public string UndergrowthtCfgFilePath;
    public string UndergrowthtTaiFilePath; // Path to the .tai file


    public string OvergrowthtRawFilePath; // Path to the .raw file


    public int TextureSize;
    //public int NTextureSize;

    public Info[] UndergrowthValues;
    public Info[] OvergrowthValues;
    public Dictionary<int, Info> UniuqeUndergrowthValues = new Dictionary<int, Info>();
    public Dictionary<int, Info> UniuqeOvergrowthValues = new Dictionary<int, Info>();

    public static GrowthLoader _Instance;
    public static GrowthLoader Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindObjectOfType<GrowthLoader>();
                if (_Instance == null)
                {
                    _Instance = new GameObject("GrowthLoader").AddComponent<GrowthLoader>();
                }
            }
            return _Instance;
        }
    }

    public void LoadUnderGrowth(Terrain terr, string rootPath)
    {
        terrain = terr;
        UndergrowthtRawFilePath = Path.Combine(rootPath, "Undergrowth.raw");
        UndergrowthtCfgFilePath = Path.Combine(rootPath, "Undergrowth.cfg");
        UndergrowthtTaiFilePath = Path.Combine(rootPath, "UndergrowthAtlas.tai");

        UndergrowthConfigInfo = new UndergrowthConfig(UndergrowthtCfgFilePath);

        UnpackAtlas();
        SetDetailTextureFromRaw();
        AssignDetailMaterials();
    }

    public void LoadOverGrowth(Terrain terr, string ConPath)
    {
        OvergrowthConfigInfo = new OvergrowthConfig(ConPath);

        terrain = terr;
        OvergrowthtRawFilePath = Path.Combine(OvergrowthConfigInfo.OvergrowthPath, "Overgrowth.raw");


        SetTreeTextureFromRaw();
    }


    [System.Serializable]
    public class Info
    {
        public int ID;
        public int Ammount;
        [System.NonSerialized] public int[,] detailMap;
        [System.NonSerialized] public int[,] TreeMap;
        public float density;

        public Texture2D TX;
        public Texture2D TX2;

        public OvergrowthMaterialInfo MaterialInfo;
    }

    [Button("Set Details Controll")]

    void SetDetailTextureFromRaw()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain not set.");
            return;
        }

        if (string.IsNullOrEmpty(UndergrowthtRawFilePath))
        {
            Debug.LogError("RAW file path not set.");
            return;
        }
        TerrainData terrainData = terrain.terrainData;

        byte[] rawFileData = FileManager.ReadAllBytes(UndergrowthtRawFilePath);

        // Assuming the file is square and 8-bit grayscale, calculate dimensions
        TextureSize = Mathf.RoundToInt(Mathf.Sqrt(rawFileData.Length));
        //NTextureSize= ToPowerOf2(TextureSize);
        if (TextureSize * TextureSize != rawFileData.Length)
        {
            Debug.LogError("RAW file size does not match expected square texture size.");
            return;
        }


        // Resize the terrain detail size to match the texture size
        terrainData.SetDetailResolution(TextureSize, 8); // 8 is the resolution per patch

        int maxValue = -1;
        UniuqeUndergrowthValues.Clear();
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                // The data is assumed to be in 8-bit grayscale format
                int index = x * TextureSize + y;
                byte pixelValue = rawFileData[index];
                if (!UniuqeUndergrowthValues.TryGetValue(pixelValue, out Info inf))
                {
                    inf = new Info();
                    inf.ID = pixelValue;
                    maxValue = Mathf.Max(maxValue, pixelValue);
                    //inf.TX = new Texture2D(TextureSize, TextureSize, TextureFormat.R8, false, false);
                    inf.detailMap = new int[TextureSize, TextureSize];
                    UniuqeUndergrowthValues.Add(pixelValue, inf);
                }
                inf.Ammount++;
                //inf.TX.SetPixel(y, x, Color.black);
                // Use the grayscale value to determine the density of the grass
                //int density = Mathf.RoundToInt(pixelValue / 255f * densityMultiplier);

                inf.detailMap[x, y] = 255;
            }
        }

        // Set the detail map for the first detail prototype
        UndergrowthValues = UniuqeUndergrowthValues.Values.ToArray();
        /*List<DetailPrototype> DetailPrefabs = terrainData.detailPrototypes.ToList();
        for (int i = DetailPrefabs.Count; DetailPrefabs.Count <= maxValue; i++)
        {
            DetailPrefabs.Add(new DetailPrototype());
            if (i > 100) { Debug.LogError("Long Loop Found, Exiting"); break; }
        }
        terrainData.detailPrototypes = DetailPrefabs.ToArray();*/
        foreach (Info inf in UndergrowthValues)
        {

            //inf.TX.Apply();
            //if (FileManager.Exists($"Assets/Scripts/BF2Tools/TestOBJS/UnderGrowth_{inf.ID}.Texture2D")) FileManager.Delete($"Assets/Scripts/BF2Tools/TestOBJS/UnderGrowth_{inf.ID}.Texture2D");
            //AssetDatabase.Refresh();
            //AssetDatabase.CreateAsset(inf.TX, $"Assets/Scripts/BF2Tools/TestOBJS/UnderGrowth_{inf.ID}.Texture2D");
            //terrainData.SetDetailLayer(0, 0, inf.ID, inf.detailMap);
            //inf.detailMap=null;
        }
    }

    [Button("Set Tree Controll")]
    void SetTreeTextureFromRaw()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain not set.");
            return;
        }

        if (string.IsNullOrEmpty(OvergrowthtRawFilePath))
        {
            Debug.LogError("RAW file path not set.");
            return;
        }
        TerrainData terrainData = terrain.terrainData;

        byte[] rawFileData = FileManager.ReadAllBytes(OvergrowthtRawFilePath);

        // Assuming the file is square and 8-bit grayscale, calculate dimensions
        TextureSize = Mathf.RoundToInt(Mathf.Sqrt(rawFileData.Length));
        //NTextureSize= ToPowerOf2(TextureSize);
        if (TextureSize * TextureSize != rawFileData.Length)
        {
            Debug.LogError("RAW file size does not match expected square texture size.");
            return;
        }


        // Resize the terrain detail size to match the texture size

        int maxValue = -1;
        UniuqeOvergrowthValues.Clear();
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                // The data is assumed to be in 8-bit grayscale format
                int index = y * TextureSize + x;
                byte pixelValue = rawFileData[index];
                if (!UniuqeOvergrowthValues.TryGetValue(pixelValue, out Info inf))
                {
                    inf = new Info();
                    inf.ID = pixelValue;
                    maxValue = Mathf.Max(maxValue, pixelValue);
                    //inf.TX = new Texture2D(TextureSize, TextureSize, TextureFormat.R8, false, false);
                    inf.TreeMap = new int[TextureSize, TextureSize];
                    UniuqeOvergrowthValues.Add(pixelValue, inf);
                }
                inf.Ammount++;
                //inf.TX.SetPixel(y, x, Color.black);
                // Use the grayscale value to determine the density of the grass
                //int density = Mathf.RoundToInt(pixelValue / 255f * densityMultiplier);

                inf.TreeMap[x, y] = 255;
            }
        }

        // Set the detail map for the first detail prototype
        OvergrowthValues = UniuqeOvergrowthValues.Values.ToArray();
        /*List<DetailPrototype> DetailPrefabs = terrainData.detailPrototypes.ToList();
        for (int i = DetailPrefabs.Count; DetailPrefabs.Count <= maxValue; i++)
        {
            DetailPrefabs.Add(new DetailPrototype());
            if (i > 100) { Debug.LogError("Long Loop Found, Exiting"); break; }
        }
        terrainData.detailPrototypes = DetailPrefabs.ToArray();*/
        List<TreePrototype> treePrototypes = new List<TreePrototype>(terrainData.treePrototypes);
        List<TreeInstance> treeInstances = new List<TreeInstance>(terrainData.treeInstances);

        foreach (Info inf in OvergrowthValues)
        {
            foreach (OvergrowthMaterialInfo OMI in OvergrowthConfigInfo.Materials)
            {
                if (OMI.ID == inf.ID) inf.MaterialInfo = OMI;
            }
            if (inf.MaterialInfo == null) continue;

            foreach (OvergrowthTypeInfo OTI in inf.MaterialInfo.Types)
            {
                float Density = OTI.Density;
                float minRadiusToSame = OTI.minRadiusToSame;
                float minRadiusToOthers = OTI.minRadiusToOthers;
                GameObject mesh = OTI.Mesh;
                if (mesh == null) continue;
                float YOffset = 0;
                float Radius = 0;

                if (mesh.transform.GetChild(0))
                    if (mesh.transform.GetChild(0).GetChild(0))
                        if (mesh.transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>())
                        {
                            Vector3[] vertices = mesh.transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices;

                            foreach (Vector3 v3 in vertices) { YOffset = Mathf.Min(YOffset, v3.y); Radius = Mathf.Max(Math.Abs(v3.x), Math.Abs(v3.z), Radius); }

                            if (!Directory.Exists("Assets/Cache/Prefabs/")) Directory.CreateDirectory("Assets/Cache/Prefabs/");
                            string localPath = "Assets/Cache/Prefabs/" + mesh.name + ".prefab";
                            PrefabUtility.SaveAsPrefabAsset(mesh.transform.GetChild(0).gameObject, localPath);
                        }
                minRadiusToSame += Radius;
                minRadiusToOthers += Radius;
                YOffset *= 0.95f;

                // Ensure the tree prototype is added only once
                int treeIndex = treePrototypes.FindIndex(tp => tp.prefab == mesh);
                if (treeIndex == -1)
                {
                    TreePrototype newTreePrototype = new TreePrototype { prefab = mesh.transform.GetChild(0).gameObject };
                    treePrototypes.Add(newTreePrototype);
                    treeIndex = treePrototypes.Count - 1;
                }
                Vector3 TerrainSize = terrainData.size;
                TerrainSize.y = terrainData.heightmapScale.y;
                float[,] noiseMap = GenerateNoiseMap((int)TextureSize, (int)TextureSize, Density);
                //inf.TX=new Texture2D(TextureSize,TextureSize);
                //inf.TX2=new Texture2D(TextureSize,TextureSize);

                for (int y = 0; y < TextureSize; y++)
                {
                    for (int x = 0; x < TextureSize; x++)
                    {
                        //inf.TX.SetPixel(x, y, Color.white*noiseMap[x,y]);

                        if (inf.TreeMap[x, y] == 255)
                        {
                            //inf.TX2.SetPixel(x, y, Color.white*noiseMap[x,y]);
                            float posX = (float)x / TextureSize;
                            float posZ = (float)y / TextureSize;
                            float posY = (terrainData.GetInterpolatedHeight(posX, posZ) - YOffset) / TerrainSize.y;

                            float GposX = TerrainSize.x * posX;
                            float GposZ = TerrainSize.z * posZ;

                            float densityCheck = noiseMap[x, y];
                            if (densityCheck >= 0.7f)
                            {
                                Vector3 newTreePos = new Vector3(posX, posY, posZ);
                                bool canPlaceTree = true;

                                foreach (TreeInstance instance in treeInstances)
                                {
                                    Vector3 existingTreePos = instance.position;
                                    float distance = Vector3.Distance(new Vector3(existingTreePos.x * TerrainSize.x, 0, existingTreePos.z * TerrainSize.z), new Vector3(GposX, 0, GposZ));

                                    if (instance.prototypeIndex == treeIndex)
                                    {
                                        if (distance < minRadiusToSame)
                                        {
                                            canPlaceTree = false;
                                            //Debug.Log($"Tree at ({GposX}, {posY}, {GposZ}) too close to another tree of the same type At {new Vector3(existingTreePos.x*TerrainSize.x, 0, existingTreePos.z*TerrainSize.z)}   -   (distance: {distance}),(TRad: { minRadiusToSame+Radius})");
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (distance < minRadiusToOthers)
                                        {
                                            canPlaceTree = false;
                                            //Debug.Log($"Tree at ({GposX}, {posY}, {GposZ}) too close to another tree of a different type At {new Vector3(existingTreePos.x*TerrainSize.x, 0, existingTreePos.z*TerrainSize.z)}   -    (distance: {distance}),(TRad: { minRadiusToOthers+Radius})");
                                            break;
                                        }
                                    }
                                }

                                if (canPlaceTree)
                                {
                                    TreeInstance treeInstance = new TreeInstance
                                    {
                                        position = newTreePos,
                                        prototypeIndex = treeIndex,
                                        widthScale = 1.0f,
                                        heightScale = 1.0f,
                                        color = Color.white,
                                        lightmapColor = Color.white
                                    };
                                    treeInstances.Add(treeInstance);
                                    //Debug.Log($"Placed tree at ({GposX}, {posY}, {GposZ})");
                                }
                            }
                            else
                            {
                                //Debug.Log($"Density check failed for tree at ({GposX}, {posY}, {GposZ})");
                            }
                        }
                    }
                }
                /*inf.TX.Apply();
                inf.TX2.Apply();
            if (File.Exists($"Assets/Scripts/BF2Tools/TestOBJS/OverGrowthNoise_{OTI.Name}.Texture2D")) File.Delete($"Assets/Scripts/BF2Tools/TestOBJS/OverGrowthNoise_{OTI.Name}.Texture2D");
            if (File.Exists($"Assets/Scripts/BF2Tools/TestOBJS/OverGrowthNoise_{OTI.Name}_Part.Texture2D")) File.Delete($"Assets/Scripts/BF2Tools/TestOBJS/OverGrowthNoise_{OTI.Name}_Part.Texture2D");
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(inf.TX, $"Assets/Scripts/BF2Tools/TestOBJS/OverGrowthNoise_{OTI.Name}.Texture2D");
            AssetDatabase.CreateAsset(inf.TX2, $"Assets/Scripts/BF2Tools/TestOBJS/OverGrowthNoise_{OTI.Name}_Part.Texture2D");
            */
                inf.detailMap = null;
            }
        }


        terrainData.treePrototypes = treePrototypes.ToArray();
        terrainData.treeInstances = treeInstances.ToArray();
        terrain.treeDistance = OvergrowthConfigInfo.viewDistance;
        Debug.Log($"Total trees placed: {treeInstances.Count}");
    }

    [Serializable]
    public class TextureAtlasInfo
    {
        public string Filename;
        public string AtlasFilename;
        public int AtlasIndex;
        public float WOffset;
        public float HOffset;
        public float Width;
        public float Height;
        public Texture2D TX;

        public Texture2D ToTexture(Texture2D atlas = null)
        {
            if (!Directory.Exists("Assets/Cache/Textures/")) Directory.CreateDirectory("Assets/Cache/Textures/");
            Texture2D cachedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cache/Textures/" + (AtlasFilename + AtlasIndex + "_" + "_" + Filename).Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Texture2D");
            if (cachedTexture)
            {
                return cachedTexture;
            }

            if (!atlas) atlas = DDSLoader.LoadDDSTexture(AtlasFilename, false);
            if (TX != null) return TX;
            int x = Mathf.FloorToInt(WOffset * atlas.width);
            int y = Mathf.FloorToInt(HOffset * atlas.height);
            int width = Mathf.FloorToInt(Width * atlas.width);
            int height = Mathf.FloorToInt(Height * atlas.height);

            Texture2D newTexture = new Texture2D(width, height, atlas.format, atlas.mipmapCount > 0);
            newTexture.name = Path.GetFileNameWithoutExtension(Filename);
            newTexture.SetPixels(atlas.GetPixels(x, y, width, height));
            newTexture.Apply();

            AssetDatabase.CreateAsset(newTexture, "Assets/Cache/Textures/" + (AtlasFilename + AtlasIndex + "_" + "_" + Filename).Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Texture2D");
            return newTexture;
        }
    }

    public Texture2D TestAtlas;
    [Button("Unpack Atlas")]
    void UnpackAtlas()
    {
        textures = ParseAtlasFile(UndergrowthtTaiFilePath);
        foreach (var texture in textures)
        {
            texture.TX = texture.ToTexture(TestAtlas);
            Debug.Log($"Filename: {texture.Filename}, AtlasFilename: {texture.AtlasFilename}, AtlasIndex: {texture.AtlasIndex}, WOffset: {texture.WOffset}, HOffset: {texture.HOffset}, Width: {texture.Width}, Height: {texture.Height}");
        }
    }

    List<TextureAtlasInfo> ParseAtlasFile(string path)
    {
        var textureInfoList = new List<TextureAtlasInfo>();

        if (!FileManager.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            return textureInfoList;
        }

        var lines = FileManager.ReadAllLines(path);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split(new[] { '\t', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 7)
            {
                Debug.LogWarning($"Invalid line format: {line}");
                continue;
            }

            var textureInfo = new TextureAtlasInfo
            {
                Filename = parts[0],
                AtlasFilename = parts[1],
                AtlasIndex = int.Parse(parts[2]),
                WOffset = float.Parse(parts[3]),
                HOffset = float.Parse(parts[4]),
                Width = float.Parse(parts[5]),
                Height = float.Parse(parts[6])
            };

            textureInfoList.Add(textureInfo);
        }

        return textureInfoList;
    }




    [Button("Assign DetailMaterials")]
    void AssignDetailMaterials()
    {
        if (FileManager.Exists(UndergrowthtCfgFilePath))
        {

            terrain.detailObjectDistance = UndergrowthConfigInfo.ViewDistance;
            terrain.terrainData.wavingGrassStrength = UndergrowthConfigInfo.SwayScale;
            //terrain.scal=UndergrowthConfigInfo.SwayScale;

            // Example of accessing parsed data
            Debug.Log($"View Distance: {UndergrowthConfigInfo.ViewDistance}");
            TerrainData terrainData = terrain.terrainData;
            List<DetailPrototype> prototypes = new List<DetailPrototype>();

            int index = 0;
            foreach (var material in UndergrowthConfigInfo.Materials)
            {
                Debug.Log($"Material: {material.Name}, GeneralHeight: {material.GeneralHeight}");

                if (material.Types.Count == 0) continue;


                foreach (UndergrowthTypeInfo type in material.Types)
                {
                    DetailPrototype prototype = new DetailPrototype();
                    foreach (TextureAtlasInfo TAI in textures) if (TAI.Filename.ToLower().Contains(type.Texture.ToLower())) prototype.prototypeTexture = TAI.ToTexture(TestAtlas);
                    if (prototype.prototypeTexture == null) continue;


                    prototype.maxHeight = type.RandomSizeScale.x;
                    prototype.maxWidth = type.RandomSizeScale.x;

                    prototype.minHeight = type.RandomSizeScale.y;
                    prototype.minWidth = type.RandomSizeScale.y;
                    prototype.noiseSpread = type.Variation;
                    prototype.renderMode = DetailRenderMode.GrassBillboard;
                    prototypes.Add(prototype);
                    terrainData.detailPrototypes = prototypes.ToArray();

                    int[,] nDetailMap = UniuqeUndergrowthValues[material.ID].detailMap;
                    for (int y = 0; y < nDetailMap.GetLength(0); y++)
                    {
                        for (int x = 0; x < nDetailMap.GetLength(1); x++)
                        {
                            nDetailMap[y, x] = (int)(nDetailMap[y, x] * type.Density);
                        }
                    }

                    terrainData.SetDetailLayer(0, 0, index, nDetailMap);
                    nDetailMap = null;
                    index++;
                    Debug.Log($"  Type: {type.Name}, Mesh: {type.Mesh}, Texture: {type.Texture}");
                }

            }
        }
        else
        {
            Debug.LogError($"UndergrowthConfig file not found: {UndergrowthtCfgFilePath}");
        }
    }

    [Serializable]
    public class UndergrowthConfig
    {
        public float ViewDistance;
        public float ViewDistanceFadeScale;
        public float ViewDistanceHeightScale;
        public float ViewDistanceHeight2Scale;
        public float ViewDistanceStreamingScale;
        public int PatchSubdivide;
        public float SwayScale;
        public float LightingScale;
        public float AlphaRef;
        public List<UndergrowthMaterialInfo> Materials = new List<UndergrowthMaterialInfo>();

        public UndergrowthConfig() { }
        public UndergrowthConfig(string path)
        {
            //var config = this;
            var Content = BF2FileManager.FileManager.ReadAllText(path);
            var lines = Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            UndergrowthMaterialInfo currentMaterial = null;
            UndergrowthTypeInfo currentTypeInfo = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                var parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                switch (parts[0])
                {
                    case "ViewDistance":
                        ViewDistance = float.Parse(parts[1]);
                        break;
                    case "ViewDistanceFadeScale":
                        ViewDistanceFadeScale = float.Parse(parts[1]);
                        break;
                    case "ViewDistanceHeightScale":
                        ViewDistanceHeightScale = float.Parse(parts[1]);
                        break;
                    case "ViewDistanceHeight2Scale":
                        ViewDistanceHeight2Scale = float.Parse(parts[1]);
                        break;
                    case "ViewDistanceStreamingScale":
                        ViewDistanceStreamingScale = float.Parse(parts[1]);
                        break;
                    case "PatchSubdivide":
                        PatchSubdivide = int.Parse(parts[1]);
                        break;
                    case "SwayScale":
                        SwayScale = float.Parse(parts[1]);
                        break;
                    case "LightingScale":
                        LightingScale = float.Parse(parts[1]);
                        break;
                    case "AlphaRef":
                        AlphaRef = float.Parse(parts[1]);
                        break;
                    case "Material":
                        currentMaterial = new UndergrowthMaterialInfo
                        {
                            Name = parts[1],
                            ID = int.Parse(parts[2]),
                        };
                        Materials.Add(currentMaterial);
                        break;
                    case "Type":
                        currentTypeInfo = new UndergrowthTypeInfo
                        {
                            Name = parts[1]
                        };
                        currentMaterial.Types.Add(currentTypeInfo);
                        break;
                    case "Mesh":
                        currentTypeInfo.Mesh = parts[1];
                        break;
                    case "Texture":
                        currentTypeInfo.Texture = parts[1];
                        break;
                    case "RandomSizeScale":
                        currentTypeInfo.RandomSizeScale = new Vector2(float.Parse(parts[1]), float.Parse(parts[2]));
                        break;
                    case "Density":
                        currentTypeInfo.Density = float.Parse(parts[1]);
                        break;
                    case "Variation":
                        currentTypeInfo.Variation = float.Parse(parts[1]);
                        break;
                    case "TerrainColorScale":
                        currentTypeInfo.TerrainColorScale = float.Parse(parts[1]);
                        break;
                    case "TypeSwayScale":
                        currentTypeInfo.TypeSwayScale = float.Parse(parts[1]);
                        break;
                    case "Skew":
                        currentTypeInfo.Skew = float.Parse(parts[1]);
                        break;

                }
            }


        }
    }

    [Serializable]
    public class OvergrowthConfig
    {
        public float viewDistance;
        public string OvergrowthPath;
        public List<OvergrowthMaterialInfo> Materials = new List<OvergrowthMaterialInfo>();

        public OvergrowthConfig() { }
        public OvergrowthConfig(string path)
        {
            //var config = this;
            var Content = BF2FileManager.FileManager.ReadAllText(path);
            var lines = Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            OvergrowthMaterialInfo currentMaterial = null;
            OvergrowthTypeInfo currentTypeInfo = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                var parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                switch (parts[0])
                {
                    case "ViewDistance":
                        viewDistance = float.Parse(parts[1]);
                        break;
                    case "Overgrowth.addMaterial":
                        currentMaterial = new OvergrowthMaterialInfo
                        {
                            Name = parts[1],
                            ID = int.Parse(parts[2]),
                        };
                        Materials.Add(currentMaterial);
                        break;
                    case "Overgrowth.addType":
                        currentTypeInfo = new OvergrowthTypeInfo
                        {
                            Name = parts[1]
                        };
                        currentMaterial.Types.Add(currentTypeInfo);
                        break;
                    case "OvergrowthType.geometry":
                        currentTypeInfo.Mesh = MeshLoader.GetMeshTemplate(parts[1], true);
                        break;
                    case "OvergrowthType.density":
                        currentTypeInfo.Density = float.Parse(parts[1]);
                        break;
                    case "OvergrowthType.minRadiusToSame":
                        currentTypeInfo.minRadiusToSame = float.Parse(parts[1]);
                        break;
                    case "OvergrowthType.minRadiusToOthers":
                        currentTypeInfo.minRadiusToOthers = float.Parse(parts[1]);
                        break;
                    case "Overgrowth.path":
                        OvergrowthPath = parts[1];
                        break;

                }
            }


        }
    }


    [Serializable]
    public class UndergrowthMaterialInfo
    {
        public string Name;
        public int ID;
        public float GeneralHeight;
        [System.NonSerialized] public int[,] detailMap;
        public List<UndergrowthTypeInfo> Types = new List<UndergrowthTypeInfo>();
    }

    [Serializable]
    public class OvergrowthMaterialInfo
    {
        public string Name;
        public int ID;
        public float GeneralHeight;
        [System.NonSerialized] public int[,] detailMap;
        public List<OvergrowthTypeInfo> Types = new List<OvergrowthTypeInfo>();
    }

    [Serializable]
    public class UndergrowthTypeInfo
    {
        public string Name;
        public string Mesh;
        public string Texture;
        public Vector2 RandomSizeScale;
        public float Density;
        public float Variation;
        public float TerrainColorScale;
        public float TypeSwayScale;
        public float Skew;
    }

    [Serializable]
    public class OvergrowthTypeInfo
    {
        public string Name;
        public GameObject Mesh;
        public float Density;
        public float minRadiusToSame;
        public float minRadiusToOthers;
    }
}
