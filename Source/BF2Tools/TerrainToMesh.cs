using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityMeshSimplifier;

public class TerrainToMesh : MonoBehaviour
{
    [Range(0, 1)] public float TerrainQuality = 1;
    public int subdivs = 1;
    public Material material;
    public bool GenerateLightmapUVPerPatch;
    public Mesh[] Generatedmeshes;

    [Button("Generate")]
    void Generate()
    {
        Terrain terrain = GetComponent<Terrain>();
        Generatedmeshes = GenerateMeshesFromTerrain(terrain, terrain.terrainData.heightmapResolution / subdivs, GenerateLightmapUVPerPatch, TerrainQuality);
        int sqrt = (int)Mathf.Sqrt(Generatedmeshes.Length);
        // Create a new GameObject for each mesh and set its position
        for (int x = 0; x < sqrt; x++)
            for (int y = 0; y < sqrt; y++)
            {
                int i = y * sqrt + x;
                Mesh patchMesh = Generatedmeshes[i];

                // Create a new GameObject
                GameObject patchObject = new GameObject($"TerrainPatch_{i}");

                patchObject.transform.SetParent(terrain.transform);
                patchObject.transform.localPosition = Vector3.zero;
                MeshFilter MF = patchObject.AddComponent<MeshFilter>();
                MeshRenderer MR = patchObject.AddComponent<MeshRenderer>();
                MF.sharedMesh = patchMesh;
                MR.sharedMaterial = material;
            }
    }



    public static Mesh[] GenerateMeshesFromTerrain(Terrain terrain, int patchSize, bool HasUv2, float Quality = 1)
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

                        uv0[y * patchWidth + x] = new Vector2(globalX / (float)(width - 1), (globalY / (float)(height - 1)));
                        if (HasUv2) uv1[y * patchWidth + x] = new Vector2(xCoord, yCoord);

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
                if (Quality < 1)
                {
                    MeshSimplifier MS = new MeshSimplifier();
                    MS.Initialize(mesh);
                    SimplificationOptions SO = new SimplificationOptions();
                    SO.PreserveBorderEdges = true;
                    SO.VertexLinkDistance = 0.05f;
                    SO.PreserveSurfaceCurvature = false;
                    SO.PreserveUVSeamEdges = false;
                    SO.MaxIterationCount = 1000;
                    SO.Agressiveness = 50;
                    MS.SimplificationOptions = SO;
                    MS.SimplifyMesh(Quality);
                    mesh = MS.ToMesh();
                    MS.Initialize(mesh);
                    MS.SimplificationOptions = SO;
                    MS.SimplifyMeshLossless();
                    mesh = MS.ToMesh();
                }
                meshes[patchIndex] = mesh;
                patchIndex++;
            }
        }

        return meshes;
    }
}
