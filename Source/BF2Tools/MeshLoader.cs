using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static MeshFuncs;
using static BF2FileManager;

public class MeshLoader : MonoBehaviour
{
    public string path;
    public bf2mesh vmesh = new bf2mesh();
    public Material DefaultMat;
    public GameObject PointCloudPrefab;

    public bool Log;

    public static MeshLoader _Instance;
    public static MeshLoader Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindObjectOfType<MeshLoader>();
                if (_Instance == null)
                {
                    _Instance = new GameObject("MeshLoader").AddComponent<MeshLoader>();
                }
            }
            return _Instance;
        }
    }

    [Button("LoadMesh")]
    public void LoadTestMesh()
    {
        LoadBF2Mesh(path);
    }

    [Button("ConvertTestMeshToPointCloud")]
    public void ConvertTestMeshToUnityMesh()
    {
        GameObject GO = vmesh.ToGameObject();
    }


    public bf2mesh LoadBF2Mesh(string filename)
    {
        path = filename;
        //try{
        /*if (!FileManager.Exists(filename))
        {
            if(Log)Debug.Log($"File {filename} not found.");
            return null;
        }*/
        vmesh = new bf2mesh();

        using (BinaryReader reader = new BinaryReader(FileManager.Open(filename)))
        {
            // Reset stuff
            vmesh.filename = filename;
            vmesh.fileext = Path.GetExtension(filename).ToLower();
            vmesh.isStaticMesh = (vmesh.fileext == ".staticmesh");
            vmesh.isBundledMesh = (vmesh.fileext == ".bundledmesh");
            vmesh.isSkinnedMesh = (vmesh.fileext == ".skinnedmesh");
            vmesh.drawok = true;
            vmesh.loadok = false;

            // --- header ---
            if (Log) Debug.Log($"head start at {reader.BaseStream.Position}");
            vmesh.head.u1 = reader.ReadInt32();
            vmesh.head.version = reader.ReadInt32();
            vmesh.head.u3 = reader.ReadInt32();
            vmesh.head.u4 = reader.ReadInt32();
            vmesh.head.u5 = reader.ReadInt32();
            if (Log) Debug.Log($" u1: {vmesh.head.u1}");
            if (Log) Debug.Log($" version: {vmesh.head.version}");
            if (Log) Debug.Log($" u3: {vmesh.head.u3}");
            if (Log) Debug.Log($" u4: {vmesh.head.u4}");
            if (Log) Debug.Log($" u5: {vmesh.head.u5}");
            if (Log) Debug.Log($"head end at {reader.BaseStream.Position}");
            if (Log) Debug.Log("");

            // Unknown (1 byte) 
            vmesh.u1 = reader.ReadByte();
            if (Log) Debug.Log($"u1: {vmesh.u1}");
            if (Log) Debug.Log("");

            // For BFP4F, the value is "1", so perhaps this is a version number as well
            if (vmesh.u1 == 1)
                vmesh.isBFP4F = true;

            // --- geom table ---
            if (Log) Debug.Log($"geom table start at {reader.BaseStream.Position}");

            // geomnum (4 bytes)
            vmesh.geomnum = reader.ReadInt32();
            if (Log) Debug.Log($" geomnum: {vmesh.geomnum}");

            // geom table (4 bytes * groupnum)
            vmesh.geom = new bf2geom[vmesh.geomnum];
            for (int i = 0; i < vmesh.geomnum; i++)
            {
                // lodnum (4 bytes)
                vmesh.geom[i] = new bf2geom();
                vmesh.geom[i].lodnum = reader.ReadInt32();
                vmesh.geom[i].lod = new bf2lod[vmesh.geom[i].lodnum];
                for (int j = 0; j < vmesh.geom[i].lodnum; j++) vmesh.geom[i].lod[j] = new bf2lod();
                if (Log) Debug.Log($"  lodnum: {vmesh.geom[i].lodnum}");
            }

            if (Log) Debug.Log($"geom table end at {reader.BaseStream.Position}");
            if (Log) Debug.Log("");

            // --- vertex attribute table ---
            if (Log) Debug.Log($"attrib block at {reader.BaseStream.Position}");

            // vertattribnum (4 bytes)
            vmesh.vertattribnum = reader.ReadInt32();
            if (Log) Debug.Log($" vertattribnum: {vmesh.vertattribnum}");

            // vertex attributes
            vmesh.vertattrib = new bf2vertattrib[vmesh.vertattribnum];
            for (int i = 0; i < vmesh.vertattribnum; i++)
            {
                vmesh.vertattrib[i] = new bf2vertattrib();
                vmesh.vertattrib[i].flag = reader.ReadInt16();
                vmesh.vertattrib[i].offset = reader.ReadInt16();
                vmesh.vertattrib[i].vartype = reader.ReadInt16();
                vmesh.vertattrib[i].usage = reader.ReadInt16();

                if (Log) Debug.Log($" attrib[{i}]: {vmesh.vertattrib[i].flag} " +
                                    $"{vmesh.vertattrib[i].offset} {vmesh.vertattrib[i].vartype} " +
                                    $"{vmesh.vertattrib[i].usage}");
            }

            if (Log) Debug.Log($"attrib block end at {reader.BaseStream.Position}");
            if (Log) Debug.Log("");

            // --- vertices ---
            if (Log) Debug.Log($"vertex block start at {reader.BaseStream.Position}");

            vmesh.vertformat = reader.ReadInt32();
            vmesh.vertstride = reader.ReadInt32();
            vmesh.vertnum = reader.ReadInt32();
            if (Log) Debug.Log($" vertformat: {vmesh.vertformat}");
            if (Log) Debug.Log($" vertstride: {vmesh.vertstride}");
            if (Log) Debug.Log($" vertnum: {vmesh.vertnum}");

            vmesh.vert = new float[((vmesh.vertstride / vmesh.vertformat) * vmesh.vertnum)];
            for (int i = 0; i < vmesh.vert.Length; i++)
            {
                vmesh.vert[i] = reader.ReadSingle();
            }

            if (Log) Debug.Log($"vertex block end at {reader.BaseStream.Position}");
            if (Log) Debug.Log("");

            // --- indices ---
            if (Log) Debug.Log($"index block start at {reader.BaseStream.Position}");

            vmesh.indexnum = reader.ReadInt32();
            if (Log) Debug.Log($" indexnum: {vmesh.indexnum}");
            vmesh.index = new int[vmesh.indexnum];
            for (int i = 0; i < vmesh.index.Length; i++)
            {
                vmesh.index[i] = reader.ReadInt16();
            }

            if (Log) Debug.Log($"index block end at {reader.BaseStream.Position}");
            if (Log) Debug.Log("");

            // --- rigs ---
            if (!vmesh.isSkinnedMesh)
            {
                vmesh.u2 = reader.ReadInt32(); // always 8?
                if (Log) Debug.Log($" u2: {vmesh.u2}");
            }

            // nodes
            if (Log) Debug.Log($"nodes chunk start at {reader.BaseStream.Position}");
            for (int i = 0; i < vmesh.geomnum; i++)
            {
                if (Log) Debug.Log($" geom {i} start");
                for (int j = 0; j < vmesh.geom[i].lodnum; j++)
                {
                    if (Log) Debug.Log($"  lod {j} start");
                    ReadLodNodeTable(reader, ref vmesh.geom[i].lod[j]);
                    if (Log) Debug.Log($"  lod {j} end");
                }
                if (Log) Debug.Log($" geom {i}  end");
                if (Log) Debug.Log("");
            }
            if (Log) Debug.Log($"nodes chunk end at {reader.BaseStream.Position}");
            if (Log) Debug.Log("");

            // --- triangles ---
            if (Log) Debug.Log($"geom block start at {reader.BaseStream.Position}");

            for (int i = 0; i < vmesh.geomnum; i++)
            {
                for (int j = 0; j < vmesh.geom[i].lodnum; j++)
                {
                    if (Log) Debug.Log($" mesh {j} start at {reader.BaseStream.Position}");
                    ReadGeomLod(reader, ref vmesh.geom[i].lod[j]);
                    if (Log) Debug.Log($" mesh {j} end at {reader.BaseStream.Position}");
                }
            }

            if (Log) Debug.Log($"geom block end at {reader.BaseStream.Position}");
            if (Log) Debug.Log("");

            // --- end of file ---
            if (Log) Debug.Log($"done reading {reader.BaseStream.Position}");
            if (Log) Debug.Log($"file size is {reader.BaseStream.Length}");
            if (Log) Debug.Log("");

            vmesh.loadok = true;
            vmesh.drawok = true;
        }

        // Generate some useful stuff
        vmesh.stride = vmesh.vertstride / 4;
        vmesh.facenum = vmesh.indexnum / 3;

        // Old fallback
        vmesh.normoff = 3;
        vmesh.tangoff = ((vmesh.vertstride - 24) / 4) + 3;

        vmesh.normoff = vmesh.BF2MeshGetNormOffset();
        vmesh.tangoff = vmesh.BF2MeshGetTangOffset();
        vmesh.texcoff = vmesh.BF2MeshGetTexcOffset(0);

        vmesh.GenMeshInfo();
        vmesh.BF2ComputeTangents();

        // Reset node transforms
        /* int nodetransformnum = 40;
         for (int i = 0; i < nodetransformnum; i++)
         {
             mat4identity(ref vmesh.nodetransform[i]);
         }
        */
        /*
                    // Auto-load con
                    if (opt_loadcon)
                    {
                        if (vmesh.isBundledMesh)
                        {
                            // Make con file title (e.g., "bedford.con")
                            string conTitle = GetNameFromFileName(vmesh.filename) + ".con";

                            // Try parent folder
                            string conName = Path.Combine(Path.GetDirectoryName(vmesh.filename), "..", conTitle);
                            if (File.Exists(conName))
                            {
                                LoadCon(conName);
                            }
                            else
                            {
                                // Try current folder
                                conName = Path.Combine(Path.GetDirectoryName(vmesh.filename), conTitle);
                                if (File.Exists(conName))
                                {
                                    LoadCon(conName);
                                }
                            }
                        }
                    }
        */
        // Detect UV channels
        vmesh.uvnum = 0;
        for (int i = 0; i < vmesh.vertattribnum; i++)
        {
            if (vmesh.vertattrib[i].flag != 255)
            {
                if (vmesh.vertattrib[i].vartype == 1)
                {
                    vmesh.uvnum++;
                }
            }
        }
        return vmesh;
        /*}
        catch (Exception ex)
        {
            Debug.LogException(ex);
            //if(Log)Debug.Log($"LoadBF2Mesh\n{ex.Message}\n{ex.StackTrace}");
            //if(Log)Debug.Log($">>> error at {ex.Source}");
            // if(Log)Debug.Log($">>> filesize {reader.BaseStream.Length}");

            return new bf2mesh();
        }*/
    }


    private void ReadLodNodeTable(BinaryReader reader, ref bf2lod lod)
    {
        try
        {
            if (Log) Debug.Log($">>> {reader.BaseStream.Position}");

            // bounds (24 bytes)
            lod.min = new Vector3();
            lod.max = new Vector3();

            lod.min.x = reader.ReadSingle();
            lod.min.y = reader.ReadSingle();
            lod.min.z = reader.ReadSingle();

            lod.max.x = reader.ReadSingle();
            lod.max.y = reader.ReadSingle();
            lod.max.z = reader.ReadSingle();


            // unknown (12 bytes)
            if (vmesh.head.version <= 6) // version 4 and 6
            {
                lod.pivot = new Vector3();
                lod.pivot.x = reader.ReadSingle();
                lod.pivot.y = reader.ReadSingle();
                lod.pivot.z = reader.ReadSingle();
            }

            if (vmesh.isSkinnedMesh)
            {
                // rignum (4 bytes)
                lod.rignum = reader.ReadInt32();
                if (Log) Debug.Log($"   rignum: {lod.rignum}");

                // read rigs
                if (lod.rignum > 0)
                {
                    lod.rig = new bf2rig[lod.rignum];
                    for (int i = 0; i < lod.rignum; i++)
                    {
                        if (Log) Debug.Log($"   rig block {i} start at {reader.BaseStream.Position}");

                        // bonenum (4 bytes)
                        lod.rig[i].bonenum = reader.ReadInt32();
                        if (Log) Debug.Log($"   bonenum: {lod.rig[i].bonenum}");

                        // bones (68 bytes * bonenum)
                        if (lod.rig[i].bonenum > 0)
                        {
                            lod.rig[i].bone = new bf2bone[lod.rig[i].bonenum];
                            for (int j = 0; j < lod.rig[i].bonenum; j++)
                            {
                                // bone id (4 bytes)
                                lod.rig[i].bone[j].id = reader.ReadInt32();

                                // bone transform (64 bytes)
                                lod.rig[i].bone[j].matrix = new Matrix4x4();
                                for (int k = 0; k < 16; k++)
                                {
                                    lod.rig[i].bone[j].matrix[k] = reader.ReadSingle();
                                }

                                if (Log) Debug.Log($"    boneid[{j}]: {lod.rig[i].bone[j].id}");
                            }
                        }

                        if (Log) Debug.Log($"   rig block {i} end at {reader.BaseStream.Position}");
                    }
                }
            }
            else
            {
                // nodenum (4 bytes)
                lod.nodenum = reader.ReadInt32();
                if (Log) Debug.Log($"   nodenum: {lod.nodenum}");

                // node matrices (64 bytes * nodenum)
                if (!vmesh.isBundledMesh)
                {
                    if (Log) Debug.Log($"node data");

                    if (lod.nodenum > 0)
                    {
                        lod.node = new Matrix4x4[lod.nodenum * 16];
                        for (int i = 0; i < lod.nodenum; i++)
                        {
                            for (int k = 0; k < 16; k++)
                            {
                                lod.node[i][k] = reader.ReadSingle();
                            }

                        }
                    }
                }

                // node matrices (BFP4F variant)
                if (vmesh.isBundledMesh && vmesh.isBFP4F)
                {
                    if (Log) Debug.Log($"   node data");

                    if (lod.nodenum > 0)
                    {
                        lod.node = new Matrix4x4[lod.nodenum * 16];
                        for (int i = 0; i < lod.nodenum; i++)
                        {
                            // matrix (64 bytes)
                            for (int j = 0; j < 16; j++)
                            {
                                for (int k = 0; k < 16; k++)
                                {
                                    lod.node[i * 16 + j][k] = reader.ReadSingle();
                                }
                            }

                            // name length (4 bytes)
                            int namelen = reader.ReadInt32();

                            // name (includes zero terminator)
                            byte[] nameBytes = reader.ReadBytes(namelen);
                            string name = System.Text.Encoding.UTF8.GetString(nameBytes);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void ReadGeomLod(BinaryReader reader, ref bf2lod mesh)
    {
        try
        {
            // internal: reset polycount
            mesh.polycount = 0;

            // matnum (4 bytes)
            mesh.matnum = reader.ReadInt32();
            Debug.Log($"matnum: {mesh.matnum}");

            // materials (? bytes)
            mesh.mat = new bf2mat[mesh.matnum];
            for (int i = 0; i < mesh.matnum; i++)
            {
                mesh.mat[i] = new bf2mat();

                Debug.Log($"  mat {i} start at {reader.BaseStream.Position}");
                ReadLodMat(reader, ref mesh.mat[i]);
                Debug.Log($"  mat {i} end at {reader.BaseStream.Position}");

                // internal: increment polycount
                mesh.polycount += mesh.mat[i].inum / 3;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }


    private void ReadLodMat(BinaryReader reader, ref bf2mat mat)
    {
        try
        {
            // alpha flag (4 bytes)
            if (!vmesh.isSkinnedMesh)
            {
                mat.alphamode = reader.ReadInt32();
                Debug.Log($"   alphamode: {mat.alphamode}");
            }

            // fx filename
            mat.fxfile = ReadString(reader);
            Debug.Log($"   fxfile: {mat.fxfile}");

            // material name
            mat.technique = ReadString(reader);
            Debug.Log($"   matname: {mat.technique}");

            // mapnum (4 bytes)
            mat.mapnum = reader.ReadInt32();
            Debug.Log($"   mapnum: {mat.mapnum}");

            // maps (? bytes)
            if (mat.mapnum > 0)
            {
                mat.map = new string[mat.mapnum];

                // mapnames
                for (int i = 0; i < mat.mapnum; i++)
                {
                    mat.map[i] = ReadString(reader);
                    Debug.Log($"    {mat.map[i]}");
                }
            }

            // geometry info
            mat.vstart = reader.ReadInt32();
            mat.istart = reader.ReadInt32();
            mat.inum = reader.ReadInt32();
            mat.vnum = reader.ReadInt32();
            Debug.Log($"   vstart: {mat.vstart}");
            Debug.Log($"   istart: {mat.istart}");
            Debug.Log($"   inum: {mat.inum}");
            Debug.Log($"   vnum: {mat.vnum}");

            // unknown
            mat.u4 = reader.ReadInt32();
            mat.u5 = reader.ReadInt32();

            // note: filled garbage for BFP4F
            if (!vmesh.isSkinnedMesh)
            {
                if (vmesh.head.version == 11)
                {
                    mat.mmin = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    mat.mmax = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }
            }

            // --- internal --------------------------------------

            mat.facenum = mat.inum / 3;

            if (mat.mapnum > 0)
            {
                mat.texmapid = new int[mat.mapnum];
                mat.mapuvid = new int[mat.mapnum];
                mat.IsBumpMap = new bool[mat.mapnum];
            }

            // quick hack: needed for proper tangent computation
            if (vmesh.isBundledMesh) mat.hasAnimatedUV = mat.technique.Contains("AnimatedUV");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public static GameObject GetMeshTemplate(string name, bool tree)
    {
        GameObject MeshTemplates;
        if (tree)
        {
            if (!GameObject.Find("TreeTemplates")) MeshTemplates = new GameObject("TreeTemplates");
            else
                MeshTemplates = GameObject.Find("TreeTemplates");
            foreach (Transform T in MeshTemplates.transform) if (T.name == name) return T.gameObject;
            MeshTemplates.transform.parent = FindObjectOfType<MapLoader>().SceneRoot.transform;
        }

        if (!GameObject.Find("MeshTemplates")) MeshTemplates = new GameObject("MeshTemplates");
        else
            MeshTemplates = GameObject.Find("MeshTemplates");

        foreach (Transform T in MeshTemplates.transform)
        {
            if (T.name == name)
            {
                GameObject Template = T.gameObject;
                if (tree)
                {
                    Template = Instantiate(Template, GameObject.Find("TreeTemplates").transform);
                    Template.name = Template.name.Replace("(Clone)", "");
                }
                return Template;
            }
        }

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
            Debug.LogException(ex);
            return string.Empty;
        }
    }


    public static bool IsBumpMap(string mapfile)
    {
        return mapfile.Contains("_b.") ||
               mapfile.Contains("_n.") ||
               mapfile.Contains("_b_") ||
               mapfile.Contains("_n_") ||
               mapfile.Contains("_deb.") ||
               mapfile.Contains("_crb.") ||
               mapfile.Contains("_deb_") ||
               mapfile.Contains("_crb_");
    }



    public void BuildShader(ref bf2mat mat, string filename)
    {
        mat.layernum = 0;
        mat.shortname = "unknown";

        switch (mat.fxfile.ToLower())
        {
            case "skinnedmesh.fx":
                //mat.glslprog = skinnedmesh;
                mat.hasBump = true;
                mat.hasWreck = false;

                if (mat.mapnum > 0) mat.shortname = GetCleanMapName(mat.map[0]);

                switch (mat.technique.ToLower())
                {
                    case "alpha_test":
                        mat.layernum = 1;
                        SetAlphaTest(ref mat, 1);
                        break;
                    default:
                        mat.layernum = 1;
                        SetBase(ref mat, mat.layernum);
                        break;
                }
                break;

            case "bundledmesh.fx":
                //mat.glslprog = bundledmesh;
                mat.hasBump = false;
                mat.hasWreck = false;
                mat.hasAnimatedUV = false;
                mat.hasBumpAlpha = false;
                mat.hasEnvMap = false;
                mat.alphaTest = 0;
                mat.twosided = false;

                if (mat.mapnum > 0) mat.shortname = GetCleanMapName(mat.map[0]);

                if (mat.mapnum == 3)
                {
                    mat.hasBump = !mat.map[1].Contains("SpecularLUT");
                }
                if (mat.mapnum == 4)
                {
                    mat.hasBump = true;
                    mat.hasWreck = true;
                }
                if (mat.technique.Contains("AnimatedUV"))
                {
                    mat.hasAnimatedUV = true;
                }

                if (mat.technique.ToLower().Contains("envmap"))
                {
                    mat.hasEnvMap = true;
                    //LoadEnvMap();
                }

                if (mat.alphamode > 0 && mat.technique.ToLower() == "alpha_testcolormapgloss")
                {
                    mat.hasBumpAlpha = true;
                }
                if (mat.alphamode > 0 && mat.technique.ToLower() == "colormapglossalpha_test")
                {
                    mat.hasBumpAlpha = true;
                }

                mat.layernum = 1;
                SetBase(ref mat, 1);

                if (mat.mapnum == 3 || mat.mapnum == 4)
                {
                    mat.layer[1].depthWrite = true;

                    mat.layernum = 2;
                    mat.layer[2].texcoff = 0;
                    mat.layer[2].texmapid = mat.texmapid[2];
                    mat.layer[2].depthfunc = (int)UnityEngine.Rendering.CompareFunction.Equal;
                    mat.layer[2].depthWrite = false;
                    mat.layer[2].blend = true;
                    mat.layer[2].blendsrc = (int)UnityEngine.Rendering.BlendMode.Zero;
                    mat.layer[2].blenddst = (int)UnityEngine.Rendering.BlendMode.SrcColor;
                    mat.layer[2].lighting = false;

                    if (mat.alphamode == 1 || mat.alphamode == 2)
                    {
                        mat.layer[2].depthfunc = (int)UnityEngine.Rendering.CompareFunction.Equal;
                    }

                    if (mat.mapnum == 4)
                    {
                        mat.layer[2].texmapid = mat.texmapid[3];
                    }
                }
                break;

            case "staticmesh.fx":
                //mat.glslprog = staticmesh;
                mat.hasDetail = false;
                mat.hasDirt = false;
                mat.hasCrack = false;
                mat.hasCrackN = false;
                mat.hasDetailN = false;
                mat.hasEnvMap = false;
                mat.alphaTest = 0;
                mat.twosided = false;
                mat.hasBump = true;

                if (mat.technique.Contains("Detail"))
                {
                    mat.hasDetail = true;
                }
                if (mat.technique.Contains("Dirt"))
                {
                    mat.hasDirt = true;
                }
                if (mat.technique.Contains("Crack"))
                {
                    mat.hasCrack = true;
                }
                if (mat.technique.Contains("NCrack"))
                {
                    mat.hasCrackN = true;
                }
                if (mat.technique.Contains("NDetail"))
                {
                    mat.hasDetailN = true;
                }

                mat.shortname = mat.hasDetail && mat.mapnum > 1 ? GetCleanMapName(mat.map[1]) : GetCleanMapName(mat.map[0]);

                mat.IsVegitation = filename.Contains("vegitation", StringComparison.OrdinalIgnoreCase);

                switch (mat.technique)
                {
                    case "ColormapGloss":
                    case "EnvColormapGloss":
                        mat.layernum = 1;
                        SetBase(ref mat, 1);
                        break;
                    case "Alpha":
                        mat.layernum = 1;
                        SetAlpha(ref mat, 1);
                        break;
                    case "Alpha_Test":
                        mat.layernum = 1;
                        SetAlphaTest(ref mat, 1);
                        break;
                    case "Base":
                        if (mat.IsVegitation)
                        {
                            //mat.glslprog = leaf;
                            mat.alphaTest = 0.5f;
                            mat.twosided = true;

                            mat.layernum = 1;
                            mat.layer[1].texcoff = 0;
                            mat.layer[1].texmapid = mat.texmapid[0];
                            mat.layer[1].depthfunc = (int)UnityEngine.Rendering.CompareFunction.Less;
                            mat.layer[1].depthWrite = true;
                            mat.layer[1].alphaTest = true;
                            mat.layer[1].alpharef = 0.25f;
                            mat.layer[1].twosided = true;
                        }
                        else
                        {
                            mat.layernum = 1;
                            SetBase(ref mat, 1);
                        }
                        break;
                    case "BaseDetail":
                    case "BaseDetailNDetail":
                    case "BaseDetailNDetailenvmap":
                        if (mat.IsVegitation)
                        {
                            //mat.glslprog = trunk;

                            mat.layernum = 2;

                            mat.layer[1].texcoff = 1;
                            mat.layer[1].texmapid = mat.texmapid[1];
                            mat.layer[1].depthfunc = (int)UnityEngine.Rendering.CompareFunction.Less;
                            mat.layer[1].depthWrite = true;
                            mat.layer[1].blend = false;
                            mat.layer[1].lighting = true;

                            mat.layer[2].texcoff = 0;
                            mat.layer[2].texmapid = mat.texmapid[0];
                            mat.layer[2].depthfunc = (int)UnityEngine.Rendering.CompareFunction.Equal;
                            mat.layer[2].depthWrite = false;
                            mat.layer[2].blend = true;
                            mat.layer[2].blendsrc = (int)UnityEngine.Rendering.BlendMode.DstColor;
                            mat.layer[2].blenddst = (int)UnityEngine.Rendering.BlendMode.SrcColor;
                            mat.layer[2].lighting = false;
                        }
                        else
                        {
                            mat.layernum = 2;
                            SetBase(ref mat, 1);
                            SetDetail(ref mat, 2);
                            MakeAlpha(ref mat);
                        }
                        break;
                    case "BaseDetailCrack":
                    case "BaseDetailCrackNCrack":
                    case "BaseDetailCrackNDetail":
                    case "BaseDetailCrackNDetailNCrack":
                        mat.layernum = 3;
                        SetBase(ref mat, 1);
                        SetDetail(ref mat, 2);
                        SetCrack(ref mat, 3);

                        mat.layer[1].texcoff = 0;
                        mat.layer[2].texcoff = 1;
                        mat.layer[3].texcoff = 3;
                        mat.layer[1].texmapid = mat.texmapid[0];
                        mat.layer[2].texmapid = mat.texmapid[1];
                        mat.layer[3].texmapid = mat.texmapid[2];
                        break;
                    case "BaseDetailDirt":
                    case "BaseDetailDirtNDetail":
                        mat.layernum = 3;
                        SetBase(ref mat, 1);
                        SetDetail(ref mat, 2);
                        SetDirt(ref mat, 3);
                        break;
                }
                break;
        }
    }
    public void SetBase(ref bf2mat mat, long i)
    {
        mat.layer[i].texcoff = 0;
        mat.layer[i].texmapid = mat.texmapid[0];
        // mat.layer[i].depthfunc = GL_LESS;
        //mat.layer[i].depthWrite = GL_TRUE;
        mat.layer[i].lighting = false;
        mat.layer[i].blend = false;
        mat.layer[i].alphaTest = false;

        switch (mat.alphamode)
        {
            case 1:
                mat.layer[i].blend = true;
                //mat.layer[i].blendsrc = GL_SRC_ALPHA;
                // mat.layer[i].blenddst = GL_ONE_MINUS_SRC_ALPHA;
                //mat.layer[i].depthWrite = GL_FALSE;
                //mat.layer[i].alphaTest = true;
                //mat.layer[i].alpharef = 0.005;
                break;
            case 2:
                mat.layer[i].alphaTest = true;
                mat.layer[i].alpharef = 0.5f;
                break;
        }
    }

    public void SetAlpha(ref bf2mat mat, long i)
    {
        mat.layer[i].texcoff = 0;
        mat.layer[i].texmapid = mat.texmapid[0];
        // mat.layer[i].depthfunc = GL_LESS;
        //mat.layer[i].depthWrite = GL_TRUE;
        mat.layer[i].blend = true;
        //mat.layer[i].blendsrc = GL_SRC_ALPHA;
        //mat.layer[i].blenddst = GL_ONE_MINUS_SRC_ALPHA;
        mat.layer[i].lighting = false;
    }

    public void SetAlphaTest(ref bf2mat mat, long i)
    {
        mat.layer[i].texcoff = 0;
        mat.layer[i].texmapid = mat.texmapid[0];
        // mat.layer[i].depthfunc = GL_LESS;
        //mat.layer[i].depthWrite = GL_TRUE;
        mat.layer[i].alphaTest = true;
        mat.layer[i].alpharef = 0.5f;
        mat.layer[i].lighting = false;
    }

    public void SetDetail(ref bf2mat mat, long i)
    {
        mat.layer[i].texcoff = 1;
        mat.layer[i].texmapid = mat.texmapid[1];
        //mat.layer[i].depthfunc = GL_EQUAL;
        //mat.layer[i].depthWrite = GL_FALSE;
        mat.layer[i].blend = true;
        //mat.layer[i].blendsrc = GL_ZERO;
        // mat.layer[i].blenddst = GL_SRC_COLOR;
        mat.layer[i].lighting = false;
    }

    public void SetDirt(ref bf2mat mat, long i)
    {
        mat.layer[i].texcoff = 2;
        mat.layer[i].texmapid = mat.texmapid[2];
        //mat.layer[i].depthfunc = GL_EQUAL;
        //mat.layer[i].depthWrite = GL_FALSE;
        mat.layer[i].blend = true;
        //mat.layer[i].blendsrc = GL_ZERO;
        //mat.layer[i].blenddst = GL_SRC_COLOR;
        mat.layer[i].lighting = false;
    }

    public void SetCrack(ref bf2mat mat, long i)
    {
        mat.layer[i].texcoff = 3;
        mat.layer[i].texmapid = mat.texmapid[3];
        //mat.layer[i].depthfunc = GL_EQUAL;
        //mat.layer[i].depthWrite = GL_FALSE;
        mat.layer[i].blend = true;
        //mat.layer[i].blendsrc = GL_SRC_ALPHA;
        //mat.layer[i].blenddst = GL_ONE_MINUS_SRC_ALPHA;
        mat.layer[i].lighting = true;
    }

    public void MakeAlpha(ref bf2mat mat)
    {
        if (mat.alphamode == 2)
        {
            int tmp = mat.layer[1].texmapid;
            mat.layer[1].texmapid = mat.layer[2].texmapid;
            mat.layer[2].texmapid = tmp;

            mat.layer[1].texcoff = 1;
            mat.layer[2].texcoff = 0;

            mat.layer[1].texmapid = mat.texmapid[1];
            mat.layer[2].texmapid = mat.texmapid[0];

            //mat.layer[1].depthfunc = GL_LESS;
            //mat.layer[2].depthfunc = GL_EQUAL;

            //mat.layer[1].depthWrite = GL_TRUE;
            //mat.layer[2].depthWrite = GL_FALSE;

            mat.layer[1].blend = false;
            mat.layer[2].blend = true;
            //mat.layer[2].blendsrc = GL_ZERO;
            //mat.layer[2].blenddst = GL_SRC_COLOR;

            mat.layer[1].alphaTest = true;
            mat.layer[1].alpharef = 0.5f;
        }
    }

    public string GetCleanMapName(string fname)
    {
        string str = GetFilenameFromPath(fname);
        str = str.Replace(".dds", ".").Replace(".tga", ".").Replace("_c.", "")
                  .Replace("_de.", "").Replace("_di.", "").Replace("_cr.", "")
                  .Replace("_deb.", "").Replace("_crb.", "").Replace(".", "");
        return str;
    }

    public string GetFilenameFromPath(string str)
    {
        int s = str.LastIndexOf('\\');
        if (s == -1) s = str.LastIndexOf('/');
        return str.Substring(s + 1);
    }


}






