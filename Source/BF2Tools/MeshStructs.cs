#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using static MeshFuncs;
using static UnityEngine.MonoBehaviour;
using Random = UnityEngine.Random;
// Internal struct for approximate shading
[Serializable]
public class mat_layer
{
    // Index of texture map
    public int texmapid;

    // Texture coordinate offset
    public int texcoff;

    // Blending flag
    public bool blend;

    // Blending source factor
    public uint blendsrc;

    // Blending destination factor
    public uint blenddst;

    // Alpha testing flag
    public bool alphaTest;

    // Alpha cutoff value [0-1]
    public float alpharef;

    // Depth testing function
    public uint depthfunc;

    // Write to z-buffer flag
    public bool depthWrite;

    // Two-sided flag
    public bool twosided;

    // Fixed-function pipeline lighting flag
    public bool lighting;
}

// Internal vertex information structure
[Serializable]
public class fh2vert
{
    // Geometry index
    public byte geom;

    // LOD index
    public byte lod;

    // LOD material index
    public byte mat;

    // Selection
    public byte sel;

    // Selection mask flag
    public byte flag;

    // Set if face selected
    public byte facesel;

    // Screen space projected vertex
    public Vector3 sv;

    // Index of first vertex of this LOD sharing position
    public int sharepos;

    // Index of first vertex of this LOD sharing position+normal
    public int sharenorm;

    // Index of first vertex of this LOD sharing position+normal+tangent
    public int sharetang;

    // Element ID
    public int elem;
}

// Internal structure for edges
[Serializable]
public class fh2edge
{
    // Geometry index
    public byte geom;

    // LOD index
    public byte lod;

    // LOD material index
    public byte mat;

    // Selection
    public byte sel;

    // Selection mask
    public byte flag;

    // Vertex index 1
    public int v1;

    // Vertex index 2
    public int v2;

    // Face index 1
    public int f1;

    // Face index 2
    public int f2;

    // Element ID
    public int elem;
}

// Internal structure for faces
[Serializable]
public class fh2face
{
    // Geometry index
    public byte geom;

    // LOD index
    public byte lod;

    // LOD material index
    public byte mat;

    // Selection
    public byte sel;

    // Selection mask
    public byte flag;

    // Marked as bad face (skip when processing)
    public byte bad;

    // Vertex index 1
    public int v1;

    // Vertex index 2
    public int v2;

    // Vertex index 3
    public int v3;

    // Polygon ID
    public int poly;

    // Element ID
    public int elem;

    // Face index of neighbor on edge 1
    public int f1;

    // Face index of neighbor on edge 2
    public int f2;

    // Face index of neighbor on edge 3
    public int f3;

    // Face normal vector
    public Vector3 n;

    // Face surface area
    public float area;

    // v1 corner angle
    public float angle1;

    // v2 corner angle
    public float angle2;

    // v3 corner angle
    public float angle3;

    // Material hash (for comparing)
    public int mathash;
}

[Serializable]
// Internal structure for polygons
public class fh2poly
{
    // Selected
    public byte sel;
}

[Serializable]
// Internal structure for elements
public class fh2elem
{
    // Selected
    public byte sel;
}

// BF2 Mesh file header
[Serializable]
public class bf2head
{
    // 0
    public int u1;

    // Version (10 for most bundledmesh, 6 for some bundledmesh, 11 for staticmesh)
    public int version;

    // 0
    public int u3;

    // 0
    public int u4;

    // 0
    public int u5;
}

// BF2 Vertex attribute table entry
[Serializable]
public class bf2vertattrib
{
    // Some sort of boolean flag (if true the below field are to be ignored?)
    public int flag;

    // Offset from vertex data start
    public int offset;

    // Attribute type (vec2, vec3 etc)
    public int vartype;

    // Usage ID (vertex, texcoord etc)
    public int usage;
}

// BF2 Bone structure
[Serializable]
public class bf2bone
{
    // Bone ID
    public int id;

    // Inverse bone matrix
    public Matrix4x4 matrix;

    // World space deformed skin transform
    public Matrix4x4 skinmat;
}

// BF2 Rig structure
[Serializable]
public class bf2rig
{
    // Number of bones
    public int bonenum;

    // Array of bones
    public bf2bone[] bone;
}

// BF2 LOD material (drawcall)
[Serializable]
public class bf2mat
{
    // Alpha mode (0=opaque, 1=blend, 2=alphatest)
    public int alphamode;

    // Shader filename string
    public string fxfile = "";

    // Technique name
    public string technique = "";

    // Number of texture maps
    public int mapnum;

    // Array of texture map filenames
    public string[] map;

    // Vertex start offset
    public int vstart;

    // Index start offset
    public int istart;

    // Number of indices
    public int inum;

    // Number of vertices
    public int vnum;

    // 0
    public int u4;

    // 0
    public int u5;

    // Per-material bounds (staticmesh only)
    public Vector3 mmin;

    // Per-material bounds (staticmesh only)
    public Vector3 mmax;

    // Internal: Number of faces (inum / 3)
    public int facenum;

    // Array of texture map IDs
    public int[] texmapid;

    // Array of bump map flags
    public bool[] IsBumpMap;

    // Array of UV indices for each map
    public int[] mapuvid;

    // Number of layers
    public int layernum;

    // Array of material layers
    public mat_layer[] layer;

    // GLSL program
    public int glslprog;
    public Material mat;

    public bool IsVegitation;

    public void ApplyMatValues()
    {
        mat.SetFloat("_" + nameof(hasAlpha), hasAlpha ? 1 : 0);
        mat.SetFloat("_" + nameof(hasBump), hasBump ? 1 : 0);
        mat.SetFloat("_" + nameof(hasWreck), hasWreck ? 1 : 0);
        mat.SetFloat("_" + nameof(hasAnimatedUV), hasAnimatedUV ? 1 : 0);
        mat.SetFloat("_" + nameof(hasBumpAlpha), hasBumpAlpha ? 1 : 0);
        mat.SetFloat("_" + nameof(hasDetail), hasDetail ? 1 : 0);
        mat.SetFloat("_" + nameof(hasDirt), hasDirt ? 1 : 0);
        mat.SetFloat("_" + nameof(hasCrack), hasCrack ? 1 : 0);
        mat.SetFloat("_" + nameof(hasCrackN), hasCrackN ? 1 : 0);
        mat.SetFloat("_" + nameof(hasDetailN), hasDetailN ? 1 : 0);
        mat.SetFloat("_" + nameof(hasEnvMap), hasEnvMap ? 1 : 0);
    }
    public bool hasAlpha => alphamode > 0;
    public bool hasBump;
    public bool hasWreck;
    public bool hasAnimatedUV;
    public bool hasBumpAlpha;
    public bool hasDetail;
    public bool hasDirt;
    public bool hasCrack;
    public bool hasCrackN;
    public bool hasDetailN;
    public bool hasEnvMap;
    // Alpha test value
    public float alphaTest;
    // Two-sided flag
    public bool twosided;
    // Internal: First vertex index
    public int vertStart;

    // Internal: Last vertex index
    public int vertEnd;

    // Internal: First face index
    public int faceStart;

    // Internal: Last face index
    public int faceEnd;

    // Unique hash
    public int hash;

    // UV editor material name
    public string shortname = "";

    public bf2mat()
    {
        layer = new mat_layer[4];
        for (int i = 0; i < layer.Length; i++)
        {
            layer[i] = new mat_layer();
        }
    }
}

// BF2 LOD structure
[Serializable]
public class bf2lod
{
    // Bounds: Minimum
    public Vector3 min;

    // Bounds: Maximum
    public Vector3 max;

    // Pivot (not sure this is really a pivot, only on version<=6)
    public Vector3 pivot;

    // Number of skinning matrices (skinnedmesh only)
    public int rignum;

    // Array of rig structures (skinnedmesh only)
    public bf2rig[] rig;

    // Number of nodes (staticmesh and bundledmesh only)
    public int nodenum;

    // Array of node matrices (staticmesh and bundledmesh only)
    public Matrix4x4[] node;

    // Number of material groups
    public int matnum;

    // Array of material structures
    public bf2mat[] mat;

    // Internal: Number of triangles
    public int polycount;

    // Internal: First vertex index
    public int vertStart;

    // Internal: Last vertex index
    public int vertEnd;

    // Internal: First face index
    public int faceStart;

    // Internal: Last face index
    public int faceEnd;
}

// BF2 Geometry structure
[Serializable]
public class bf2geom
{
    // Number of LODs
    public int lodnum;

    // Array of LOD structures
    public bf2lod[] lod;
}

// BF2 BundledMesh vertex weight (helper structure, memcopy float to this)
[Serializable]
public class bf2vw
{
    // Bone 1 index
    public byte b1;

    // Bone 2 index
    public byte b2;

    // Weight for bone 1
    public byte w1;

    // Weight for bone 2
    public byte w2;
}

// BF2 SkinnedMesh vertex weight (helper structure, memcopy float to this)
[Serializable]
public class bf2skinweight
{
    // Weight
    public float w;

    // Bone 1 index
    public byte b1;

    // Bone 2 index
    public byte b2;

    // Bone 3 index
    public byte b3;

    // Bone 4 index
    public byte b4;
}

// BF2 Mesh file structure
[Serializable]
public class bf2mesh
{
    // Header
    public bf2head head = new bf2head();

    // Always 0?
    public byte u1;

    // Number of geometry structures
    public int geomnum;

    // Array of geometry structures
    public bf2geom[] geom;

    // Number of vertex attribute table entries
    public int vertattribnum;

    // Array of vertex attribute table entries
    public bf2vertattrib[] vertattrib;

    // Vertex format (always 4? e.g. GL_FLOAT)
    public int vertformat;

    // Vertex stride
    public int vertstride;

    // Number of vertices
    public int vertnum;

    // Array of vertices
    public float[] vert;

    // Number of indices
    public int indexnum;

    // Array of indices
    public int[] index;

    // Always 8?
    public int u2;

    public string name => Path.GetFileNameWithoutExtension(filename);
    // Filename of the current loaded mesh file
    public string filename = "";

    // Filename extension
    public string fileext = "";

    // True if file extension is "staticmesh"
    public bool isStaticMesh;

    // True if file extension is "skinnedmesh"
    public bool isSkinnedMesh;

    // True if file extension is "bundledmesh"
    public bool isBundledMesh;

    // True if file is inside BFP4F directory
    public bool isBFP4F;

    // True if mesh loaded properly
    public bool loadok;

    // True if mesh rendered properly
    public bool drawok;

    // Vertex stride / 4
    public int stride;

    // Number of faces / 3
    public int facenum;

    // Number of polygons
    public int polynum;

    // Number of elements
    public int elemnum;

    // Vertex array normal vector offset
    public int normoff;

    // Vertex array tangent vector offset
    public int tangoff;

    // Vertex array UV0 offset
    public int texcoff;

    // Corrected tangents
    public Vector4[] xtan;

    // Number of detected UV channels
    public int uvnum;

    // Has mesh_edit info computed
    public bool editinfo;

    // Array of vertex info & flags
    public fh2vert[] vertinfo;

    // Array of face info & flags
    public fh2face[] faceinfo;

    // Array of polygon info & flags
    public fh2poly[] polyinfo;

    // Array of element info & flags
    public fh2elem[] eleminfo;

    // Deformed vertices flag
    public bool hasSkinVerts;

    // Array of deformed vertices
    public Vector3[] skinvert;

    // Array of deformed normals
    public Vector3[] skinnorm;

    // Returns true if valid geometry indices
    public bool IsValidGeom(int g)
    {
        if (loadok)
        {
            return g >= 0 && g < geomnum;
        }
        return false;
    }

    // Returns true if valid geometry and LOD indices
    // Returns true if valid geometry and LOD indices
    public bool IsValidLod(int g, int L)
    {
        if (loadok)
        {
            if (g >= 0 && g < geomnum)
            {
                if (L >= 0 && L < geom[g].lodnum)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Returns true if valid geometry, LOD, and material indices
    public bool IsValidMat(int g, int L, int m)
    {
        if (loadok)
        {
            if (g >= 0 && g < geomnum)
            {
                if (L >= 0 && L < geom[g].lodnum)
                {
                    if (m >= 0 && m < geom[g].lod[L].matnum)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Returns vertex buffer offset for normal attribute
    public int BF2MeshGetAttribOffset(int usageID)
    {
        for (int i = 0; i < vertattribnum; i++)
        {
            if (vertattrib[i].usage == usageID)
            {
                return vertattrib[i].offset / 4;
            }
        }
        return -1;
    }

    // Returns vertex buffer offset for UV attribute
    public int BF2MeshGetTexcOffset(int uvchan)
    {
        if (uvchan == 0) return BF2MeshGetAttribOffset(5);
        if (uvchan == 1) return BF2MeshGetAttribOffset(261);
        if (uvchan == 2) return BF2MeshGetAttribOffset(517);
        if (uvchan == 3) return BF2MeshGetAttribOffset(773);
        if (uvchan == 4) return BF2MeshGetAttribOffset(1029);

        return -1;
    }

    // Returns vertex buffer offset for normal vector
    public int BF2MeshGetNormOffset()
    {
        return BF2MeshGetAttribOffset(3);
    }

    // Returns vertex buffer offset for tangent vector
    public int BF2MeshGetTangOffset()
    {
        return BF2MeshGetAttribOffset(6);
    }

    // Returns vertex buffer offset for weight/bone index attributes (start of 4 x 1-byte block)
    public int BF2MeshGetWeightOffset()
    {
        return BF2MeshGetAttribOffset(2);
    }


    public const float NORMTHRESHOLD = 0.99f;
    public const float TANGTHRESHOLD = 0.5f;

    public void GenMeshInfo()
    {
        // Internal stuff
        vertinfo = new fh2vert[vertnum];
        faceinfo = new fh2face[facenum];
        editinfo = false;

        // Generate info
        for (int g = 0; g < geomnum; g++)
        {
            var geom = this.geom[g];
            for (int l = 0; l < geom.lodnum; l++)
            {
                var lod = geom.lod[l];
                for (int m = 0; m < lod.matnum; m++)
                {
                    var mat = lod.mat[m];

                    // Set vertex range
                    mat.vertStart = mat.vstart;
                    mat.vertEnd = mat.vstart + mat.vnum - 1;

                    // Set face range
                    mat.faceStart = mat.istart / 3;
                    mat.faceEnd = (mat.istart / 3) + (mat.inum / 3) - 1;

                    // Set vertex info
                    for (int i = mat.vertStart; i <= mat.vertEnd; i++)
                    {
                        vertinfo[i] = new fh2vert
                        {
                            geom = (byte)g,
                            lod = (byte)l,
                            mat = (byte)m,
                            sel = 0,
                            flag = 0,
                            facesel = 0
                        };
                    }

                    // Set face info
                    for (int i = 0; i < mat.inum / 3; i++)
                    {
                        int f = (mat.istart / 3) + i;
                        faceinfo[f] = new fh2face
                        {
                            geom = (byte)g,
                            lod = (byte)l,
                            mat = (byte)m,
                            sel = 0,
                            flag = 0,
                            bad = 0,
                            v1 = mat.vstart + index[mat.istart + i * 3 + 0],
                            v2 = mat.vstart + index[mat.istart + i * 3 + 1],
                            v3 = mat.vstart + index[mat.istart + i * 3 + 2],
                            mathash = mat.hash
                        };
                    }
                }

                // Set vert/face range
                if (lod.matnum > 0)
                {
                    lod.vertStart = lod.mat[0].vertStart;
                    lod.faceStart = lod.mat[0].faceStart;
                    lod.vertEnd = lod.mat[lod.matnum - 1].vertEnd;
                    lod.faceEnd = lod.mat[lod.matnum - 1].faceEnd;
                }
                else
                {
                    lod.vertStart = 0;
                    lod.faceStart = 0;
                    lod.vertEnd = 0;
                    lod.faceEnd = 0;
                }
            }
        }
    }


    public void BF2ComputeTangents()
    {
        if (!loadok)
            return;

        // Allocate
        xtan = new Vector4[vertnum];

        if (true)
        {
            // TODO: Optimize, loop over all faces
            for (int g = 0; g < geomnum; g++)
            {
                for (int l = 0; l < geom[g].lodnum; l++)
                {
                    for (int m = 0; m < geom[g].lod[l].matnum; m++)
                    {
                        BF2MatGenTangents(ref geom[g].lod[l].mat[m]);
                    }
                }
            }
        }
        else
        {
            // Determine tangent W by triangle sign
            int facenum = (int)(indexnum / 3);
            for (int i = 0; i < facenum; i++)
            {
                int i1 = index[i * 3 + 0];
                int i2 = index[i * 3 + 1];
                int i3 = index[i * 3 + 2];

                Vector2 uv1 = GetTexc(1, i1);
                Vector2 uv2 = GetTexc(1, i2);
                Vector2 uv3 = GetTexc(1, i3);

                float s = TriangleSign(ref uv1, ref uv2, ref uv3);
                xtan[i1].w = s;
                xtan[i2].w = s;
                xtan[i3].w = s;
            }

            // Copy tangents
            for (int i = 0; i < vertnum; i++)
            {
                // Get tangent
                Vector3 t = new Vector3(
                    vert[i * stride + tangoff + 0],
                    vert[i * stride + tangoff + 1],
                    vert[i * stride + tangoff + 2]
                );

                // Normalize
                xtan[i].x = t.x;
                xtan[i].y = t.y;
                xtan[i].z = t.z;
            }
        }


    }

    public void BF2MatGenTangents(ref bf2mat mat)
    {
        try
        {
            // Get UV offset (staticmesh tangents are for the second UV channel)
            int off = isStaticMesh ? 9 : 7;

            // Animated UV crap
            int off2 = isBundledMesh && mat.hasAnimatedUV ? 7 + 2 : 0;

            // Temp tangent array
            var tan1 = new Vector3[vertnum];
            var tan2 = new Vector3[vertnum];

            int facenum = (int)(mat.inum / 3);

            // Compute bi-tangents
            for (int i = 0; i < facenum; i++)
            {
                int i1 = (int)(mat.vstart + index[mat.istart + i * 3 + 0]);
                int i2 = (int)(mat.vstart + index[mat.istart + i * 3 + 1]);
                int i3 = (int)(mat.vstart + index[mat.istart + i * 3 + 2]);

                Vector3 v1 = GetVert(i1);
                Vector3 v2 = GetVert(i2);
                Vector3 v3 = GetVert(i3);

                Vector2 uv1 = GetTexc(off, i1);
                Vector2 uv2 = GetTexc(off, i2);
                Vector2 uv3 = GetTexc(off, i3);

                if (mat.hasAnimatedUV)
                {
                    Vector2 tuv1 = GetTexc(off2, i1);
                    Vector2 tuv2 = GetTexc(off2, i2);
                    Vector2 tuv3 = GetTexc(off2, i3);

                    uv1.x += tuv1.x * 0.5f;
                    uv2.x += tuv2.x * 0.5f;
                    uv3.x += tuv3.x * 0.5f;

                    uv1.y += tuv1.y;
                    uv2.y += tuv2.y;
                    uv3.y += tuv3.y;
                }

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = uv2.x - uv1.x;
                float s2 = uv3.x - uv1.x;
                float t1 = uv2.y - uv1.y;
                float t2 = uv3.y - uv1.y;

                float r = (s1 * t2 - s2 * t1) == 0 ? 0 : 1 / (s1 * t2 - s2 * t1);

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] = AddFloat3(tan1[i1], sdir);
                tan1[i2] = AddFloat3(tan1[i2], sdir);
                tan1[i3] = AddFloat3(tan1[i3], sdir);

                tan2[i1] = AddFloat3(tan2[i1], tdir);
                tan2[i2] = AddFloat3(tan2[i2], tdir);
                tan2[i3] = AddFloat3(tan2[i3], tdir);
            }

            // Ortho-normalize
            for (int i = (int)mat.vstart; i < mat.vstart + mat.vnum; i++)
            {
                // Get normal
                Vector3 n = GetNorm(i);

                Vector3 t = GetTang(i);
                if (Magnitude(t) < 0.5f)
                    t = Normalize(tan1[i]);

                // Set internal tangent XYZ vector
                xtan[i].x = t.x;
                xtan[i].y = t.y;
                xtan[i].z = t.z;

                // Calculate handedness
                xtan[i].w = DotProduct(CrossProduct(n, t), tan2[i]) > 0 ? 1 : -1;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            //de.Show("BF2MatGenTangents\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Returns UV coordinate
    public Vector2 GetTexc(int off, int i)
    {
        Vector2 uv;
        uv.x = vert[i * stride + off + 0];
        uv.y = vert[i * stride + off + 1];
        return uv;
    }

    // Sets UV coordinate
    public void SetTexc(int off, int i, ref Vector2 uv)
    {
        vert[i * stride + off + 0] = uv.x;
        vert[i * stride + off + 1] = uv.y;
    }

    // Returns sign of triangle
    public float TriangleSign(ref Vector2 v1, ref Vector2 v2, ref Vector2 v3)
    {
        float result = ((v2.y - v1.y) - (v2.x - v1.x)) +
                       ((v3.y - v2.y) - (v3.x - v2.x)) +
                       ((v1.y - v3.y) - (v1.x - v3.x)) > 0 ? 1 : -1;

        return result;
    }

    // Returns transformed vertex position
    public Vector3 GetVertTF(int i)
    {
        Vector3 result;
        if (hasSkinVerts)
        {
            result.x = -skinvert[i].x; // Note: all DICE stuff is mirrored on the X axis
            result.y = skinvert[i].y;
            result.z = skinvert[i].z;
        }
        else
        {
            result.x = -vert[i * stride + 0];
            result.y = vert[i * stride + 1];
            result.z = vert[i * stride + 2];
        }

        return result;
    }

    // Returns vertex position
    public Vector3 GetVert(int i)
    {
        Vector3 result;
        result.x = vert[i * stride + 0];
        result.y = vert[i * stride + 1];
        result.z = vert[i * stride + 2];

        return result;
    }

    // Sets vertex position
    public void SetVert(int i, ref Vector3 p)
    {
        vert[i * stride + 0] = p.x;
        vert[i * stride + 1] = p.y;
        vert[i * stride + 2] = p.z;
    }

    // Returns normal vector
    public Vector3 GetNorm(int i)
    {
        Vector3 result;
        result.x = vert[i * stride + normoff + 0];
        result.y = vert[i * stride + normoff + 1];
        result.z = vert[i * stride + normoff + 2];

        return result;
    }

    // Sets normal vector
    public void SetNorm(int i, Vector3 n)
    {
        vert[i * stride + normoff + 0] = n.x;
        vert[i * stride + normoff + 1] = n.y;
        vert[i * stride + normoff + 2] = n.z;
    }

    // Returns tangent vector
    public Vector4 GetTang(long i)
    {
        Vector3 tangent = new Vector3(
            vert[i * stride + tangoff + 0],
            vert[i * stride + tangoff + 1],
            vert[i * stride + tangoff + 2]
        );

        // Assign a default value to the w-component (e.g., 1 or -1)
        float w = 1.0f; // or -1.0f if needed for your handedness

        return new Vector4(tangent.x, tangent.y, tangent.z, w);
    }

    // Sets tangent vector
    public void SetTang(int i, Vector3 t)
    {
        vert[i * stride + tangoff + 0] = t.x;
        vert[i * stride + tangoff + 1] = t.y;
        vert[i * stride + tangoff + 2] = t.z;
    }

    public static List<bf2mat> MatsCache = new List<bf2mat>();
    public GameObject ToGameObject()
    {


        GameObject MeshTemplates;
        if (!GameObject.Find("MeshTemplates")) MeshTemplates = new GameObject("MeshTemplates");
        else
            MeshTemplates = GameObject.Find("MeshTemplates");
        if (GameObject.Find(name) && GameObject.Find(name).transform.parent == MeshTemplates) return GameObject.Find(name);

        GameObject Obj = new GameObject(name);
        Obj.transform.parent = MeshTemplates.transform;

        if (!Directory.Exists("Assets/Cache/StaticMeshes/")) Directory.CreateDirectory("Assets/Cache/StaticMeshes/");
        // Loop through each geometry in the vmesh
        for (int i = 0; i < geomnum; i++)
        {

            // Destroy any existing GameObject with the same name

            // Create a new GameObject for the geometry
            GameObject Geom = new GameObject($"Geom_{i}");
            Geom.transform.parent = Obj.transform;

            LOD[] LODs = new LOD[geom[i].lodnum];
            // Loop through each level of detail (LOD) in the geometry
            float RelativeHeight = 1;
            for (int j = 0; j < geom[i].lodnum; j++)
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Cache/StaticMeshes/" + filename.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + $"LOD{j}.mesh");
                bool FoundMeshCache = mesh != null;
                //for (int k = 0; k < geom[i].lod[j].matnum; k++)
                //  MeshLoader.Instance.BuildShader(ref geom[i].lod[j].mat[k], filename);

                // Create a new GameObject for the LOD
                GameObject Lod = new GameObject($"LOD{j}");
                Lod.transform.parent = Geom.transform;

                // Loop through vertices for the current submesh
                List<Vector3> Vertices = new List<Vector3>();  // List to store vertices
                List<Vector2>[] UVs = new List<Vector2>[uvnum];  // Lists to store UVS
                for (int l = 0; l < UVs.Length; l++) UVs[l] = new List<Vector2>();

                List<Vector3> Normals = new List<Vector3>();  // List to store Normmals
                List<Vector4> Tangents = new List<Vector4>();  // List to store Tangents
                Dictionary<int, int> indexMap = new Dictionary<int, int>(); // Map global to local indices
                Debug.Log("UNNUM For " + name + " : " + uvnum);
                if (!FoundMeshCache)
                    for (int k = geom[i].lod[j].vertStart; k <= geom[i].lod[j].vertEnd; k++)
                    {
                        // Add the vertex to the Vertices list
                        Vertices.Add(GetVert(k));

                        for (int l = 0; l < UVs.Length; l++)
                        {
                            Vector2 uv = GetTexc(BF2MeshGetTexcOffset(l), k);
                            if (l != UVs.Length - 1) uv.y = 1 - uv.y;
                            UVs[l].Add(uv);
                        }

                        Normals.Add(GetNorm(k));
                        Tangents.Add(GetTang(k));

                        // Map global index to local index
                        indexMap.Add(k, Vertices.Count - 1);

                        // Instantiate a prefab at the vertex position (for visualization/debugging)
                        //Instantiate(PointCloudPrefab, Vertices[Vertices.Count - 1], Quaternion.identity, Lod.transform);



                    }



                Lod.transform.parent = Lod.transform;

                // Add MeshFilter and MeshRenderer components to the submesh GameObject
                MeshFilter MF = Lod.AddComponent<MeshFilter>();
                MeshRenderer MR = Lod.AddComponent<MeshRenderer>();
                Material[] Mats = new Material[geom[i].lod[j].matnum];

                // Create a new Mesh
                List<int> Triangles = new List<int>();         // List to store triangle indices
                if (!FoundMeshCache)
                {
                    mesh = new Mesh();
                    mesh.subMeshCount = Mats.Length;
                    mesh.SetVertices(Vertices);
                    mesh.SetNormals(Normals);
                    mesh.SetTangents(Tangents);


                    List<List<Vector2>> UVsList = UVs.ToList();
                    if (UVsList.Count > 2)
                    {
                        // Move the last item to the second index
                        List<Vector2> lastItem = UVs[^1]; // Using index from end
                        UVsList.RemoveAt(UVs.Length - 1);
                        UVsList.Insert(1, lastItem);
                    }
                    UVs = UVsList.ToArray();

                    for (int k = 0; k < UVs.Length; k++) mesh.SetUVs(k, UVs[k]);

                    AssetDatabase.CreateAsset(mesh, "Assets/Cache/StaticMeshes/" + filename.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + $"LOD{j}.mesh");
                    AssetDatabase.Refresh();

                }
                bool RecieveShadow = false;
                // Loop through each material (submesh) in the LOD
                for (int k = 0; k < geom[i].lod[j].matnum; k++)
                {

                    // Create a new GameObject for the submesh
                    //GameObject Mat = new GameObject($"SubMesh{k}");


                    //Debug.Log($"SubmeshCount{geom[i].lod[j].mat[k].vnum}");

                    // Loop through indices for the current submesh
                    if (!FoundMeshCache)
                        for (int l = geom[i].lod[j].mat[k].faceStart; l <= geom[i].lod[j].mat[k].faceEnd; l++)
                        {
                            // Get the global index
                            int globalIndex1 = faceinfo[l].v1;
                            int globalIndex2 = faceinfo[l].v2;
                            int globalIndex3 = faceinfo[l].v3;

                            // Add the remapped index to the Triangles list
                            if (indexMap.ContainsKey(globalIndex1))
                            {
                                Triangles.Add(indexMap[globalIndex1]);
                                //Debug.LogWarning($"Index {globalIndex1} Remapped");
                            }
                            else
                            {
                                //Debug.LogWarning($"Index {globalIndex1} not found in indexMap From Face {l}");
                            }

                            // Add the remapped index to the Triangles list
                            if (indexMap.ContainsKey(globalIndex2))
                            {
                                Triangles.Add(indexMap[globalIndex2]);
                                //Debug.LogWarning($"Index {globalIndex2} Remapped");
                            }
                            else
                            {
                                //Debug.LogWarning($"Index {globalIndex2} not found in indexMap");
                            }

                            // Add the remapped index to the Triangles list
                            if (indexMap.ContainsKey(globalIndex3))
                            {
                                Triangles.Add(indexMap[globalIndex3]);
                                //Debug.LogWarning($"Index {globalIndex3} Remapped");
                            }
                            else
                            {
                                //Debug.LogWarning($"Index {globalIndex3} not found in indexMap");
                            }
                        }

                    // Assign the vertices and triangles to the mesh
                    if (!FoundMeshCache) mesh.SetTriangles(Triangles, k);

                    MeshLoader.Instance.BuildShader(ref geom[i].lod[j].mat[k], filename);
                    string shaderName = "BF2/" + geom[i].lod[j].mat[k].fxfile.ToLower();
                    string technique = geom[i].lod[j].mat[k].technique;
                    if (geom[i].lod[j].mat[k].fxfile == "StaticMesh.fx")
                    {
                        shaderName += "/" + (geom[i].lod[j].mat[k].IsVegitation ? "Foilage" : geom[i].lod[j].mat[k].hasAlpha ? "Cutout" : "Opaque");
                    }
                    else
                    if (geom[i].lod[j].mat[k].fxfile == "BundledMesh.fx")
                    {
                        shaderName += "/" + (geom[i].lod[j].mat[k].IsVegitation ? "Foilage" : geom[i].lod[j].mat[k].hasAlpha ? "Cutout" : "Opaque");
                    }
                    else
                    {
                        Debug.LogError("Unknown FX : " + geom[i].lod[j].mat[k].fxfile); continue;

                    }
                    if (technique == "Alpha_One") shaderName = "BF2/Billboard";
                    Shader shader = Shader.Find(shaderName);
                    if (!shader) { Debug.LogError("Shader Not Found For : " + shaderName); continue; }

                    //string cacheKey = GenerateMatCacheKey(geom, i, j, k);
                    bool exit = false;
                    Mats[k] = null;
                    foreach (bf2mat mat in MatsCache)
                    {
                        if (SameMats(mat, geom[i].lod[j].mat[k])) { Mats[k] = mat.mat; break; }
                    }
                    RecieveShadow = !geom[i].lod[j].mat[k].IsVegitation;

                    if (Mats[k] == null)
                    {
                        Mats[k] = new Material(shader);
                        geom[i].lod[j].mat[k].mat = Mats[k];
                        geom[i].lod[j].mat[k].ApplyMatValues();

                        for (int l = 0; l < geom[i].lod[j].mat[k].mapnum; l++)
                        {
                            string TexName = null;
                            if (technique == "Base" || technique == "BaseDetail" || technique == "BaseDetailNDetail" || technique == "BaseDetailNDetailenvmap" || technique == "BaseDetailNDetailparallaxdetail")
                            {
                                TexName = l == 0 ? "_MainTex" : l == 1 ? "_Detail" : l == 2 ? "_DetailNRM" : "_EnvMap";
                            }
                            else
                            if (technique == "BaseDetailCrack" || technique == "BaseDetailCrackNCrack" || technique == "BaseDetailCrackNDetail" || technique == "BaseDetailCrackNDetailNCrack")
                            {
                                TexName = l == 0 ? "_MainTex" : l == 1 ? "_Detail" : l == 2 ? "_Crack" : l == 2 ? "_DetailNRM" : "_CrackNRM";
                            }
                            else
                            if (technique == "BaseDetailDirt" || technique == "BaseDetailDirtNDetail")
                            {
                                TexName = l == 0 ? "_MainTex" : l == 1 ? "_Detail" : l == 2 ? "_Dirt" : "_null";
                            }
                            else
                            if (technique == "Alpha_One")
                            {
                                TexName = l == 0 ? "_MainTex" : "_null";
                            }
                            else
                            if (geom[i].lod[j].mat[k].fxfile == "BundledMesh.fx")
                            {
                                TexName = l == 0 ? "_MainTex" : l == 1 ? "_BumpMap" : l == 2 ? "_Wreck" : "_null";
                            }
                            else
                            if (technique == "BaseDetailDirtCrackNDetailNCrack")
                            {
                                TexName = l == 0 ? "_MainTex" : l == 1 ? "_Detail" : l == 2 ? "_Dirt" : l == 2 ? "_Crack" : l == 3 ? "_DetailNRM" : "_CrackNRM";
                            }
                            else
                            {
                                Debug.LogError(shaderName + " technique Not Found For : " + geom[i].lod[j].mat[k].technique + " On Object " + name);
                                Mats[k] = null;
                                exit = true;
                                break;
                            }


                            Mats[k].SetTexture(TexName, DDSLoader.LoadDDSTexture(geom[i].lod[j].mat[k].map[l], true, MeshLoader.IsBumpMap(geom[i].lod[j].mat[k].map[l])));

                        }
                        if (exit) break;

                        Mats[k].name=Random.Range(int.MinValue,int.MaxValue).ToString();
                        if (!Directory.Exists("Assets/Cache/Materials/")) Directory.CreateDirectory("Assets/Cache/Materials/");
                        string SavePath = Path.Combine("Assets/Cache/Materials/",CWModUtility.GetMaterialChecksum(Mats[k])+".mat");
                        if(!File.Exists(SavePath))
                        {
                            AssetDatabase.CreateAsset(Mats[k],SavePath);
                            //AssetDatabase.Refresh();
                        }
                        else
                        Mats[k]=AssetDatabase.LoadAssetAtPath<Material>(SavePath);

                        MatsCache.Add(geom[i].lod[j].mat[k]);
                        //Debug.Log("Created A Material Cache From " + name + " With Key : " + cacheKey);
                    }
                    else
                    {
                        geom[i].lod[j].mat[k].mat = Mats[k];
                        // Debug.Log("Assinged A Material From Cache For " + name + " With Key : " + cacheKey);
                    }
                    // Assign the mesh to the MeshFilter component
                }
                //mesh.RecalculateNormals();
                MF.sharedMesh = mesh;
                MR.sharedMaterials = Mats;
                if (!RecieveShadow) MR.receiveShadows = false;
                if (geom[i].lodnum > 1)
                {
                    RelativeHeight = RelativeHeight - (RelativeHeight * 0.5f);
                    if (j == geom[i].lodnum - 1) if (RelativeHeight > 0.005f) RelativeHeight = 0.005f;
                    LODs[j] = new LOD(RelativeHeight, new Renderer[] { MR });
                }
            }
            if (geom[i].lodnum > 1)
            {
                LODGroup LG = Geom.AddComponent<LODGroup>();
                LG.SetLODs(LODs);
            }
        }
        return Obj;
    }

    bool SameMats(bf2mat m1, bf2mat m2)
    {
        bool valid = true;
        if (
            !m1.fxfile.Equals(m2.fxfile) ||
            !m1.fxfile.Equals(m2.technique) ||
            !m1.hasBump.Equals(m2.hasBump) ||
            !m1.hasWreck.Equals(m2.hasWreck) ||
            //!m1.hasAnimatedUV.Equals(m2.hasAnimatedUV) ||
            !m1.hasBumpAlpha.Equals(m2.hasBumpAlpha) ||
            !m1.hasDetail.Equals(m2.hasDetail) ||
            !m1.hasDirt.Equals(m2.hasDirt) ||
            !m1.hasCrack.Equals(m2.hasCrack) ||
            !m1.hasCrackN.Equals(m2.hasCrackN) ||
            !m1.hasDetailN.Equals(m2.hasDetailN) ||
            !m1.hasEnvMap.Equals(m2.hasEnvMap)
            ) valid = false;

        if (m1.mapnum != m2.mapnum) return false;

        for (int i = 0; i < m1.mapnum; i++)
        {
            if (!m1.map[i].ToLower().Equals(m2.map[i].ToLower())) return false;
        }

        return valid;
    }
    string GenerateMatCacheKey(bf2geom[] geom, int i, int j, int k)
    {
        var sb = new StringBuilder();
        sb.Append(geom[i].lod[j].mat[k].fxfile).Append('|');
        sb.Append(geom[i].lod[j].mat[k].technique).Append('|');

        sb.Append(geom[i].lod[j].mat[k].hasBump.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasWreck.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasAnimatedUV.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasBumpAlpha.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasDetail.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasDirt.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasCrack.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasCrackN.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasDetailN.ToString()).Append('|');
        sb.Append(geom[i].lod[j].mat[k].hasEnvMap.ToString()).Append('|');


        for (int l = 0; l < geom[i].lod[j].mat[k].mapnum; l++)
        {
            sb.Append(geom[i].lod[j].mat[k].map[l]).Append('|');
        }
        sb.Append(geom[i].lod[j].mat[k].mapnum);

        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(bytes);
        }
    }

}

#endif