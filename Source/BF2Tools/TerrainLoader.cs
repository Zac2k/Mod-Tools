using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;
using static CWModUtility;
using UnityMeshSimplifier;

public class TerrainLoader : MonoBehaviour
{


    [System.Serializable]
    public class TerrainInfo
    {
        public int ID = -1;
        public Terrain terrain;
        public MeshRenderer[] renderers;
        public MeshFilter[] filters;
        public Material[] Mats;
    }

    public Texture2D Lightmap;
    public Texture2D ShadowMask;

    public Terrain terrain;

    public int PatchSize = 128;

    public Mesh[] meshes;
    public Material[] Mats;

    public List<TerrainInfo> terrains = new List<TerrainInfo>();

    public static TerrainLoader _Instance;
    public static TerrainLoader Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindObjectOfType<TerrainLoader>();
                if (_Instance == null)
                {
                    _Instance = new GameObject("TerrainLoader").AddComponent<TerrainLoader>();
                }
            }
            return _Instance;
        }
    }

    public void AddTerrain(Terrain terrain, int PatchSize)
    {
        Vector2 UVMult = new Vector2(1, 1);
        TerrainInfo TI = new TerrainInfo();
        TI.ID = terrains.Count;
        bool rotate = TI.ID == 3 || TI.ID == 6 || TI.ID == 5 || TI.ID == 4;

        UVMult.x = TI.ID == 5 || TI.ID == 3 || TI.ID == 2 || TI.ID == 1 ? -1 : 1;
        UVMult.y = TI.ID == 8 || TI.ID == 7 || TI.ID == 3 || TI.ID == 0 ? -1 : 1;

        Mesh[] terrainmeshes = GenerateMeshesFromTerrain(terrain, PatchSize, UVMult, rotate, TI.ID == 0,TI.ID==0?MapLoader.Instance.TerrainQuality:MapLoader.Instance.TerrainQuality/2);
        TI.filters = new MeshFilter[terrainmeshes.Length];
        TI.renderers = new MeshRenderer[terrainmeshes.Length];
        TI.Mats = new Material[terrainmeshes.Length];

        int sqrt = (int)Mathf.Sqrt(terrainmeshes.Length);
        // Create a new GameObject for each mesh and set its position
        for (int x = 0; x < sqrt; x++)
            for (int y = 0; y < sqrt; y++)
            {
                int i = y * sqrt + x;
                Mesh patchMesh = terrainmeshes[i];

                // Create a new GameObject
                GameObject patchObject = new GameObject($"TerrainPatch_{i}");

                patchObject.transform.SetParent(terrain.transform);
                patchObject.transform.localPosition = Vector3.zero;
                TI.filters[i] = patchObject.AddComponent<MeshFilter>();
                TI.renderers[i] = patchObject.AddComponent<MeshRenderer>();
                TI.Mats[i] = new Material(Shader.Find("BF2/Terrain"));
                TI.filters[i].sharedMesh = patchMesh;

                patchObject.isStatic = true;
                if (TI.ID == 0)
                {
                    TI.renderers[i].lightmapIndex = 0;
                    //TI.renderers[i].lightmapScaleOffset = new Vector4(1, 1, 1, 1);
                }
                else
                {
                    TI.renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                TI.renderers[i].sharedMaterial = TI.Mats[i];
            }
        terrains.Add(TI);
    }


    [System.Serializable]
    public class BF2TerrainData
    {
        [System.Serializable]
        public class BF2TerrainMaterial
        {

            public string Path = ""; // Array of Detail paths
            public byte PlaneMap = 0;
            public Vector2 SideTilling;
            public float TopTilling = 0;
            public float YOffset = 0;
            public byte UB1 = 0;
        }

        public string[] Paths; // Array of paths
        public List<BF2TerrainMaterial> TerrainMaterials = new List<BF2TerrainMaterial>(); // Array of Detail Mats
    }


    public string TerrainDataPath = "";
    public BF2TerrainData bF2TerrainData;

    public BF2TerrainData ReadTerrainData(string terrainDataPath)
    {
        TerrainDataPath = terrainDataPath;
        ReadTerrainData();
        return bF2TerrainData;
    }
    [Button("Try Load TerrainData")]
    void ReadTerrainData()
    {
        if (string.IsNullOrEmpty(TerrainDataPath))
        {
            Debug.LogError("TerrainDataPath is not set.");
            return;
        }

        try
        {
            using (MemoryStream fileStream = (MemoryStream)BF2FileManager.FileManager.Open(TerrainDataPath))
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                bF2TerrainData = new BF2TerrainData();
                fileStream.Position = 57;

                bF2TerrainData.Paths = new string[4];
                for (int i = 0; i < bF2TerrainData.Paths.Length; i++)
                {
                    bF2TerrainData.Paths[i] = ReadString(reader);
                }

                fileStream.Position += 56;

                int matcount = reader.ReadInt32();
                Debug.Log($"MatCount : {matcount}");
                if (matcount > 6) { Debug.LogError("MatCount Is More Than 6 : " + matcount); return; }
                for (int i = 0; i < matcount; i++)
                {
                    BF2TerrainData.BF2TerrainMaterial TMat = new BF2TerrainData.BF2TerrainMaterial();
                    TMat.Path = ReadString(reader);
                    TMat.PlaneMap = reader.ReadByte();
                    TMat.SideTilling = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    TMat.TopTilling = reader.ReadSingle();
                    TMat.YOffset = reader.ReadSingle();
                    TMat.UB1 = reader.ReadByte();
                    bF2TerrainData.TerrainMaterials.Add(TMat);
                }

            }

        }

        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [Button("Set ShadowMask")]
    void SetShadowMask()
    {
        LightmapData[] LMDs = new LightmapData[1];
        for (int i = 0; i < LMDs.Length; i++)
        {
            LMDs[i] = new LightmapData();
            LMDs[i].lightmapColor = Lightmap;
            LMDs[i].lightmapDir = null;
            LMDs[i].shadowMask = ShadowMask;
        }
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = LMDs;

        DynamicGI.UpdateEnvironment();
    }

    [Button("Generate Mesh From Terrain")]
    public void GetTerrainMesh()
    {
        // Clear existing child GameObjects if needed
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child != transform)
                DestroyImmediate(child.gameObject);
        }


        // Generate meshes from the terrain
        meshes = GenerateMeshesFromTerrain(terrain, PatchSize, new Vector2(1, -1), false, true,MapLoader.Instance.TerrainQuality);
        int sqrt = (int)Mathf.Sqrt(meshes.Length);
        // Create a new GameObject for each mesh and set its position
        for (int x = 0; x < sqrt; x++)
            for (int y = 0; y < sqrt; y++)
            {
                int i = y * sqrt + x;
                Mesh patchMesh = meshes[i];

                // Create a new GameObject
                GameObject patchObject = new GameObject($"TerrainPatch_{i}");

                // Set the new GameObject as a child of the current object
                patchObject.transform.SetParent(transform);
                patchObject.transform.localPosition = Vector3.zero;
                // Add a MeshFilter and MeshRenderer components
                MeshFilter meshFilter = patchObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = patchObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = Mats[i];
                // Assign the mesh to the MeshFilter
                meshFilter.sharedMesh = patchMesh;

                patchObject.isStatic = true;
                meshRenderer.lightmapIndex = 0;
                float scale = 1 / PatchSize;
                //meshRenderer.lightmapScaleOffset = new Vector4(scale, scale, (x / sqrt) + (scale / 2), (y / sqrt) + (scale / 2));
                //meshRenderer.lightmapScaleOffset = new Vector4(1, 1, 1, 1);
                // Optionally set a material for the MeshRenderer (make sure you have a suitable material)
                // meshRenderer.material = someMaterial;

                // Set the position of the patchObject (you might need to adjust this based on your needs)
                int patchX = (i % (terrain.terrainData.heightmapResolution / PatchSize)) * PatchSize;
                int patchY = (i / (terrain.terrainData.heightmapResolution / PatchSize)) * PatchSize;

                float patchWidth = terrain.terrainData.size.x / (terrain.terrainData.heightmapResolution / PatchSize);
                float patchLength = terrain.terrainData.size.z / (terrain.terrainData.heightmapResolution / PatchSize);

                //patchObject.transform.position = new Vector3(patchX * patchWidth, 0, patchY * patchLength);
            }
    }
    public static Mesh[] GenerateMeshesFromTerrain(Terrain terrain, int patchSize, Vector2 uvMult, bool rotate, bool HasUv2, float Quality = 1)
    {
        TerrainData terrainData = terrain.terrainData;
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;

        // Ensure width and height are the same
        if (width != height)
        {
            Debug.LogError("Terrain heightmap resolution width and height are not equal. This script requires a square terrain.");
            return null;
        }

        float terrainWidth = terrainData.size.x;
        float terrainHeight = terrainData.size.y;
        float terrainLength = terrainData.size.z;

        // Calculate the number of patches
        int numPatches = (width / patchSize) * (height / patchSize);
        Mesh[] meshes = new Mesh[numPatches];
        int patchIndex = 0;
        List<int> VertsToDissolve = new List<int>();

        for (int patchX = 0; patchX < width - 1; patchX += patchSize)
        {
            for (int patchY = 0; patchY < height - 1; patchY += patchSize)
            {
                Mesh mesh = new Mesh();
                int patchWidth = Mathf.Min(patchSize + 1, width - patchX);
                int patchHeight = Mathf.Min(patchSize + 1, height - patchY);

                Vector3[] vertices = new Vector3[patchWidth * patchHeight];
                if (patchWidth * patchHeight >= 60000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                int[] triangles = new int[(patchWidth - 1) * (patchHeight - 1) * 6];
                Vector2[] uv0 = new Vector2[vertices.Length];
                Vector2[] uv1 = HasUv2 ? new Vector2[vertices.Length] : null;

                for (int y = 0; y < patchHeight; y++)
                {
                    for (int x = 0; x < patchWidth; x++)
                    {
                        if (Random.Range(0, 2) == 0) VertsToDissolve.Add(y * patchWidth + x);
                        int globalX = patchX + x;
                        int globalY = patchY + y;

                        float xCoord = (float)x / (patchWidth - 1);
                        float yCoord = (float)y / (patchHeight - 1);

                        float worldX = (globalX / (float)(width - 1)) * terrainWidth;
                        float worldY = terrainData.GetHeight(globalX, globalY);
                        float worldZ = (globalY / (float)(height - 1)) * terrainLength;

                        vertices[y * patchWidth + x] = new Vector3(worldX, worldY, worldZ);
                        Vector2 uv = new Vector2(xCoord * uvMult.x, yCoord * uvMult.y);
                        uv0[y * patchWidth + x] = rotate ? new Vector2(uv.y, uv.x) : uv;

                        if (HasUv2) uv1[y * patchWidth + x] = new Vector2(globalX / (float)(width - 1), (globalY / (float)(height - 1)));

                        if (x < patchWidth - 1 && y < patchHeight - 1)
                        {
                            int baseIndex = y * patchWidth + x;
                            int triangleIndex = (y * (patchWidth - 1) + x) * 6;

                            triangles[triangleIndex] = baseIndex;
                            triangles[triangleIndex + 1] = baseIndex + patchWidth;
                            triangles[triangleIndex + 2] = baseIndex + patchWidth + 1;

                            triangles[triangleIndex + 3] = baseIndex;
                            triangles[triangleIndex + 4] = baseIndex + patchWidth + 1;
                            triangles[triangleIndex + 5] = baseIndex + 1;
                        }
                    }
                }

                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uv0;
                if (HasUv2) mesh.uv2 = uv1;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();

                //AutoWeld(mesh, 0.005f, 5, 20);
                //RemoveInvalidTriangles(mesh);
                if(Quality<1){
                MeshSimplifier MS = new MeshSimplifier();
                MS.Initialize(mesh);
                SimplificationOptions SO = new SimplificationOptions();
                SO.PreserveBorderEdges = true;
                SO.VertexLinkDistance = 0.05f;
                SO.PreserveSurfaceCurvature = false;
                SO.PreserveUVSeamEdges = false;
                SO.MaxIterationCount = 100;
                SO.Agressiveness = 50;
                MS.SimplificationOptions = SO;
                MS.SimplifyMesh(Quality);
                mesh = MS.ToMesh();
                }
                meshes[patchIndex] = mesh;
                patchIndex++;
            }
        }

        return meshes;
    }

    public string ReadString(BinaryReader reader)
    {
        string result = "";
        char ch;
        if ((ch = reader.ReadChar()) == '\n') return result;
        result += ch;

        while ((ch = reader.ReadChar()) != '\n')
        {
            result += ch;
        }
        return result;
    }
}
