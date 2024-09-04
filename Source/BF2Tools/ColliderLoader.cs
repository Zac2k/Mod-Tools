using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static MeshFuncs;
using static BF2FileManager;

[ExecuteInEditMode]
public class ColliderLoader : MonoBehaviour
{
    public string path;
    public Bf2Col cmesh = new Bf2Col();
    public Material DefaultMat;
    public GameObject PointCloudPrefab;

    public Color[] colorTable;

    void Start()
    {
        GenerateColorTable(10);
    }

    public void GenerateColorTable(int numColors)
    {
        colorTable = new Color[numColors];
        float hueStep = 1.0f / numColors;

        for (int i = 0; i < numColors; i++)
        {
            float hue = i * hueStep;
            colorTable[i] = Color.HSVToRGB(hue, 1.0f, 1.0f);
        }
    }

    public bool Log;

    public static ColliderLoader _Instance;
    public static ColliderLoader Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindObjectOfType<ColliderLoader>();
                if (_Instance == null)
                {
                    _Instance = new GameObject("ColliderLoader").AddComponent<ColliderLoader>();
                }
            }
            return _Instance;
        }
    }

    [Button("LoadCollMesh")]
    public void LoadTestCollMesh()
    {
        LoadBF2Col(path);
    }

    [Button("ConvertTestCollMeshToPointCloud")]
    public void ConvertTestCollMeshToUnityMesh()
    {
        GameObject GO = cmesh.ToGameObject();
    }



    // geom face (8 bytes)
    [Serializable]
    public struct ColFace
    {
        public short v1;
        public short v2;
        public short v3;
        public short m;
    }

    // ystruct (16 bytes)
    [Serializable]
    public struct YStruct
    {
        public float u1; // typically, matches a vertex X, Y, or Z coordinate
        public short u2;
        public short u3;
        public int u4;
        public int u5;
    }

    // geom
    [Serializable]
    public struct Bf2ColLod
    {
        // collider type
        public int coltype; // 0=projectile, 1=vehicle, 2=soldier, 3=AI

        // face data
        public int facenum;
        public ColFace[] face;

        // vertex data
        public int vertnum;
        public Vector3[] vert;

        // unknown
        public short[] vertid;

        // vertex bounds
        public Vector3 min;
        public Vector3 max;

        // unknown
        public byte u7;

        // tree bounds
        public Vector3 bmin;
        public Vector3 bmax;

        // unknown
        public int ynum;
        public YStruct[] ydata;

        // unknown
        public int znum;
        public short[] zdata;

        // unknown
        public int anum;
        public int[] adata;

        // !!!internal!!!
        public Vector3[] norm;
        public int badtri;
    }

    // sub
    [Serializable]
    public struct Bf2ColSub
    {
        public int lodnum;
        public Bf2ColLod[] lod;
    }

    // geom
    [Serializable]
    public struct Bf2ColGeom
    {
        public int subgnum;
        public Bf2ColSub[] subg; // holds variations of this geom
    }

    // collisionmesh file
    [Serializable]
    public class Bf2Col
    {
        // header
        public int u1;          // 0  ?
        public int ver;         // 8  file format version

        // geoms
        public int geomnum;     // 1  number of geoms
        public Bf2ColGeom[] geom;

        // internal
        public int maxid;
        public bool loadok;
        public bool drawok;
        public string filename;
        public string name;
        public Dictionary<short, string> Mats;

        public GameObject ToGameObject()
        {


            GameObject CollTemplates;
            if (!GameObject.Find("CollTemplates")) CollTemplates = new GameObject("CollTemplates");
            else
                CollTemplates = GameObject.Find("CollTemplates");
            if (GameObject.Find(name) && GameObject.Find(name).transform.parent == CollTemplates) return GameObject.Find(name);

            GameObject Obj = new GameObject(name);
            Obj.transform.parent = CollTemplates.transform;


            // Loop through each geometry in the vmesh
            for (int i = 0; i < geomnum; i++)
            {
                // Destroy any existing GameObject with the same name

                // Create a new GameObject for the geometry
                GameObject Geom = new GameObject($"Geom_{i}");
                Geom.transform.parent = Obj.transform;

                // Loop through each level of detail (LOD) in the geometry
                float RelativeHeight = 1;
                for (int j = 0; j < geom[i].subgnum; j++)
                {
                    // Create a new GameObject for the LOD
                    GameObject SubG = new GameObject($"SubG{j}");
                    SubG.transform.parent = Geom.transform;

                    // Loop through vertices for the current submesh
                    //List<Vector3> Vertices = new List<Vector3>();  // List to store vertices
                    //List<Vector3> Normals = new List<Vector3>();  // List to store Normmals
                    //List<Vector4> Tangents = new List<Vector4>();  // List to store Tangents
                    Dictionary<int, int> indexMap = new Dictionary<int, int>(); // Map global to local indices
                    for (int k = 0; k < geom[i].subg[j].lodnum; k++)
                    {
                        int colType = geom[i].subg[j].lod[k].coltype;
                        if(colType!=(int)MapLoader.Instance.colliderType)continue;
                        GameObject Lod = new GameObject(colType == 0 ? "Projectile" : colType == 1 ? "Vehicle" : colType == 2 ? "Soldier" : colType == 3 ? "AI" : $"LOD{j}");
                        Lod.transform.parent = SubG.transform;
                        /*for (int l = 0; l < geom[i].subg[j].lod[k].vertnum; l++)
                        {
                            //Instantiate a prefab at the vertex position (for visualization/debugging)
                            Instantiate(ColliderLoader.Instance.PointCloudPrefab, geom[i].subg[j].lod[k].vert[l], Quaternion.identity, Lod.transform);
                        }*/

                        Dictionary<short, List<int>> TrianglesDict = new Dictionary<short, List<int>>();        // List to store triangle indices
                        for (int l = 0; l < geom[i].subg[j].lod[k].facenum; l++)
                        {
                            List<int> Triangles;
                            if (!TrianglesDict.TryGetValue(geom[i].subg[j].lod[k].face[l].m, out Triangles))
                            {
                                Triangles = new List<int>();
                                TrianglesDict.Add(geom[i].subg[j].lod[k].face[l].m, Triangles);
                            }
                            Triangles.Add(geom[i].subg[j].lod[k].face[l].v3);
                            Triangles.Add(geom[i].subg[j].lod[k].face[l].v2);
                            Triangles.Add(geom[i].subg[j].lod[k].face[l].v1);
                        }

                        foreach (short matID in TrianglesDict.Keys)
                        {
                            string MatName = "";
                            if (Mats != null) Mats.TryGetValue(matID, out MatName);
                            if (string.IsNullOrEmpty(MatName)) { MatName = $"Material : {matID}"; }

                            GameObject Mat = new GameObject(MatName);
                            //Mat.tag=MatName;
                            Mat.transform.parent = Lod.transform;

                            //MeshFilter MF = Mat.AddComponent<MeshFilter>();
                            //MeshRenderer MR = Mat.AddComponent<MeshRenderer>();
                            MeshCollider MC = Mat.AddComponent<MeshCollider>();

                            Mesh mesh = new Mesh();
                            mesh.SetVertices(geom[i].subg[j].lod[k].vert);
                            mesh.SetTriangles(TrianglesDict[matID], 0);
                            //if(TrianglesDict[matID].Count>5)mesh.RecalculateNormals(180);else mesh.RecalculateNormals();
                            mesh.Optimize();
                            MC.sharedMesh = mesh;
                            //MF.sharedMesh = mesh;
                            //MR.sharedMaterial = new Material(Shader.Find("SuperSystems/Wireframe-Transparent-Culled"));
                            //if(Log)Debug.Log("MatID : "+matID);
                            //if (ColliderLoader.Instance.colorTable.Length <= matID) ColliderLoader.Instance.GenerateColorTable(matID + 1);
                            //MR.sharedMaterial.SetColor("_WireColor", ColliderLoader.Instance.colorTable[matID]);
                        }
                        //mesh.SetNormals(Normals);
                        //mesh.SetTangents(Tangents);
                    }


                }
            }
            return Obj;
        }
    }


    // loads collisionmesh from file
    public Bf2Col LoadBF2Col(string filename)
    {
        try
        {
            // open file
            using (BinaryReader reader = new BinaryReader(FileManager.Open(filename)))
            {
                cmesh.loadok = false;
                cmesh.drawok = true;
                cmesh.filename = filename;
                cmesh.name = Path.GetFileNameWithoutExtension(filename);

                // --- header ---------------------------------------------------------

                // unknown (4 bytes)
                cmesh.u1 = reader.ReadInt32();
                if (Log) Debug.Log("u1: " + cmesh.u1);

                // version (4 bytes)
                cmesh.ver = reader.ReadInt32();
                if (Log) Debug.Log("version: " + cmesh.ver);

                // version warning
                switch (cmesh.ver)
                {
                    case 8:
                    case 9:
                    case 10:
                        break;
                    default:
                        if (Log) Debug.LogWarning("File type not tested, may crash!");
                        break;
                }

                // geomnum (4 bytes)
                cmesh.geomnum = reader.ReadInt32();
                if (Log) Debug.Log("geomnum: " + cmesh.geomnum);
                if (Log) Debug.Log("");

                // loop through geoms
                if (cmesh.geomnum > 0)
                {
                    cmesh.geom = new Bf2ColGeom[cmesh.geomnum];
                    for (int i = 0; i < cmesh.geomnum; i++)
                    {
                        if (Log) Debug.Log("geom " + i + " start at " + reader.BaseStream.Position);
                        BF2ReadColGeom(reader, ref cmesh.geom[i]);
                        if (Log) Debug.Log("geom " + i + " end at " + reader.BaseStream.Position);
                        if (Log) Debug.Log("");
                    }
                }

                // --- end of file ------------------------------------------------------------------

                if (Log) Debug.Log("done reading " + reader.BaseStream.Position);
                if (Log) Debug.Log("file size is " + reader.BaseStream.Length);
                if (Log) Debug.Log("");

                if (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    if (Log) Debug.LogWarning("File not loaded properly!");
                }

                // determine max material id
                cmesh.maxid = 0;
                for (int i = 0; i < cmesh.geomnum; i++)
                {
                    for (int j = 0; j < cmesh.geom[i].subgnum; j++)
                    {
                        for (int k = 0; k < cmesh.geom[i].subg[j].lodnum; k++)
                        {
                            for (int f = 0; f < cmesh.geom[i].subg[j].lod[k].facenum; f++)
                            {
                                if (cmesh.geom[i].subg[j].lod[k].face[f].m > cmesh.maxid)
                                {
                                    cmesh.maxid = cmesh.geom[i].subg[j].lod[k].face[f].m;
                                }
                            }
                        }
                    }
                }

                // internal
                cmesh.loadok = true;
                cmesh.drawok = true;
            }

            GenColNormals();
            CheckBF2ColMesh();

            // success
            return cmesh;
        }
        catch (Exception ex)
        {
            if (Log) Debug.LogException(ex);
            GenColNormals();
            return null;
        }
    }

    // reads geom
    public void BF2ReadColGeom(BinaryReader reader, ref Bf2ColGeom geom)
    {
        geom.subgnum = reader.ReadInt32();
        if (Log) Debug.Log(" subnum: " + geom.subgnum);

        // read subs
        if (geom.subgnum > 0)
        {
            if (Log) Debug.Log("");
            geom.subg = new Bf2ColSub[geom.subgnum];
            for (int i = 0; i < geom.subgnum; i++)
            {
                if (Log) Debug.Log(" sub " + i + " start at " + reader.BaseStream.Position);
                BF2ReadColSub(reader, ref geom.subg[i]);
                if (Log) Debug.Log(" sub " + i + " end at " + reader.BaseStream.Position);
                if (Log) Debug.Log("");
            }
        }
    }

    // reads sub
    public void BF2ReadColSub(BinaryReader reader, ref Bf2ColSub subg)
    {
        subg.lodnum = reader.ReadInt32();
        if (Log) Debug.Log(" lodnum: " + subg.lodnum);

        // read geoms
        if (subg.lodnum > 0)
        {
            if (Log) Debug.Log("");
            subg.lod = new Bf2ColLod[subg.lodnum];
            for (int i = 0; i < subg.lodnum; i++)
            {
                if (Log) Debug.Log(" lod " + i + " start at " + reader.BaseStream.Position);
                BF2ReadColLod(reader, ref subg.lod[i]);
                if (Log) Debug.Log(" lod " + i + " end at " + reader.BaseStream.Position);
                if (Log) Debug.Log("");
            }
        }
    }

    // read collider geom block
    public void BF2ReadColLod(BinaryReader reader, ref Bf2ColLod lod)
    {
        if (cmesh.ver >= 9)
        {
            lod.coltype = reader.ReadInt32();
            if (Log) Debug.Log("  id: " + lod.coltype);
        }

        // --- faces ---
        lod.facenum = reader.ReadInt32();
        if (Log) Debug.Log("  facenum: " + lod.facenum);
        if (lod.facenum > 0)
        {
            lod.face = new ColFace[lod.facenum];
            for (int i = 0; i < lod.facenum; i++)
            {
                lod.face[i].v1 = reader.ReadInt16();
                lod.face[i].v2 = reader.ReadInt16();
                lod.face[i].v3 = reader.ReadInt16();
                lod.face[i].m = reader.ReadInt16();
            }
        }

        // --- vertices ---
        lod.vertnum = reader.ReadInt32();
        if (Log) Debug.Log("  vertnum: " + lod.vertnum);
        if (lod.vertnum > 0)
        {
            lod.vert = new Vector3[lod.vertnum];
            for (int i = 0; i < lod.vertnum; i++)
            {
                lod.vert[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }

        // vertid (2 bytes * vertnum)
        if (lod.vertnum > 0)
        {
            lod.vertid = new short[lod.vertnum];
            for (int i = 0; i < lod.vertnum; i++)
            {
                lod.vertid[i] = reader.ReadInt16();
            }
        }

        // --- bounds ---
        lod.min = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        lod.max = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        // unknown (1 byte)
        lod.u7 = reader.ReadByte();
        if (Log) Debug.Log("  u7: " + lod.u7);

        // bounds (24 bytes)
        lod.bmin = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        lod.bmax = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        if (Log) Debug.Log("  unknown stuff start at " + reader.BaseStream.Position);

        // --- y block ---
        lod.ynum = reader.ReadInt32();
        if (Log) Debug.Log("   ynum: " + lod.ynum);
        if (lod.ynum > 0)
        {
            if (Log) Debug.Log("   ydata start at " + reader.BaseStream.Position);
            lod.ydata = new YStruct[lod.ynum];
            for (int i = 0; i < lod.ynum; i++)
            {
                lod.ydata[i].u1 = reader.ReadSingle();
                lod.ydata[i].u2 = reader.ReadInt16();
                lod.ydata[i].u3 = reader.ReadInt16();
                lod.ydata[i].u4 = reader.ReadInt32();
                lod.ydata[i].u5 = reader.ReadInt32();
            }
            if (Log) Debug.Log("   ydata end at " + reader.BaseStream.Position);
        }

        // --- z block ---
        lod.znum = reader.ReadInt32();
        if (Log) Debug.Log("   znum: " + lod.znum);
        if (lod.znum > 0)
        {
            if (Log) Debug.Log("   zdata start at " + reader.BaseStream.Position);
            lod.zdata = new short[lod.znum];
            for (int i = 0; i < lod.znum; i++)
            {
                lod.zdata[i] = reader.ReadInt16();
            }
            if (Log) Debug.Log("   zdata end at " + reader.BaseStream.Position);
        }

        // --- a block ---
        if (cmesh.ver >= 10)
        {
            lod.anum = reader.ReadInt32();
            if (Log) Debug.Log("   anum: " + lod.anum);
            if (lod.anum > 0)
            {
                if (Log) Debug.Log("   adata start at " + reader.BaseStream.Position);
                lod.adata = new int[lod.anum];
                for (int i = 0; i < lod.anum; i++)
                {
                    lod.adata[i] = reader.ReadInt32();
                }
                if (Log) Debug.Log("   adata end at " + reader.BaseStream.Position);
            }
        }

        if (Log) Debug.Log("  unknown stuff end at " + reader.BaseStream.Position);
    }

    // counts number of degenerate triangles
    public void CheckBF2ColLod(ref Bf2ColLod lod)
    {
        lod.badtri = 0;

        for (int i = 0; i < lod.facenum; i++)
        {
            bool err = false;

            Vector3 v1 = lod.vert[lod.face[i].v1];
            Vector3 v2 = lod.vert[lod.face[i].v2];
            Vector3 v3 = lod.vert[lod.face[i].v3];

            float a1 = Vector3.Angle(v1 - v2, v1 - v3);
            float a2 = Vector3.Angle(v2 - v3, v2 - v1);
            float a3 = Vector3.Angle(v3 - v1, v3 - v2);

            // DEGENERATEFACEANGLE
            const float badangle = 0.1f;

            if (a1 < badangle) err = true;
            if (a2 < badangle) err = true;
            if (a3 < badangle) err = true;

            if (err)
            {
                lod.badtri++;
            }
        }
    }

    // checks for errors
    public void CheckBF2ColMesh()
    {
        if (!cmesh.loadok) return;

        for (int i = 0; i < cmesh.geomnum; i++)
        {
            for (int j = 0; j < cmesh.geom[i].subgnum; j++)
            {
                for (int k = 0; k < cmesh.geom[i].subg[j].lodnum; k++)
                {
                    CheckBF2ColLod(ref cmesh.geom[i].subg[j].lod[k]);
                }
            }
        }
    }

    public void GenColNormals()
    {
        // Add implementation for generating collision normals if needed.
    }

    public string ReadString(BinaryReader reader)
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
            if (Log) if (Log) Debug.LogException(ex);
            return string.Empty;
        }
    }

    public static GameObject GetCollTemplate(string name)
    {
        GameObject CollTemplates;
        if (!GameObject.Find("CollTemplates")) CollTemplates = new GameObject("CollTemplates");
        else
            CollTemplates = GameObject.Find("CollTemplates");

        foreach (Transform T in CollTemplates.transform) if (T.name == name) return T.gameObject;


        return null;
    }

    public string SafeString(byte[] chars, int length)
    {
        try
        {
            // Assuming the characters are encoded in UTF-8
            return Encoding.UTF8.GetString(chars, 0, length);
        }
        catch (Exception ex)
        {
            if (Log) if (Log) Debug.LogException(ex);
            return string.Empty;
        }
    }


}






