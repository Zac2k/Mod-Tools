using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class ObstacleColliderGenerator : MonoBehaviour
{
    public Mesh MainMesh;
    public Mesh GridMesh;
    public float GridSpacing = 3; // Spacing between vertices
    public NavMeshSurface Surface;

    [Button("Generate Main Mesh")]
    public void GenerateMainMesh()
    {
        // Generate NavMesh triangulation data
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        // Create a new mesh from the NavMesh data
        MainMesh = new Mesh();
        MainMesh.vertices = navMeshData.vertices;
        MainMesh.triangles = navMeshData.indices;
        MainMesh.RecalculateBounds();
        // Create a new GameObject as a child of the current object
        GameObject childObject = new GameObject("GeneratedMesh");
        childObject.transform.parent = this.transform;

        // Add MeshFilter and MeshRenderer components to the child object
        MeshFilter meshFilter = childObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = childObject.AddComponent<MeshRenderer>();

        // Assign the generated mesh to the MeshFilter
        meshFilter.mesh = MainMesh;

        // Optionally, assign a material to the MeshRenderer (if you want to visualize the mesh)
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }

    [Button("Generate Grid Mesh")]
    public void GenerateGridMesh()
    {
        if (MainMesh == null)
        {
            Debug.LogError("MainMesh is not assigned.");
            return;
        }

        // Get the bounding box of the MainMesh
        Bounds bounds = MainMesh.bounds;
        float width = bounds.size.x;
        float height = bounds.size.z;

        // Calculate the number of vertices based on the grid spacing
        int numVerticesX = Mathf.CeilToInt(width / GridSpacing) + 1;
        int numVerticesZ = Mathf.CeilToInt(height / GridSpacing) + 1;

        // Create a new grid mesh
        GridMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Generate vertices
        for (int z = 0; z < numVerticesZ; z++)
        {
            for (int x = 0; x < numVerticesX; x++)
            {
                vertices.Add(new Vector3(x * GridSpacing, 0, z * GridSpacing));
            }
        }

        // Generate triangles
        for (int z = 0; z < numVerticesZ - 1; z++)
        {
            for (int x = 0; x < numVerticesX - 1; x++)
            {
                int startIndex = z * numVerticesX + x;
                triangles.Add(startIndex);
                triangles.Add(startIndex + numVerticesX);
                triangles.Add(startIndex + 1);

                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + numVerticesX);
                triangles.Add(startIndex + numVerticesX + 1);
            }
        }

        // Assign vertices and triangles to the mesh
        GridMesh.vertices = vertices.ToArray();
        GridMesh.triangles = triangles.ToArray();

        // Create a new GameObject as a child of the current object
        GameObject childObject = new GameObject("GeneratedGridMesh");
        childObject.transform.parent = this.transform;

        // Add MeshFilter and MeshRenderer components to the child object
        MeshFilter meshFilter = childObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = childObject.AddComponent<MeshRenderer>();

        // Assign the generated mesh to the MeshFilter
        meshFilter.mesh = GridMesh;

        // Optionally, assign a material to the MeshRenderer (if you want to visualize the mesh)
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}
