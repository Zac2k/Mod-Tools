using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static MeshFuncs;
using static CWModUtility;
using static BF2FileManager;
using UnityEditor;

public class RoadLoader : MonoBehaviour
{
    public string path;
    public bf2road Troad = new bf2road();
    public Material DefaultMat;
    public GameObject PointCloudPrefab;

    public bool Log;
    public bool SpawnVertexPoints;

    public Vector3 Offset = new Vector3(0, 0.035f, 0);

    public static RoadLoader _Instance;
    public static RoadLoader Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindObjectOfType<RoadLoader>();
                if (_Instance == null)
                {
                    _Instance = new GameObject("MeshLoader").AddComponent<RoadLoader>();
                }
            }
            return _Instance;
        }
    }

    [Button("LoadRoad")]
    public void LoadTestRoad()
    {
        LoadBF2Road(path);
    }

    public GameObject RoadUVPointsParent;
    public GameObject RoadPointsParent;
    public GameObject RoadUV2PointsParent;
    public void addRoadPoint(Vector3 V3, Transform Parent, string name = null, bool local = false)
    {
        GameObject Point = Instantiate(PointCloudPrefab, Parent.transform);
        Point.transform.parent = Parent;
        if (!local) Point.transform.position = V3;
        else
            Point.transform.localPosition = V3;
        if (name != null) Point.name = name;
        if (Log) Debug.Log($"DebugPoint Added To {Parent.name}: " + V3);

    }

    [Button("ConvertTestRoadToPointCloud")]
    public void ConvertTestRoadToUnityMesh()
    {
        // GameObject GO = Troad.ToGameObject();
    }

    [System.Serializable]
    public class Spline
    {
        public int UnknownInt0;
        public int UnknownInt1;
        public float UnknownFloat0;
        public Vector3 Position;

    }

    [System.Serializable]
    public class bf2road
    {
        public string filename = "";
        public string name = "";
        public short H1;
        public short H2;

        public float UnknownFloat0;
        public Vector3 UnknownVector0;
        public Vector3 UnknownVector1;
        public int UnknownInt0;
        public Vector3 Position;
        public Vector3[] vertices;
        public Vector2[] Uvs0;
        public Vector2[] Uvs2;
        public Color32[] alphas;

        public Vector3[] UknownVectors1;
        public Vector3[] UknownVectors2;

        public Spline[] Splines;
        public byte UnknownByte0;
        public short UnknownShort0;
        public byte UnknownByte1;
        public int UnknownInt1;
        public float UnknownFloat2;


        public int[] indices;

        public Mesh ToMesh()
        {
            if (!Directory.Exists("Assets/Cache/RoadMeshes/")) Directory.CreateDirectory("Assets/Cache/RoadMeshes/");
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Cache/StaticMeshes/" + filename.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".mesh");
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.uv = Uvs0;
                mesh.uv2 = Uvs2;
                mesh.colors32 = alphas;
                mesh.triangles = indices;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.Optimize();
                AssetDatabase.CreateAsset(mesh, "Assets/Cache/RoadMeshes/" + filename.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".mesh");
                AssetDatabase.Refresh();
            }
            return mesh;
        }

        public GameObject ToGameObject()
        {


            GameObject Roads;
            if (!GameObject.Find("RoadTemplates")) Roads = new GameObject("RoadTemplates");
            else
                Roads = GameObject.Find("RoadTemplates");
            if (GameObject.Find(name) && GameObject.Find(name).transform.parent == Roads) return GameObject.Find(name);

            GameObject Road = new GameObject(name);
            Road.transform.parent = Roads.transform;
            Road.transform.position = Position;

            // Destroy any existing GameObject with the same name

            // Create a new GameObject for the geometry


            MeshFilter MF = Road.AddComponent<MeshFilter>();
            MeshRenderer MR = Road.AddComponent<MeshRenderer>();
            MF.sharedMesh = ToMesh();
            MR.sharedMaterial = new Material(Shader.Find("Standard"));
            if (Instance.SpawnVertexPoints)
            {
                Instance.addRoadPoint(UnknownVector0, Road.transform, "UP1");
                Instance.addRoadPoint(UnknownVector1, Road.transform, "UP2");
                if (H1 == 4)
                    for (int i = 0; i < Splines.Length; i++)
                    {
                        Instance.addRoadPoint(Splines[i].Position, Road.transform, $"Spline{i}_Pos");

                    }
            }
            MR.sharedMaterial.SetVector("_FadePosition", new Vector4(Splines[0].Position.x, Splines[0].Position.y, Splines[0].Position.z));
            return Road;
        }

    }

    public int vertexLoadCount = 5;
    public Mesh Testmesh;
    public bf2road LoadBF2Road(string filename)
    {
        path = filename;
        /*if (!FileManager.Exists(filename))
        {
            if(Log)Debug.Log($"File {filename} not found.");
            return null;
        }*/
        if (SpawnVertexPoints)
        {

            if (!GameObject.Find("RoadUVPointsParent")) RoadUVPointsParent = new GameObject("RoadUVPointsParent"); else RoadPointsParent = GameObject.Find("RoadUVPointsParent");
            if (!GameObject.Find("RoadPointsParent")) RoadPointsParent = new GameObject("RoadPointsParent"); else RoadPointsParent = GameObject.Find("RoadPointsParent");
            if (!GameObject.Find("RoadUV2PointsParent")) RoadUV2PointsParent = new GameObject("RoadUV2PointsParent"); else RoadUV2PointsParent = GameObject.Find("RoadUV2PointsParent");

            if (!RoadPointsParent.GetComponent<MeshFilter>())
            {
                RoadPointsParent.AddComponent<MeshFilter>();
                MeshRenderer MR = RoadPointsParent.AddComponent<MeshRenderer>();
                MR.sharedMaterial = DefaultMat;
            }
            ClearAllChildrenInEditMode(RoadUVPointsParent.transform);
            ClearAllChildrenInEditMode(RoadPointsParent.transform);
            ClearAllChildrenInEditMode(RoadUV2PointsParent.transform);
        }
        using (BinaryReader reader = new BinaryReader(FileManager.Open(filename)))
        {
            // Reset stuff
            Troad = new bf2road();
            Troad.filename = filename;
            Troad.name = Path.GetFileNameWithoutExtension(filename);

            // --- header ---
            Troad.H1 = reader.ReadInt16();
            Troad.H2 = reader.ReadInt16();

            if (Log) Debug.Log("H1  " + Troad.H1);
            if (Log) Debug.Log("H2  " + Troad.H2);

            Troad.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            if (Log) Debug.Log("Position : " + Troad.Position);
            if (SpawnVertexPoints)
            {
                RoadPointsParent.transform.position = Troad.Position;
                RoadUVPointsParent.transform.position = Troad.Position;
                RoadUV2PointsParent.transform.position = Troad.Position;
            }
            Troad.UnknownFloat0 = reader.ReadSingle();
            if (Log) Debug.Log("UnknownSingle : " + Troad.UnknownFloat0);
            Troad.UnknownVector0 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            if (Log) Debug.Log("UnknownVector0 : " + Troad.UnknownVector0);

            Troad.UnknownVector1 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            if (Log) Debug.Log("UnknownVector1 : " + Troad.UnknownVector1);

            Troad.UnknownInt0 = reader.ReadInt32();
            if (Log) Debug.Log("UInt : " + Troad.UnknownInt0);

            if (Log) Debug.Log("vertex Stuff Starts At " + reader.BaseStream.Position);
            int VertexCount = reader.ReadInt32();
            Troad.vertices = new Vector3[VertexCount];
            Troad.Uvs0 = new Vector2[VertexCount];
            Troad.Uvs2 = new Vector2[VertexCount];
            Troad.alphas = new Color32[VertexCount];
            if (Troad.H1 == 2)
            {
                // unknown vectors, probably normals or tangents or UV3 IDGAF!!
                Troad.UknownVectors1 = new Vector3[VertexCount];
                Troad.UknownVectors2 = new Vector3[VertexCount];
            }

            if (Log) Debug.Log("Road has  " + VertexCount + " Vertices");
            // VCount - 923
            // A - 29588
            // AL - 29450

            // EA - 51740
            // EAL - 51692
            for (int i = 0; i < VertexCount; i++)
            {
                Troad.vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()) + Offset;
                Troad.Uvs0[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                Troad.Uvs2[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                Troad.alphas[i].a = (byte)(reader.ReadSingle() * 255);

                if (Troad.H1 == 2)
                {
                    Troad.UknownVectors1[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    Troad.UknownVectors2[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }
                if (SpawnVertexPoints)
                {
                    addRoadPoint(Troad.Position + Troad.vertices[i], RoadPointsParent.transform);
                    addRoadPoint(Troad.Position + new Vector3(Troad.Uvs0[i].x, 0, Troad.Uvs0[i].y), RoadUVPointsParent.transform);
                    addRoadPoint(Troad.Position + new Vector3(Troad.Uvs2[i].x, 0, Troad.Uvs2[i].y), RoadUV2PointsParent.transform);
                }
            }
            if (Log) Debug.Log("vertex Stuff Ends At " + reader.BaseStream.Position);

            if (Log) Debug.Log("index Stuff Starts At " + reader.BaseStream.Position);
            int IndexCount = reader.ReadInt32();
            if (Log) Debug.Log("Road has  " + IndexCount + " Indices");
            //return Troad;
            Troad.indices = new int[IndexCount];
            for (int i = 0; i < IndexCount; i++)
            {
                Troad.indices[i] = reader.ReadInt16();
            }
            if (Log) Debug.Log("index Stuff Ends At " + reader.BaseStream.Position);
            //Testmesh = Troad.ToMesh();
            if (Troad.H1 == 4)
            {
                Troad.Splines = new Spline[reader.ReadInt32()];
                if (Log) Debug.Log("Road has  " + Troad.Splines.Length + " Bones");

                for (int i = 0; i < Troad.Splines.Length; i++)
                {
                    Troad.Splines[i] = new Spline();
                    Troad.Splines[i].UnknownInt0 = reader.ReadInt32();
                    Troad.Splines[i].UnknownInt1 = reader.ReadInt32();
                    Troad.Splines[i].UnknownFloat0 = reader.ReadSingle();
                    Troad.Splines[i].Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }
            }
            if (Log) Debug.Log("Reader Finished At " + reader.BaseStream.Position);
            RoadPointsParent = Troad.ToGameObject();

        }



        return Troad;
    }


    public static GameObject GetRoadTemplate(string name)
    {
        GameObject Roads;
        if (!GameObject.Find("RoadTemplates")) Roads = new GameObject("RoadTemplates");
        else
            Roads = GameObject.Find("RoadTemplates");

        foreach (Transform T in Roads.transform) if (T.name == name) return T.gameObject;

        return null;
    }


    private string ReadString(BinaryReader reader)
    {
        try
        {
            int num = reader.ReadInt32();

            if (num == 0)
            {
                return string.Empty;
            }

            byte[] chars = reader.ReadBytes(num);

            return SafeString(chars, num);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return string.Empty;
        }
    }

    private string SafeString(byte[] chars, int length)
    {
        try
        {
            // Assuming the characters are encoded in UTF-8
            return Encoding.UTF8.GetString(chars, 0, length);
        }
        catch (Exception ex)
        {
            if (Log) Debug.LogException(ex);
            return string.Empty;
        }
    }


}






