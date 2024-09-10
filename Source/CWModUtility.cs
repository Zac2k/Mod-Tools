using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine.Networking;
using System.Drawing;
using System.Drawing.Imaging;
using Graphics = UnityEngine.Graphics;
using UnityEditor;
using Color = UnityEngine.Color;
using UnityMeshSimplifier;
using MessagePack;
using System.IO.Compression;
using Random = UnityEngine.Random;
using System.Reflection;

public static class CWModUtility
{
    public static string[] Letters = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", };

    /// <summary>
    /// Checks if a target position is visible to the Eye based on look constraints.
    /// </summary>
    /// <param name="Eye">The Eye's transform.</param>
    /// <param name="targetPosition">The target position in world space.</param>
    /// <param name="viewDistance">Maximum view distance.</param>
    /// <param name="minConstraints">Minimum constraint angles as a Vector3 (horizontal min, vertical min, not used).</param>
    /// <param name="maxConstraints">Maximum constraint angles as a Vector3 (horizontal max, vertical max, not used).</param>
    /// <returns>True if the target position is visible to the Eye; otherwise, false.</returns>
    public static bool IsObjectVisible(Transform Eye, Vector3 targetPosition, float viewDistance, Vector3 minConstraints, Vector3 maxConstraints)
    {
        // Direction from the Eye to the target position
        Vector3 directionToTarget = targetPosition - Eye.position;
        float distanceToTarget = directionToTarget.sqrMagnitude;

        // Check if the target position is within view distance
        if (distanceToTarget > viewDistance * viewDistance)
            return false;

        // Normalize direction to target
        Vector3 directionToTargetNormalized = directionToTarget.normalized;

        // Horizontal FOV Check (X-Z plane)
        float horizontalAngle = Vector3.SignedAngle(Eye.forward, new Vector3(directionToTarget.x, 0, directionToTarget.z), Vector3.up);
        if (horizontalAngle < minConstraints.x || horizontalAngle > maxConstraints.x)
            return false;

        // Vertical FOV Check (Y axis)
        float verticalAngle = Vector3.SignedAngle(Eye.forward, directionToTarget, Eye.right);
        if (verticalAngle < minConstraints.y || verticalAngle > maxConstraints.y)
            return false;

        // Perform a raycast to check if there are any obstacles blocking the view
        RaycastHit hit;
        if (Physics.Raycast(Eye.position, directionToTargetNormalized, out hit, viewDistance))
        {
            // Check if the raycast hit the target position
            if (hit.point == targetPosition || Vector3.Distance(hit.point, targetPosition) < 0.1f)
            {
                // The target position is visible
                return true;
            }
        }

        // The target position is not visible
        return false;
    }


    static SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();

    public static byte[] Compress(byte[] toCompress)
    {
        using (MemoryStream input = new MemoryStream(toCompress))
        using (MemoryStream output = new MemoryStream())
        {
            coder.WriteCoderProperties(output);

            for (int i = 0; i < 8; i++)
            {
                output.WriteByte((byte)(input.Length >> (8 * i)));
            }

            coder.Code(input, output, -1, -1, null);
            return output.ToArray();
        }
    }

    public static byte[] Decompress(byte[] toDecompress)
    {
        //return ConvertByteDecompress(toDecompress);
        SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder();

        using (MemoryStream input = new MemoryStream(toDecompress))
        using (MemoryStream output = new MemoryStream())
        {

            // Read the decoder properties
            byte[] properties = new byte[5];
            input.Read(properties, 0, 5);


            // Read in the decompress file size.
            byte[] fileLengthBytes = new byte[8];
            input.Read(fileLengthBytes, 0, 8);
            long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

            coder.SetDecoderProperties(properties);
            coder.Code(input, output, input.Length, fileLength, null);

            return output.ToArray();
        }
    }

    public static float[,] GenerateNoiseMap(int width, int height, float scale)
    {
        float[,] noiseMap = new float[width, height];

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = x / scale;
                float sampleY = y / scale;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = perlinValue;
            }
        }

        return noiseMap;
    }



    public static void ClearAllChildren(Transform T)
    {
        if (!T) return;
        int count = T.childCount;
        for (int i = 0; i < count; i++)
        {
            MonoBehaviour.Destroy(T.GetChild(i).gameObject);
        }
    }

    public static void ClearAllChildrenInEditMode(Transform T)
    {

        for (int i = 0; i < T.childCount; i++)
        {
            MonoBehaviour.DestroyImmediate(T.GetChild(i).gameObject);
            i--;
        }
    }

    private static byte[] buffer = new byte[8192]; // Choose an appropriate buffer size

    public static byte[] Deflate(byte[] input)
    {
        using (MemoryStream compressedStream = new MemoryStream())
        {
            using (DeflateStream compressionStream = new DeflateStream(compressedStream, CompressionMode.Compress, true))
            {
                compressionStream.Write(input, 0, input.Length);
            }
            return compressedStream.ToArray();
        }
    }

    public static byte[] Inflate(byte[] input)
    {
        using (MemoryStream compressedStream = new MemoryStream(input))
        using (DeflateStream decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress, true))
        using (MemoryStream decompressedStream = new MemoryStream())
        {
            int read;
            while ((read = decompressionStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                decompressedStream.Write(buffer, 0, read);
            }
            return decompressedStream.ToArray();
        }
    }


    public static float NormalizeAngle(float value, float min, float max)
    {
        float width = max - min;
        float offset = value - min;
        return (offset - (Mathf.Floor(offset / width) * width)) + min;
    }

    public static Vector3 NormalizEulerAngle(Vector3 rot)
    {
        return new Vector3(NormalizeAngle(rot.x, -180, 180), NormalizeAngle(rot.y, -180, 180), NormalizeAngle(rot.z, -180, 180));
    }

    static ushort MeshVersion = 1;
    public static byte[] SerializeMesh(Mesh mesh, bool CompressMatrix, bool CompressBoneWeights, bool CompressVertices, bool CompressNormals, bool CompressTangents, bool CompressUvs)
    {
        using (MemoryStream Ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(Ms))
        {
            //writer.Write(MeshVersion);
            writer.Write(mesh.name);
            writer.Write(CompressMatrix);
            writer.Write(CompressBoneWeights);
            writer.Write(CompressVertices);
            writer.Write(CompressNormals);
            writer.Write(CompressTangents);
            writer.Write(CompressUvs);

            writer.Write((ushort)(int)mesh.hideFlags);
            writer.Write((ushort)(int)mesh.indexBufferTarget);
            writer.Write((ushort)(int)mesh.indexFormat);

            writer.Write(mesh.bounds.center.x);
            writer.Write(mesh.bounds.center.y);
            writer.Write(mesh.bounds.center.z);
            writer.Write(mesh.bounds.extents.x);
            writer.Write(mesh.bounds.extents.y);
            writer.Write(mesh.bounds.extents.z);

            Vector4 MaxV4 = new Vector4(-float.MaxValue, -float.MaxValue, -float.MaxValue, -float.MaxValue);
            Vector4 MinV4 = new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);

            Vector3 MaxV3 = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            Vector3 MinV3 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            Vector2 MaxV2 = new Vector2(-float.MaxValue, -float.MaxValue);
            Vector2 MinV2 = new Vector2(float.MaxValue, float.MaxValue);
            Vector3[] vertices = mesh.vertices;
            //Debug.Log($"vertices : "+mesh.vertices.Length);
            if (CompressVertices)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    MinV3.x = Mathf.Min(MinV3.x, vertices[i].x);
                    MaxV3.x = Mathf.Max(MaxV3.x, vertices[i].x);

                    MinV3.y = Mathf.Min(MinV3.y, vertices[i].y);
                    MaxV3.y = Mathf.Max(MaxV3.y, vertices[i].y);

                    MinV3.z = Mathf.Min(MinV3.z, vertices[i].z);
                    MaxV3.z = Mathf.Max(MaxV3.z, vertices[i].z);
                }
                writer.Write(MinV3.x);
                writer.Write(MinV3.y);
                writer.Write(MinV3.z);

                writer.Write(MaxV3.x);
                writer.Write(MaxV3.y);
                writer.Write(MaxV3.z);
            }
            writer.Write(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (CompressVertices)
                {
                    writer.Write(CompressAsUshort(vertices[i].x, MinV3.x, MaxV3.x));
                    writer.Write(CompressAsUshort(vertices[i].y, MinV3.y, MaxV3.y));
                    writer.Write(CompressAsUshort(vertices[i].z, MinV3.z, MaxV3.z));
                }
                else
                {
                    writer.Write(vertices[i].x);
                    writer.Write(vertices[i].y);
                    writer.Write(vertices[i].z);
                }
            }



            writer.Write((byte)mesh.subMeshCount);

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] trises = mesh.GetTriangles(i);
                writer.Write(trises.Length);

                for (int j = 0; j < trises.Length; j++)
                {
                    writer.Write(trises[j]);
                }
                UnityEngine.Rendering.SubMeshDescriptor SMD = mesh.GetSubMesh(i);
                writer.Write(SMD.bounds.center.x);
                writer.Write(SMD.bounds.center.y);
                writer.Write(SMD.bounds.center.z);
                writer.Write(SMD.bounds.extents.x);
                writer.Write(SMD.bounds.extents.y);
                writer.Write(SMD.bounds.extents.z);

                writer.Write((byte)(int)SMD.topology);
                writer.Write(SMD.indexStart);
                writer.Write(SMD.indexCount);
                writer.Write(SMD.baseVertex);
                writer.Write(SMD.firstVertex);
                writer.Write(SMD.vertexCount);
            }


            MaxV3 = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            MinV3 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3[] normals = mesh.normals;

            if (CompressNormals)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    MinV3.x = Mathf.Min(MinV3.x, normals[i].x);
                    MaxV3.x = Mathf.Max(MaxV3.x, normals[i].x);

                    MinV3.y = Mathf.Min(MinV3.y, normals[i].y);
                    MaxV3.y = Mathf.Max(MaxV3.y, normals[i].y);

                    MinV3.z = Mathf.Min(MinV3.z, normals[i].z);
                    MaxV3.z = Mathf.Max(MaxV3.z, normals[i].z);
                }
                writer.Write(MinV3.x);
                writer.Write(MinV3.y);
                writer.Write(MinV3.z);

                writer.Write(MaxV3.x);
                writer.Write(MaxV3.y);
                writer.Write(MaxV3.z);
            }
            writer.Write(normals.Length);
            for (int i = 0; i < normals.Length; i++)
            {
                if (CompressNormals)
                {
                    writer.Write(CompressAsByte(normals[i].x, MinV3.x, MaxV3.x));
                    writer.Write(CompressAsByte(normals[i].y, MinV3.y, MaxV3.y));
                    writer.Write(CompressAsByte(normals[i].z, MinV3.z, MaxV3.z));
                }
                else
                {
                    writer.Write(normals[i].x);
                    writer.Write(normals[i].y);
                    writer.Write(normals[i].z);
                }
            }

            // Debug.Log($"Normals : "+mesh.normals.Length);
            MaxV4 = new Vector4(-float.MaxValue, -float.MaxValue, -float.MaxValue, -float.MaxValue);
            MinV4 = new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
            Vector4[] tangents = mesh.tangents;

            if (CompressTangents)
            {
                for (int i = 0; i < tangents.Length; i++)
                {
                    MinV4.x = Mathf.Min(MinV4.x, tangents[i].x);
                    MaxV4.x = Mathf.Max(MaxV4.x, tangents[i].x);

                    MinV4.y = Mathf.Min(MinV4.y, tangents[i].y);
                    MaxV4.y = Mathf.Max(MaxV4.y, tangents[i].y);

                    MinV4.z = Mathf.Min(MinV4.z, tangents[i].z);
                    MaxV4.z = Mathf.Max(MaxV4.z, tangents[i].z);

                    MinV4.w = Mathf.Min(MinV4.w, tangents[i].w);
                    MaxV4.w = Mathf.Max(MaxV4.w, tangents[i].w);
                }
                writer.Write(MinV4.x);
                writer.Write(MinV4.y);
                writer.Write(MinV4.z);
                writer.Write(MinV4.w);

                writer.Write(MaxV4.x);
                writer.Write(MaxV4.y);
                writer.Write(MaxV4.z);
                writer.Write(MaxV4.w);
            }
            writer.Write(tangents.Length);
            for (int i = 0; i < tangents.Length; i++)
            {
                if (CompressTangents)
                {
                    writer.Write(CompressAsByte(tangents[i].x, MinV4.x, MaxV4.x));
                    writer.Write(CompressAsByte(tangents[i].y, MinV4.y, MaxV4.y));
                    writer.Write(CompressAsByte(tangents[i].z, MinV4.z, MaxV4.z));
                    writer.Write(CompressAsByte(tangents[i].w, MinV4.z, MaxV4.w));
                }
                else
                {
                    writer.Write(tangents[i].x);
                    writer.Write(tangents[i].y);
                    writer.Write(tangents[i].z);
                    writer.Write(tangents[i].w);
                }
            }

            Vector2[][] Uvs = new Vector2[][] { mesh.uv, mesh.uv2, mesh.uv3, mesh.uv4, mesh.uv5, mesh.uv6, mesh.uv7, mesh.uv8 };

            for (int i = 0; i < 8; i++)
            {
                writer.Write(Uvs[i].Length);
                //Debug.Log($"Uvs[{i}] : "+Uvs[i].Length);
                if (CompressUvs)
                {
                    for (int j = 0; j < Uvs[i].Length; j++)
                    {
                        MinV2.x = Mathf.Min(MinV2.x, Uvs[i][j].x);
                        MaxV2.x = Mathf.Max(MaxV2.x, Uvs[i][j].x);

                        MinV2.y = Mathf.Min(MinV2.y, Uvs[i][j].y);
                        MaxV2.y = Mathf.Max(MaxV2.y, Uvs[i][j].y);
                    }
                    writer.Write(MinV2.x);
                    writer.Write(MinV2.y);

                    writer.Write(MaxV2.x);
                    writer.Write(MaxV2.y);

                    for (int j = 0; j < Uvs[i].Length; j++)
                    {
                        writer.Write(CompressAsUshort(Uvs[i][j].x, MinV2.x, MaxV2.x));
                        writer.Write(CompressAsUshort(Uvs[i][j].y, MinV2.y, MaxV2.y));
                    }
                }
                else for (int j = 0; j < Uvs[i].Length; j++)
                    {
                        writer.Write(Uvs[i][j].x);
                        writer.Write(Uvs[i][j].y);
                    }
            }



            float MaxVal = -float.MaxValue;
            float MinVal = float.MaxValue;

            //Debug.Log("BindPoses : "+mesh.bindposes.Length);
            for (int i = 0; i < mesh.bindposes.Length; i++)
            {
                Matrix4x4 M4 = mesh.bindposes[i];
                MinVal = Mathf.Min(MinVal, M4.m00, M4.m01, M4.m02, M4.m03, M4.m10, M4.m11, M4.m12, M4.m13, M4.m20, M4.m21, M4.m22, M4.m23);
                MaxVal = Mathf.Max(MaxVal, M4.m00, M4.m01, M4.m02, M4.m03, M4.m10, M4.m11, M4.m12, M4.m13, M4.m20, M4.m21, M4.m22, M4.m23);
            }
            if (CompressMatrix)
            {
                writer.Write(MinVal);
                writer.Write(MaxVal);
            }


            writer.Write(mesh.bindposes.Length);
            for (int i = 0; i < mesh.bindposes.Length; i++)
            {

                if (CompressMatrix)
                {
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m00, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m01, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m02, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m03, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m10, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m11, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m12, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m13, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m20, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m21, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m22, MinVal, MaxVal));
                    writer.Write(CompressAsUshort(mesh.bindposes[i].m23, MinVal, MaxVal));
                }
                else
                {
                    writer.Write(mesh.bindposes[i].m00);
                    writer.Write(mesh.bindposes[i].m01);
                    writer.Write(mesh.bindposes[i].m02);
                    writer.Write(mesh.bindposes[i].m03);
                    writer.Write(mesh.bindposes[i].m10);
                    writer.Write(mesh.bindposes[i].m11);
                    writer.Write(mesh.bindposes[i].m12);
                    writer.Write(mesh.bindposes[i].m13);
                    writer.Write(mesh.bindposes[i].m20);
                    writer.Write(mesh.bindposes[i].m21);
                    writer.Write(mesh.bindposes[i].m22);
                    writer.Write(mesh.bindposes[i].m23);
                    writer.Write(mesh.bindposes[i].m30);
                    writer.Write(mesh.bindposes[i].m31);
                    writer.Write(mesh.bindposes[i].m32);
                    writer.Write(mesh.bindposes[i].m33);
                }
            }


            MaxVal = float.MinValue;
            MinVal = float.MaxValue;
            writer.Write(mesh.boneWeights.Length);
            //   Debug.Log("S  -  BWLengths : "+mesh.boneWeights.Length+"   CBW : "+CompressBoneWeights.ToString());
            for (int i = 0; i < mesh.boneWeights.Length; i++)
            {
                writer.Write((byte)mesh.boneWeights[i].boneIndex0);
                writer.Write((byte)mesh.boneWeights[i].boneIndex1);
                writer.Write((byte)mesh.boneWeights[i].boneIndex2);
                writer.Write((byte)mesh.boneWeights[i].boneIndex3);

                if (CompressBoneWeights)
                {
                    MaxVal = Mathf.Max(MaxVal, mesh.boneWeights[i].weight0, mesh.boneWeights[i].weight1, mesh.boneWeights[i].weight2, mesh.boneWeights[i].weight3);
                    MinVal = Mathf.Min(MinVal, mesh.boneWeights[i].weight0, mesh.boneWeights[i].weight1, mesh.boneWeights[i].weight2, mesh.boneWeights[i].weight3);
                    writer.Write(CompressAsByte(mesh.boneWeights[i].weight0, 0, 1));
                    writer.Write(CompressAsByte(mesh.boneWeights[i].weight1, 0, 1));
                    writer.Write(CompressAsByte(mesh.boneWeights[i].weight2, 0, 1));
                    writer.Write(CompressAsByte(mesh.boneWeights[i].weight3, 0, 1));
                }
                else
                {
                    writer.Write(mesh.boneWeights[i].weight0);
                    writer.Write(mesh.boneWeights[i].weight1);
                    writer.Write(mesh.boneWeights[i].weight2);
                    writer.Write(mesh.boneWeights[i].weight3);
                }



            }
            Color32[] colors = mesh.colors32;
            writer.Write(colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                writer.Write(colors[i].r);
                writer.Write(colors[i].g);
                writer.Write(colors[i].b);
                writer.Write(colors[i].a);
            }
            //Debug.LogError("Max BW : "+MaxVal);
            // Debug.LogError("Min BW : "+MinVal);

            return Deflate(Ms.ToArray());

            //writer.Close();
            //Ms.Close();

        }
    }

    public static Mesh DeserializeMesh(byte[] Data)
    {

        Data = Inflate(Data);

        Mesh mesh = new Mesh();

        using (MemoryStream Ms = new MemoryStream(Data))
        using (BinaryReader reader = new BinaryReader(Ms))
        {
            //ushort Version = reader.ReadUInt16();
            mesh.name = reader.ReadString();
            bool CompressMatrix = reader.ReadBoolean();
            bool CompressBoneWeights = reader.ReadBoolean();
            bool CompressVertices = reader.ReadBoolean();
            bool CompressNormals = reader.ReadBoolean();
            bool CompressTangents = reader.ReadBoolean();
            bool CompressUvs = reader.ReadBoolean();

            mesh.hideFlags = (HideFlags)System.Enum.Parse(typeof(HideFlags), reader.ReadUInt16().ToString());
            mesh.indexBufferTarget = (GraphicsBuffer.Target)System.Enum.Parse(typeof(GraphicsBuffer.Target), reader.ReadUInt16().ToString());
            mesh.indexFormat = (UnityEngine.Rendering.IndexFormat)System.Enum.Parse(typeof(UnityEngine.Rendering.IndexFormat), reader.ReadUInt16().ToString()); ;

            Vector3 MaxV3 = new Vector3();
            Vector3 MinV3 = new Vector3();

            Vector2 MaxV2 = new Vector2(-float.MaxValue, -float.MaxValue);
            Vector2 MinV2 = new Vector2(float.MaxValue, float.MaxValue);
            float MaxVal = 0;
            float MinVal = 0;
            int lenghts;

            Vector3 BoundsCenter;
            Vector3 BoundsExtents;
            Bounds bounds = new Bounds();
            BoundsCenter.x = reader.ReadSingle();
            BoundsCenter.y = reader.ReadSingle();
            BoundsCenter.z = reader.ReadSingle();
            BoundsExtents.x = reader.ReadSingle();
            BoundsExtents.y = reader.ReadSingle();
            BoundsExtents.z = reader.ReadSingle();
            bounds.center = BoundsCenter;
            bounds.extents = BoundsExtents;
            mesh.bounds = bounds;

            if (CompressVertices)
            {
                MinV3.x = reader.ReadSingle();
                MinV3.y = reader.ReadSingle();
                MinV3.z = reader.ReadSingle();

                MaxV3.x = reader.ReadSingle();
                MaxV3.y = reader.ReadSingle();
                MaxV3.z = reader.ReadSingle();
            }

            lenghts = reader.ReadInt32();
            Vector3[] vertices = new Vector3[lenghts];
            if (lenghts >= ushort.MaxValue) Debug.Log($"{mesh.name} Has A Total Vertex Count Of {lenghts}");
            for (int i = 0; i < lenghts; i++)
            {
                if (CompressVertices)
                {
                    vertices[i].x = DeCompressFromUshort(reader.ReadUInt16(), MinV3.x, MaxV3.x);
                    vertices[i].y = DeCompressFromUshort(reader.ReadUInt16(), MinV3.y, MaxV3.y);
                    vertices[i].z = DeCompressFromUshort(reader.ReadUInt16(), MinV3.z, MaxV3.z);
                }
                else
                {
                    vertices[i].x = reader.ReadSingle();
                    vertices[i].y = reader.ReadSingle();
                    vertices[i].z = reader.ReadSingle();
                }
            }
            mesh.vertices = vertices;



            mesh.subMeshCount = reader.ReadByte();
            for (int i = 0; i < mesh.subMeshCount; i++)
            {

                lenghts = reader.ReadInt32();
                int[] triangles = new int[lenghts];
                for (int j = 0; j < lenghts; j++)
                {
                    triangles[j] = reader.ReadInt32();
                }
                mesh.SetTriangles(triangles, i);

                UnityEngine.Rendering.SubMeshDescriptor SMD = new UnityEngine.Rendering.SubMeshDescriptor();
                BoundsCenter.x = reader.ReadSingle();
                BoundsCenter.y = reader.ReadSingle();
                BoundsCenter.z = reader.ReadSingle();
                BoundsExtents.x = reader.ReadSingle();
                BoundsExtents.y = reader.ReadSingle();
                BoundsExtents.z = reader.ReadSingle();
                bounds.center = BoundsCenter;
                bounds.extents = BoundsExtents;

                SMD.bounds = bounds;
                SMD.topology = (MeshTopology)System.Enum.Parse(typeof(MeshTopology), reader.ReadByte().ToString());
                SMD.indexStart = reader.ReadInt32();
                SMD.indexCount = reader.ReadInt32();
                SMD.baseVertex = reader.ReadInt32();
                SMD.firstVertex = reader.ReadInt32();
                SMD.vertexCount = reader.ReadInt32();
                mesh.SetSubMesh(i, SMD);
            }


            if (CompressNormals)
            {
                MinV3.x = reader.ReadSingle();
                MinV3.y = reader.ReadSingle();
                MinV3.z = reader.ReadSingle();

                MaxV3.x = reader.ReadSingle();
                MaxV3.y = reader.ReadSingle();
                MaxV3.z = reader.ReadSingle();
            }
            lenghts = reader.ReadInt32();
            Vector3[] normals = new Vector3[lenghts];
            for (int i = 0; i < lenghts; i++)
            {
                if (CompressNormals)
                {
                    normals[i].x = DeCompressFromByte(reader.ReadByte(), MinV3.x, MaxV3.x);
                    normals[i].y = DeCompressFromByte(reader.ReadByte(), MinV3.y, MaxV3.y);
                    normals[i].z = DeCompressFromByte(reader.ReadByte(), MinV3.z, MaxV3.z);
                }
                else
                {
                    normals[i].x = reader.ReadSingle();
                    normals[i].y = reader.ReadSingle();
                    normals[i].z = reader.ReadSingle();
                }
            }
            mesh.SetNormals(normals);

            Vector4 MaxV4 = new Vector4();
            Vector4 MinV4 = new Vector4();
            if (CompressTangents)
            {
                MinV4.x = reader.ReadSingle();
                MinV4.y = reader.ReadSingle();
                MinV4.z = reader.ReadSingle();
                MinV4.w = reader.ReadSingle();

                MaxV4.x = reader.ReadSingle();
                MaxV4.y = reader.ReadSingle();
                MaxV4.z = reader.ReadSingle();
                MaxV4.w = reader.ReadSingle();
            }
            lenghts = reader.ReadInt32();
            Vector4[] tangents = new Vector4[lenghts];
            for (int i = 0; i < lenghts; i++)
            {
                if (CompressTangents)
                {
                    tangents[i].x = DeCompressFromByte(reader.ReadByte(), MinV4.x, MaxV4.x);
                    tangents[i].y = DeCompressFromByte(reader.ReadByte(), MinV4.y, MaxV4.y);
                    tangents[i].z = DeCompressFromByte(reader.ReadByte(), MinV4.z, MaxV4.z);
                    tangents[i].w = DeCompressFromByte(reader.ReadByte(), MinV4.w, MaxV4.w);
                }
                else
                {
                    tangents[i].x = reader.ReadSingle();
                    tangents[i].y = reader.ReadSingle();
                    tangents[i].z = reader.ReadSingle();
                    tangents[i].w = reader.ReadSingle();
                }
            }
            mesh.SetTangents(tangents);


            Vector2[][] Uvs = new Vector2[8][];//{uv,uv2,uv3,uv4,uv5,uv6,uv7,uv8};

            for (int i = 0; i < 8; i++)
            {
                lenghts = reader.ReadInt32();
                Uvs[i] = new Vector2[lenghts];
                if (CompressUvs)
                {
                    MinV2.x = reader.ReadSingle();
                    MinV2.y = reader.ReadSingle();

                    MaxV2.x = reader.ReadSingle();
                    MaxV2.y = reader.ReadSingle();

                    for (int j = 0; j < lenghts; j++)
                    {
                        Uvs[i][j].x = DeCompressFromUshort(reader.ReadUInt16(), MinV2.x, MaxV2.x);
                        Uvs[i][j].y = DeCompressFromUshort(reader.ReadUInt16(), MinV2.y, MaxV2.y);
                    }
                }
                else for (int j = 0; j < lenghts; j++)
                    {
                        Uvs[i][j].x = reader.ReadSingle();
                        Uvs[i][j].y = reader.ReadSingle();
                    }
                mesh.SetUVs(i, Uvs[i]);
            }

            if (CompressMatrix) { MinVal = reader.ReadSingle(); MaxVal = reader.ReadSingle(); }
            lenghts = reader.ReadInt32();
            //mesh.bindposes=new Matrix4x4[lenghts];
            Matrix4x4[] bindPoses = new Matrix4x4[lenghts];
            //    Debug.LogError("De  -  BPLengths : "+lenghts+"   CM : "+CompressMatrix.ToString());
            for (int i = 0; i < lenghts; i++)
            {
                if (CompressMatrix)
                {
                    bindPoses[i].m00 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m01 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m02 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m03 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m10 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m11 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m12 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m13 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m20 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m21 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m22 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    bindPoses[i].m23 = DeCompressFromUshort(reader.ReadUInt16(), MinVal, MaxVal);
                    //bindPoses[i].m30=DeCompressFromUshort(reader.ReadUInt16(),MinVal,MaxVal);
                    //bindPoses[i].m31=DeCompressFromUshort(reader.ReadUInt16(),MinVal,MaxVal);
                    //bindPoses[i].m32=DeCompressFromUshort(reader.ReadUInt16(),MinVal,MaxVal);
                    //bindPoses[i].m33=DeCompressFromUshort(reader.ReadUInt16(),MinVal,MaxVal);
                    bindPoses[i].m33 = 1;
                }
                else
                {
                    bindPoses[i].m00 = reader.ReadSingle();
                    bindPoses[i].m01 = reader.ReadSingle();
                    bindPoses[i].m02 = reader.ReadSingle();
                    bindPoses[i].m03 = reader.ReadSingle();
                    bindPoses[i].m10 = reader.ReadSingle();
                    bindPoses[i].m11 = reader.ReadSingle();
                    bindPoses[i].m12 = reader.ReadSingle();
                    bindPoses[i].m13 = reader.ReadSingle();
                    bindPoses[i].m20 = reader.ReadSingle();
                    bindPoses[i].m21 = reader.ReadSingle();
                    bindPoses[i].m22 = reader.ReadSingle();
                    bindPoses[i].m23 = reader.ReadSingle();
                    bindPoses[i].m30 = reader.ReadSingle();
                    bindPoses[i].m31 = reader.ReadSingle();
                    bindPoses[i].m32 = reader.ReadSingle();
                    bindPoses[i].m33 = reader.ReadSingle();
                }
            }
            mesh.bindposes = bindPoses;


            lenghts = reader.ReadInt32();
            //   Debug.LogError("DS  -  BWLengths : "+lenghts+"   CBW : "+CompressBoneWeights.ToString());
            BoneWeight[] boneWeights = new BoneWeight[lenghts];
            for (int i = 0; i < lenghts; i++)
            {
                boneWeights[i].boneIndex0 = reader.ReadByte();
                boneWeights[i].boneIndex1 = reader.ReadByte();
                boneWeights[i].boneIndex2 = reader.ReadByte();
                boneWeights[i].boneIndex3 = reader.ReadByte();
                if (CompressBoneWeights)
                {
                    boneWeights[i].weight0 = DeCompressFromByte(reader.ReadByte(), 0, 1) + 0.1f;
                    boneWeights[i].weight1 = DeCompressFromByte(reader.ReadByte(), 0, 1) + 0.1f;
                    boneWeights[i].weight2 = DeCompressFromByte(reader.ReadByte(), 0, 1) + 0.1f;
                    boneWeights[i].weight3 = DeCompressFromByte(reader.ReadByte(), 0, 1) + 0.1f;
                }
                else
                {
                    boneWeights[i].weight0 = reader.ReadSingle();
                    boneWeights[i].weight1 = reader.ReadSingle();
                    boneWeights[i].weight2 = reader.ReadSingle();
                    boneWeights[i].weight3 = reader.ReadSingle();
                }
            }
            mesh.boneWeights = boneWeights;


            if (Ms.Position < Ms.Length)
            {
                lenghts = reader.ReadInt32();
                Color32[] colors = new Color32[lenghts];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i].r = reader.ReadByte();
                    colors[i].g = reader.ReadByte();
                    colors[i].b = reader.ReadByte();
                    colors[i].a = reader.ReadByte();

                }
                mesh.colors32 = colors;
            }
        }
        mesh.OptimizeReorderVertexBuffer();
        mesh.OptimizeIndexBuffers();
        mesh.Optimize();
        return mesh;
    }

    public static Vector3[] GiftWrapping(Vector3[] points)
    {
        if (points == null || points.Length < 3) return points;
        List<Vector3> convexHull = new List<Vector3>();

        // Find the point with the lowest x-coordinate (and frontmost if tied)
        int anchorIndex = 0;
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i].x < points[anchorIndex].x || (points[i].x == points[anchorIndex].x && points[i].z < points[anchorIndex].z))
            {
                anchorIndex = i;
            }
        }

        int current = anchorIndex;
        do
        {
            convexHull.Add(points[current]);

            // Find the next point on the convex hull
            int next = (current + 1) % points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                // If the point is to the left of the current->next vector, update next
                if (Orientation(points[current], points[i], points[next]) == 2)
                {
                    next = i;
                }
            }

            current = next;
        } while (current != anchorIndex);

        return convexHull.ToArray();
    }

    // Helper function to determine the orientation of three points
    // 0: Collinear, 1: Clockwise, 2: Counterclockwise
    public static int Orientation(Vector3 p, Vector3 q, Vector3 r)
    {
        float val = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);
        if (val == 0) return 0;
        return (val > 0) ? 1 : 2;
    }


    public static byte[] PathTobyteArray(string[] PathsArray, string path)
    {
        string[] pathSplit = path.Split('/');
        byte[] PathBytes = new byte[pathSplit.Length];
        for (int i = 0; i < pathSplit.Length; i++)
        {
            for (int j = 0; j < PathsArray.Length; j++)
            {
                if (PathsArray[j].CompareTo(pathSplit[i]) == 0) { PathBytes[i] = (byte)j; continue; }
            }

        }
        Debug.LogError("S PathsArray Length : " + PathsArray.Length);
        Debug.LogError("S PathBytes Length : " + PathBytes.Length);

        //SbyteArray=PathBytes;
        //SPathArray=PathsArray;

        return PathBytes;
    }

    public static string ByteToPathArray(string[] PathsArray, byte[] PathBytes)
    {
        Debug.LogError("DS PathsArray Length : " + PathsArray.Length);
        Debug.LogError("DS PathBytes Length : " + PathBytes.Length);
        //DSbyteArray=PathBytes;
        //DSPathArray=PathsArray;
        string Path = PathsArray[PathBytes[0]];
        for (int i = 1; i < PathBytes.Length; i++)
        {
            Path = string.Join("/", Path, PathsArray[PathBytes[i]]);
        }
        Debug.LogError("RCPA : " + Path);

        return Path;
    }

#if UNITY_EDITOR

    public static byte[] SerializeAnim(GameObject Rig, AnimationClip anim, bool Compress)
    {
        anim = MonoBehaviour.Instantiate(anim);
        using (MemoryStream Ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(Ms))
        {
            Vector3 MaxV3 = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            Vector3 MinV3 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            //float MaxVal=-float.MaxValue;
            //float MinVal=float.MaxValue;

            writer.Write(Compress);
            writer.Write(anim.name);
            writer.Write(anim.frameRate);
            writer.Write((byte)(int)anim.hideFlags);
            writer.Write((byte)(int)anim.wrapMode);
            writer.Write(anim.legacy);

            writer.Write(anim.localBounds.center.x);
            writer.Write(anim.localBounds.center.y);
            writer.Write(anim.localBounds.center.z);

            writer.Write(anim.localBounds.extents.x);
            writer.Write(anim.localBounds.extents.y);
            writer.Write(anim.localBounds.extents.z);

            writer.Write(anim.events.Length);
            for (int i = 0; i < anim.events.Length; i++)
            {
                writer.Write(anim.events[i].functionName);
                writer.Write(anim.events[i].floatParameter);
                writer.Write(anim.events[i].intParameter);
                writer.Write(anim.events[i].stringParameter);
                writer.Write((byte)(int)anim.events[i].messageOptions);
            }


            Vector2 TimeRange = new Vector2(float.MaxValue, float.MinValue);
            Vector2 ValueRange = new Vector2(float.MaxValue, float.MinValue);

            Transform[] Ts = Rig.GetComponentsInChildren<Transform>();
            HashSet<string> PathNamesList = new HashSet<string>();
            EditorCurveBinding[] CurveBinding = AnimationUtility.GetCurveBindings(anim);
            for (int i = 0; i < CurveBinding.Length; i++)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(anim, CurveBinding[i]);
                for (int j = 0; j < curve.keys.Length; j++)
                {

                    TimeRange.x = Mathf.Min(TimeRange.x, curve[j].time);
                    TimeRange.y = Mathf.Max(TimeRange.y, curve[j].time);

                    ValueRange.x = Mathf.Min(ValueRange.x, curve[j].value);
                    ValueRange.y = Mathf.Max(ValueRange.y, curve[j].value);
                }
                foreach (string S in CurveBinding[i].path.Split('/'))
                {
                    foreach (Transform T in Ts)
                    {
                        if (T.name.CompareTo(S) == 0) { PathNamesList.Add(S); continue; }
                    }
                }
            }
            string[] PathNames = PathNamesList.ToArray();
            writer.Write(PathNames.Length);
            for (int i = 0; i < PathNames.Length; i++)
            {
                writer.Write(PathNames[i]);
            }
            writer.Write(TimeRange.x); writer.Write(TimeRange.y);
            writer.Write(ValueRange.x); writer.Write(ValueRange.y);


            writer.Write(CurveBinding.Length);
            for (int i = 0; i < CurveBinding.Length; i++)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(anim, CurveBinding[i]);
                string propertyName = CurveBinding[i].propertyName.Replace("localEulerAnglesRaw", "localEulerAngles");
                if (propertyName.ToLower().Contains("scale")) { writer.Write(false); continue; }
                string[] paths = CurveBinding[i].path.Split('/');
                string path = paths[paths.Length - 1];
                Debug.LogError("BoneName : " + path);
                Transform T = null;//Rig.transform.Find(path);
                foreach (Transform T2 in Ts)
                {
                    if (T2.gameObject.name == path) { T = T2; break; }
                }
                if (T == null)
                {
                    writer.Write(false);
                    Debug.LogError(path + " Was Not Found In Rig");
                    continue;
                }

                writer.Write(true);
                byte[] PathBytes = PathTobyteArray(PathNames, CurveBinding[i].path);

                writer.Write((byte)PathBytes.Length);
                for (int j = 0; j < PathBytes.Length; j++) writer.Write(PathBytes[j]);
                writer.Write(propertyName);
                bool IsRotation = propertyName.Contains("localEulerAngles");
                curve = CompressCurve(AnimationUtility.GetEditorCurve(anim, CurveBinding[i]).keys, IsRotation ? 0.001f : 0.002f);

                writer.Write(curve.keys.Length);
                for (int j = 0; j < curve.keys.Length; j++)
                {

                    float value = curve.keys[j].value;
                    if (IsRotation)
                    {
                        anim.SampleAnimation(Rig, curve.keys[j].time);

                        if (propertyName.Split('.')[1] == "x")
                            value = NormalizeAngle(T.localRotation.eulerAngles.x, -210, 150);
                        else
                        if (propertyName.Split('.')[1] == "y")
                            value = NormalizeAngle(T.localRotation.eulerAngles.y, -210, 150);
                        else
                            value = NormalizeAngle(T.localRotation.eulerAngles.z, -210, 150);
                    }
                    //if(j==0)Debug.LogError("Path : "+CurveBinding[i].path+" : "+propertyName+" = "+value);

                    if (Compress)
                    {
                        writer.Write(CompressAsUshort(curve.keys[j].time, TimeRange.x, TimeRange.y));
                        writer.Write(CompressAsUshort(value, ValueRange.x, ValueRange.y));
                    }
                    else
                    {
                        writer.Write(curve.keys[j].time);
                        writer.Write(value);
                    }
                }
            }

            return Deflate(Ms.ToArray());

        }
    }

#endif
    public static AnimationClip DeSerializeAnim(byte[] Data)
    {

        Data = Inflate(Data);
        AnimationClip anim = new AnimationClip();

        using (MemoryStream Ms = new MemoryStream(Data))
        using (BinaryReader reader = new BinaryReader(Ms))
        {
            bool Compress = reader.ReadBoolean();
            anim.name = reader.ReadString();
            anim.frameRate = reader.ReadSingle();
            anim.hideFlags = (HideFlags)reader.ReadByte();
            anim.wrapMode = (WrapMode)reader.ReadByte();
            anim.legacy = reader.ReadBoolean();

            Vector3 BoundsCenter;
            Vector3 BoundsExtents;
            Bounds bounds = new Bounds();
            BoundsCenter.x = reader.ReadSingle();
            BoundsCenter.y = reader.ReadSingle();
            BoundsCenter.z = reader.ReadSingle();
            BoundsExtents.x = reader.ReadSingle();
            BoundsExtents.y = reader.ReadSingle();
            BoundsExtents.z = reader.ReadSingle();
            bounds.center = BoundsCenter;
            bounds.extents = BoundsExtents;
            anim.localBounds = bounds;

            int AnimEventLenght = reader.ReadInt32();
            AnimationEvent[] Events = new AnimationEvent[AnimEventLenght];
            for (int i = 0; i < AnimEventLenght; i++)
            {
                AnimationEvent AE = Events[i];
                AE.functionName = reader.ReadString();
                AE.floatParameter = reader.ReadSingle();
                AE.intParameter = reader.ReadInt32();
                AE.stringParameter = reader.ReadString();
                AE.messageOptions = (SendMessageOptions)reader.ReadByte();
            }
            anim.events = Events;

            string[] PathNames = new string[reader.ReadInt32()];
            for (int i = 0; i < PathNames.Length; i++)
            {
                PathNames[i] = reader.ReadString();
            }

            Vector2 TimeRange = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            Vector2 ValueRange = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            int CurvesLength = reader.ReadInt32();
            AnimationCurve[] Curves = new AnimationCurve[CurvesLength];
            for (int i = 0; i < CurvesLength; i++)
            {
                if (!reader.ReadBoolean()) continue;
                Curves[i] = new AnimationCurve();
                AnimationCurve curve = Curves[i];
                byte[] PathBytes = new byte[reader.ReadByte()];
                for (int j = 0; j < PathBytes.Length; j++)
                    PathBytes[j] = reader.ReadByte();

                string path = ByteToPathArray(PathNames, PathBytes);
                string propertyName = reader.ReadString();
                //curve.postWrapMode=(WrapMode)reader.ReadByte();
                //curve.preWrapMode=(WrapMode)reader.ReadByte();

                int KeyframesLenght = reader.ReadInt32();
                Keyframe[] keyFrames = new Keyframe[KeyframesLenght];
                for (int j = 0; j < KeyframesLenght; j++)
                {
                    //keyFrames[j].weightedMode=(WeightedMode)reader.ReadByte();
                    //keyFrames[j].tangentMode=reader.ReadByte();
                    //keyFrames[j].weightedMode=WeightedMode.None;
                    //keyFrames[j].tangentMode=2;
                    //Debug.LogError("DS Tangent Mode : "+keyFrames[j].tangentMode);
                    if (Compress)
                    {
                        keyFrames[j].time = DeCompressFromUshort(reader.ReadUInt16(), TimeRange.x, TimeRange.y);
                        keyFrames[j].value = DeCompressFromUshort(reader.ReadUInt16(), ValueRange.x, ValueRange.y);
                    }
                    else
                    {
                        //keyFrames[j].inTangent=reader.ReadSingle();
                        // keyFrames[j].inWeight=reader.ReadSingle();
                        //keyFrames[j].outTangent=reader.ReadSingle();
                        //keyFrames[j].outWeight=reader.ReadSingle();
                        keyFrames[j] = new Keyframe(reader.ReadSingle(), reader.ReadSingle());
                        //keyFrames[j].time=reader.ReadSingle();
                        //keyFrames[j].value=reader.ReadSingle();
                    }
                    curve.AddKey(keyFrames[j]);
                }
                //curve.keys=keyFrames;
                anim.SetCurve(path, typeof(UnityEngine.Transform), propertyName, curve);
            }

        }

        //anim.EnsureQuaternionContinuity();

        return anim;
    }


    // Function to serialize AudioClip to byte array
    // Function to serialize AudioClip to byte array
    public static byte[] SerializeAudioClip(AudioClip audioClip)
    {
#if UNITY_EDITOR
        if (audioClip == null)
        {
            Debug.LogError("AudioClip is null.");
            return null;
        }

        // Create a memory stream to hold the serialized data
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                string path = AssetDatabase.GetAssetPath(audioClip);
                byte[] data = File.ReadAllBytes(path);
                // Write sample rate, channels, and audio data to the stream
                writer.Write(Path.GetExtension(path).ToLower());
                writer.Write(data.Length);
                writer.Write(data);
                writer.Write(audioClip.channels);
            }

            // Convert the memory stream to a byte array
            return memoryStream.ToArray();
        }
#endif
        return null;
    }

    // Function to deserialize byte array to AudioClip
    public static AudioClip DeserializeAudioClip(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length == 0)
        {
            Debug.LogError("Byte array is null or empty.");
            return null;
        }

        // Create a memory stream from the byte array
        using (MemoryStream memoryStream = new MemoryStream(byteArray))
        {
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                string ext = reader.ReadString();
                int lenght = reader.ReadInt32();
                byte[] data = reader.ReadBytes(lenght);
                File.WriteAllBytes(Application.persistentDataPath + "TmpAudio" + ext, data);
                AudioClip AC = LoadAudioClipFromURL(Application.persistentDataPath + "TmpAudio" + ext);
                return AC;
            }
        }
    }

    public static AudioClip LoadAudioClipFromURL(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
        {
            www.SendWebRequest();
            while (!www.isDone) { }
            if (www.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerAudioClip.GetContent(www);
            }
            else
            {
                Debug.LogError("Error Loading Audio : " + www.error);
                return null;
            }
        }

    }

    public static byte[] SerializeTexture2D(Texture2D tex)
    {
        if (tex == null)
        {
            Debug.LogError("Texture is null.");
            return null;
        }

        // Get audio data as float array
        byte[] textureData = tex.GetRawTextureData();

        // Create a memory stream to hold the serialized data
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                // Write sample rate, channels, and audio data to the stream
                writer.Write(tex.width);
                writer.Write(tex.height);
                writer.Write((byte)tex.format);
                writer.Write(tex.mipmapCount);
                writer.Write(textureData.Length);
                writer.Write(textureData);
            }

            // Convert the memory stream to a byte array
            return memoryStream.ToArray();
        }
    }

    // Function to deserialize byte array to AudioClip
    public static Texture2D DeserializeTexture2D(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length == 0)
        {
            Debug.LogError("Byte array is null or empty.");
            return null;
        }

        // Create a memory stream from the byte array
        using (MemoryStream memoryStream = new MemoryStream(byteArray))
        {
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                TextureFormat format = (TextureFormat)reader.ReadByte();
                int mipCount = reader.ReadInt32();
                int lenght = reader.ReadInt32();
                byte[] data = reader.ReadBytes(lenght);
                Texture2D tex = new Texture2D(width, height, format, mipCount, false);
                tex.LoadRawTextureData(data);
                return tex;
            }
        }
    }

    public static ushort CompressAsUshort(float value, float min, float max)
    {
        return (ushort)(((value - min) / (max - min)) * 65535);
    }

    public static byte CompressAsByte(float value, float min, float max)
    {
        return (byte)(((value - min) / (max - min)) * 255);
    }

    public static float DeCompressFromUshort(ushort value, float min, float max)
    {
        return (float)(((value / 65535f) * (max - min)) + min);
    }

    public static float DeCompressFromByte(byte value, float min, float max)
    {
        return (float)(((value / 255f) * (max - min)) + min);
    }




    public static AnimationCurve CompressCurve(Keyframe[] keys, float error, int ScaledFPS = 15, float MainFPS = 30)
    {

        float TimeLenght = (float)ScaledFPS / MainFPS;

        AnimationCurve curve = new AnimationCurve(keys);

        //Keyframe[] keys = curve.keys;

        //COMPRESS KEYS
        int i = 1;
        while (i < keys.Length - 1 && keys.Length > 2)
        {
            Keyframe[] testKeys = new Keyframe[keys.Length - 1];
            int c = 0;
            for (int n = 0; n < keys.Length; n++)
            {
                if (i != n)
                {
                    testKeys[c] = new Keyframe(keys[n].time, keys[n].value, keys[n].inTangent, keys[n].outTangent);
                    c++;
                }
            }

            AnimationCurve testCurve = new AnimationCurve();
            testCurve.keys = testKeys;

            float test0 = Mathf.Abs(testCurve.Evaluate(keys[i].time) - keys[i].value);
            float beforeTime = keys[i].time + (keys[i - 1].time - keys[i].time) * 0.5f;
            float afterTime = keys[i].time + (keys[i + 1].time - keys[i].time) * 0.5f;

            float testBefore = Mathf.Abs(testCurve.Evaluate(beforeTime) - curve.Evaluate(beforeTime));
            float testAfter = Mathf.Abs(testCurve.Evaluate(afterTime) - curve.Evaluate(afterTime));

            if (test0 < error && testBefore < error && testAfter < error)
            {
                keys = testKeys;
            }
            else
            {
                i++;
            }
        }
        //COMPRESS KEYS//

        //OPTIMIZE  KEYFRAMES//
        for (i = 0; i < keys.Length; i++)
        {
            keys[i].time *= TimeLenght;
        }
        //DECLUSTER KEYFRAMES//
        List<Keyframe> KeyList = new List<Keyframe>();
        Keyframe lastKeyfram = keys[0];
        KeyList.Add(keys[0]);
        for (i = 0; i < keys.Length; i++)
        {
            keys[i].time = Mathf.Round(keys[i].time * MainFPS) / MainFPS;
            //Debug.Log("Time : "+keys[i].time);
            //Debug.Log("Frame : "+keys[i].time*framerate);
            if (keys[i].time == lastKeyfram.time) { lastKeyfram = keys[i]; continue; }
            KeyList.Add(keys[i]);
            lastKeyfram = keys[i];
            //keys[i].time /= TimeLenght;
        }
        Keyframe[] kf = KeyList.ToArray();
        //DECLUSTER KEYFRAMES//


        for (i = 0; i < kf.Length; i++)
        {
            kf[i].time /= TimeLenght;
        }
        //OPTIMIZE  KEYFRAMES END//



        curve = new AnimationCurve(kf);
        return curve;
    }

    public static string GetMeshChecksum(Mesh mesh)
    {
        string Checksum = "";
#if UNITY_EDITOR
        MD5 md5 = MD5.Create();

        // Combine vertices and UVs into a single byte array
        List<byte> meshData = new List<byte>();

        foreach (Vector3 vertex in mesh.vertices)
        {
            meshData.AddRange(BitConverter.GetBytes(vertex.x));
            meshData.AddRange(BitConverter.GetBytes(vertex.y));
            meshData.AddRange(BitConverter.GetBytes(vertex.z));
        }

        foreach (Vector2 uv in mesh.uv)
        {
            meshData.AddRange(BitConverter.GetBytes(uv.x));
            meshData.AddRange(BitConverter.GetBytes(uv.y));
        }

        // Calculate the hash
        byte[] hashBytes = md5.ComputeHash(meshData.ToArray());

        // Convert the hash bytes to a hexadecimal string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("x2"));
        }

        Checksum = sb.ToString();
#endif
        return Checksum;
    }


    public static string GetTransformChecksum(Transform transform)
    {
        return transform.GetInstanceID().ToString();
        string Checksum = "";
#if UNITY_EDITOR
        MD5 md5 = MD5.Create();

        // Transform properties
        string transformData = $"{transform.position},{transform.rotation},{transform.localScale},{transform.name},{transform.tag},{LayerMask.LayerToName(transform.gameObject.layer)},{transform.childCount},{transform.GetSiblingIndex()}";
        Transform T = transform.parent;
        while (T != null && T.parent != T)
        {
            transformData += $"{T.position},{T.rotation},{T.localScale},{T.name},{T.tag},{LayerMask.LayerToName(T.gameObject.layer)},{T.childCount},{T.GetSiblingIndex()}";

            T = T.parent;
        }
        // Child components
        List<string> childComponents = new List<string>();
        foreach (Component component in transform.GetComponents<Component>())
        {
            childComponents.Add(EditorJsonUtility.ToJson(component));
        }
        string componentsData = string.Join(",", childComponents);

        // Child transforms
        List<string> childTransformsData = new List<string>();
        foreach (Transform childTransform in transform)
        {
            childTransformsData.Add(GetTransformChecksum(childTransform));
        }
        string childTransformsChecksum = string.Join(",", childTransformsData);

        // Combine all data
        string combinedData = $"{transformData},{componentsData},{childTransformsChecksum}";

        // Calculate the hash
        byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(combinedData));

        // Convert the hash bytes to a hexadecimal string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("x2"));
        }

        Checksum = sb.ToString();
#endif
        return Checksum;
    }

    public static string GetTextureChecksum(Texture tex)
    {
        if (tex == null) return null;
        StringBuilder sb = new StringBuilder();

        // Include material name
        sb.Append(tex.name);
#if UNITY_EDITOR
        sb.Append(tex.imageContentsHash);
#endif
        return ComputeMD5Hash(sb.ToString());
    }

    public static string GetAudioChecksum(AudioClip clip)
    {
        StringBuilder sb = new StringBuilder();

#if UNITY_EDITOR
        // Include material name
        sb.Append(clip.name);
        sb.Append(AssetDatabase.GetAssetPath(clip));
        float[] data = new float[15];
        clip.GetData(data, 0);
        sb.Append(string.Join(",", data));
        sb.Append(clip.samples);
        sb.Append(clip.channels);
        sb.Append(clip.frequency);
#endif
        return ComputeMD5Hash(sb.ToString());
    }

    public static string GetMaterialChecksum(Material material)
    {
        StringBuilder sb = new StringBuilder();

#if UNITY_EDITOR
        // Include material name
        sb.Append(material.name);

        // Include material shader name
        sb.Append(material.shader.name);

        // Include material textures
        foreach (var str in material.GetTexturePropertyNames())
        {
            if (str != null)
            {
                sb.Append(str);
            }
        }


        int propertyCount = ShaderUtil.GetPropertyCount(material.shader);


        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName = ShaderUtil.GetPropertyName(material.shader, i);
            ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(material.shader, i);

            if (propertyType == ShaderUtil.ShaderPropertyType.Float)
            {
                sb.Append(material.GetFloat(propertyName).ToString());
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.Int)
            {
                sb.Append(material.GetInt(propertyName).ToString());
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.Color)
            {
                sb.Append(material.GetColor(propertyName).ToString());
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.Vector)
            {
                sb.Append(material.GetVector(propertyName).ToString());
            }
            else
            if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                Texture tx = material.GetTexture(propertyName);
                if (tx != null)
                    sb.Append(tx.name);
            }
        }
#endif
        // Include other relevant properties as needed

        // Compute MD5 hash
        string checksum = ComputeMD5Hash(sb.ToString());

        return checksum;
    }

    // Function to compute MD5 hash from a string
    private static string ComputeMD5Hash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder hashBuilder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashBuilder.Append(hashBytes[i].ToString("x2"));
            }

            return hashBuilder.ToString();
        }
    }

    // Encode a path string
    public static string SimpleEncode(string str)
    {
        byte[] bytesToEncode = Encoding.UTF8.GetBytes(str);
        return Convert.ToBase64String(bytesToEncode);
    }

    // Decode a path string
    public static string SimpleDecode(string str)
    {
        byte[] decodedBytes = Convert.FromBase64String(str);
        return Encoding.UTF8.GetString(decodedBytes);
    }


    public static string CreateDirectory(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static T[] Shuffle<T>(T[] array)
    {
        T[] shuffledArray = (T[])array.Clone(); // Clone the original array
        int n = shuffledArray.Length;

        // Perform the Fisher-Yates shuffle algorithm
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // Get a random index from 0 to i
            T temp = shuffledArray[i];
            shuffledArray[i] = shuffledArray[j];
            shuffledArray[j] = temp;
        }

        return shuffledArray;
    }


    public static bool TryLoadImage(this Texture2D Tex, byte[] data, bool markNonReadable = false)
    {
        try
        {
            if (CheckIfPngOrJpg(data)) 
            return Tex.LoadImage(data, markNonReadable);
            else 
            if (CheckIfTGA(data)){
            return Tex.LoadTGA(data, markNonReadable);
            }
            else
            {
                return Tex.LoadImage(ImgToPng(data), markNonReadable);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }

    }

    public static bool CheckIfPngOrJpg(byte[] data)
    {
        return data != null && data.Length >= 2 &&
            ((data[0] == 0x89 && data[1] == 0x50) ||  // PNG
             (data[0] == 0xFF && data[1] == 0xD8));   // JPEG
    }


public static bool CheckIfTGA(byte[] data)
{
    // TGA files have a minimum header size of 18 bytes
    if (data == null || data.Length < 18)
    {
        return false;
    }

    // TGA header fields
    byte idLength = data[0];        // ID length (byte 0)
    byte colorMapType = data[1];     // Color map type (byte 1)
    byte imageType = data[2];        // Image type (byte 2)

    // Image type should be one of the valid TGA image types (1, 2, 3, 9, 10, 11)
    // 1: Color-mapped (indexed) image
    // 2: Truecolor image
    // 3: Black and white (grayscale) image
    // 9: RLE color-mapped image
    // 10: RLE truecolor image
    // 11: RLE black and white image
    if (imageType != 1 && imageType != 2 && imageType != 3 &&
        imageType != 9 && imageType != 10 && imageType != 11)
    {
        return false;
    }

    // Further checks can be added as needed (e.g., validate other fields, color map specification, etc.)

    // If we passed all checks, assume the data represents a TGA file
    return true;
}

    public static byte[] ImgToPng(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError("Invalid image data");
            return null;
        }

        using (MemoryStream pngStream = new MemoryStream())
        {
            using (Bitmap bitmap = new Bitmap(new MemoryStream(imageData)))
            {
                bitmap.Save(pngStream, ImageFormat.Png);
            }

            return pngStream.ToArray();
        }
    }


    public static Cubemap GetReadable(this Cubemap cubemap)
    {
        Material BlendMat = new Material(Shader.Find("Hidden/CubeRT"));
        BlendMat.SetTexture("_CubeMap", cubemap);

        RenderTexture rt = RenderTexture.GetTemporary((int)cubemap.width, (int)cubemap.height);
        //rt.enableRandomWrite = true;
        rt.Create();

        rt.filterMode = FilterMode.Point;
        
        RenderTexture.active = rt;
        Cubemap newCubemap = new Cubemap(cubemap.width, TextureFormat.RGBA32, cubemap.mipmapCount > 0);
        for (int i = 0; i < 6; i++)
        {
            BlendMat.SetFloat("_FaceIndex",i);
            Graphics.Blit(null, rt, BlendMat);
            Texture2D nTex = new Texture2D((int)cubemap.width, (int)cubemap.height);
            nTex.name = cubemap.name + i;
            nTex.ReadPixels(new Rect(0, 0, (int)cubemap.width, (int)cubemap.height), 0, 0);
            nTex.filterMode = cubemap.filterMode;
            nTex.Apply(true);
            GL.Clear(true, true, Color.clear);
            newCubemap.SetPixels(nTex.GetPixels(), (CubemapFace)i, 0);
            newCubemap.Apply(true);
        }
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return newCubemap;
    }







    public static Texture2D GetReadable(this Texture texture, int W, int H, bool linear = false)
    {
        texture.filterMode = FilterMode.Point;
        RenderTexture rt = RenderTexture.GetTemporary(W, H);
        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(texture, rt);
        Texture2D nTex = new Texture2D(W, H, TextureFormat.ARGB32, true, linear);
        nTex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return nTex;
    }

    public static Texture2D GetReadable(this Texture2D texture, int W, int H)
    {
        texture.filterMode = FilterMode.Point;
        RenderTexture rt = RenderTexture.GetTemporary(W, H);
        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(texture, rt);
        Texture2D nTex = new Texture2D(W, H);
        nTex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return nTex;
    }

#if UNITY_EDITOR
    public static Mesh GetReadable(this Mesh mesh)
    {
        if (ModMapManager.Stop) return null;
        ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Setting {mesh.name} To Readable", ModMapManager.FullProgress);
        Mesh newMesh = new Mesh();
        newMesh.name = mesh.name;
        newMesh.bounds = mesh.bounds;
        newMesh.SetVertices(mesh.vertices);
        newMesh.hideFlags = mesh.hideFlags;
        newMesh.indexBufferTarget = mesh.indexBufferTarget;
        newMesh.indexFormat = mesh.indexFormat;
        newMesh.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            newMesh.SetTriangles(mesh.GetTriangles(i), i);
            newMesh.SetSubMesh(i, mesh.GetSubMesh(i));
        }
        newMesh.normals = mesh.normals;
        newMesh.tangents = mesh.tangents;
        newMesh.uv = mesh.uv;
        newMesh.uv2 = mesh.uv2;
        newMesh.uv3 = mesh.uv3;
        newMesh.uv4 = mesh.uv4;
        newMesh.uv5 = mesh.uv5;
        newMesh.uv6 = mesh.uv6;
        newMesh.uv7 = mesh.uv7;
        newMesh.uv8 = mesh.uv8;
        newMesh.bindposes = mesh.bindposes;
        newMesh.boneWeights = mesh.boneWeights;
        return newMesh;
    }

    public class ComplexMesh
    {

        public List<Triangle>[] triangles = new List<Triangle>[0];
        string name;

        public ComplexMesh(Mesh mesh, bool MergeSubmeshes = false)
        {
            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Converting {mesh.name} To Complex Mesh with {mesh.triangles.Length} Triangles ", ModMapManager.FullProgress);
            triangles = new List<Triangle>[MergeSubmeshes ? 1 : mesh.subMeshCount];
            Vector3[] vertices = mesh.vertices;
            List<Vector2>[] uvs = new List<Vector2>[UVCount];
            for (int i = 0; i < UVCount; i++) { uvs[i] = new List<Vector2>(); mesh.GetUVs(i, uvs[i]); }
            Vector3[] normals = mesh.normals;
            name = mesh.name;
            if (MergeSubmeshes) triangles[0] = new List<Triangle>();
            for (int i = 0; i < triangles.Length; i++)
            {
                if (ModMapManager.Stop) return;
                int[] tris = mesh.GetTriangles(i);

                if (!MergeSubmeshes) triangles[i] = new List<Triangle>();
                for (int j = 0; j < tris.Length; j += 3)
                {
                    Vector2[] uv1 = new Vector2[UVCount];
                    Vector2[] uv2 = new Vector2[UVCount];
                    Vector2[] uv3 = new Vector2[UVCount];
                    for (int k = 0; k < UVCount; k++)
                    {
                        if (uvs[k].Count < vertices.Length) continue;
                        uv1[k] = uvs[k][tris[j]];
                        uv2[k] = uvs[k][tris[j + 1]];
                        uv3[k] = uvs[k][tris[j + 2]];
                    }
                    triangles[MergeSubmeshes ? 0 : i].Add(new Triangle(vertices[tris[j]], vertices[tris[j + 1]], vertices[tris[j + 2]], uv1, uv2, uv3, normals[tris[j]], normals[tris[j + 1]], normals[tris[j + 2]]));
                }
                //Debug.Log($"Added A Submesh From {mesh.name} With {triangles[i].Count} : {(tris.Length/3)}");
            }

        }




        public void ReduceFaces()
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                foreach (Triangle T in triangles[i].ToArray())
                {
                }
            }
        }



        static int[] o210 = new int[] { 2, 1, 0 };
        static int[] o120 = new int[] { 1, 2, 0 };
        /*
        public void FitUVs(){
            Debug.LogWarning($"{name} has Uvs Outside Bounds, This Will Result In Mesh Slicing And Increased Geometry For Texture Packing");
                for (int i = 0; i < triangles.Length; i++)
            {
      int TrisCount=0;          
                
for(int j=0; j<50; j++){
                    if(ModMapManager.Stop)return;
     if(TrisCount!=triangles[i].Count){
         TrisCount = triangles[i].Count;
        // if(TrisCount>68000)break;
     }else break;

        ModMapManager.Stop=EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Slicing Submesh {i} of {name} To Fit Atlas ({j} iteraion{(j<=1?"":"s")} : {TrisCount} Triangles))", ModMapManager.FullProgress);
        //System.Threading.Thread.Sleep(4*1000);
    bool Completed=true;
                ///////X Axis/////////////
                foreach (Triangle T in triangles[i].ToArray())
                {
                    if(T.InUVBounds)continue;
                    Completed=false;
                    if(T.uv[0].x>=1&&T.uv[1].x<=1&&T.uv[2].x<=1){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],true,false))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].x>=1&&T.uv[0].x<=1&&T.uv[2].x<=1){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],true,false,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].x>=1&&T.uv[1].x<=1&&T.uv[0].x<=1){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],true,false,o210))triangles[i].Remove(T);
                    }else
                    
                    if(T.uv[0].x<=1&&T.uv[1].x>=1&&T.uv[2].x>=1){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],true,false))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].x<=1&&T.uv[0].x>=1&&T.uv[2].x>=1){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],true,false,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].x<=1&&T.uv[1].x>=1&&T.uv[0].x>=1){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],true,false,o210))triangles[i].Remove(T);
                    }
                }
        ModMapManager.Stop=EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Slicing Submesh {i} of {name} To Fit Atlas ({j} iteraion{(j<=1?"":"s")} : {TrisCount} Triangles))", ModMapManager.FullProgress);
                    foreach (Triangle T in triangles[i].ToArray())
                {
                    if(T.InUVBounds)continue;
                    Completed=false;
                    if(T.uv[0].x>=0&&T.uv[1].x<=0&&T.uv[2].x<=0){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],false,false))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].x>=0&&T.uv[0].x<=0&&T.uv[2].x<=0){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],false,false,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].x>=0&&T.uv[1].x<=0&&T.uv[0].x<=0){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],false,false,o210))triangles[i].Remove(T);
                    }else
                    
                    if(T.uv[0].x<=0&&T.uv[1].x>=0&&T.uv[2].x>=0){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],false,false))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].x<=0&&T.uv[0].x>=0&&T.uv[2].x>=0){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],false,false,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].x<=0&&T.uv[1].x>=0&&T.uv[0].x>=0){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],false,false,o210))triangles[i].Remove(T);
                    }
                }
                

        ModMapManager.Stop=EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Slicing Submesh {i} of {name} To Fit Atlas ({j} iteraion{(j<=1?"":"s")} : {TrisCount} Triangles))", ModMapManager.FullProgress);
                ///////Y Axis/////////////
                foreach (Triangle T in triangles[i].ToArray())
                {
                    if(T.InUVBounds)continue;
                    Completed=false;
                    if(T.uv[0].y>=1&&T.uv[1].y<=1&&T.uv[2].y<=1){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],true,true))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].y>=1&&T.uv[0].y<=1&&T.uv[2].y<=1){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],true,true,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].y>=1&&T.uv[1].y<=1&&T.uv[0].y<=1){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],true,true,o210))triangles[i].Remove(T);
                    }else
                    
                    if(T.uv[0].y<=1&&T.uv[1].y>=1&&T.uv[2].y>=1){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],true,true))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].y<=1&&T.uv[0].y>=1&&T.uv[2].y>=1){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],true,true,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].y<=1&&T.uv[1].y>=1&&T.uv[0].y>=1){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],true,true,o210))triangles[i].Remove(T);
                    }
                }

        ModMapManager.Stop=EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Slicing Submesh {i} of {name} To Fit Atlas ({j} iteraion{(j<=1?"":"s")} : {TrisCount} Triangles))", ModMapManager.FullProgress);
                    foreach (Triangle T in triangles[i].ToArray())
                {
                    if(T.InUVBounds)continue;
                    Completed=false;
                    if(T.uv[0].y>=0&&T.uv[1].y<=0&&T.uv[2].y<=0){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],false,true))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].y>=0&&T.uv[0].y<=0&&T.uv[2].y<=0){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],false,true,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].y>=0&&T.uv[1].y<=0&&T.uv[0].y<=0){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],false,true,o210))triangles[i].Remove(T);
                    }else
                    
                    if(T.uv[0].y<=0&&T.uv[1].y>=0&&T.uv[2].y>=0){
                        
                        if(triangles[i].SliceTriangle(T.v[0],T.v[1],T.v[2],T.uvs[0],T.uvs[1],T.uvs[2],T.n[0],T.n[1],T.n[2],false,true))triangles[i].Remove(T);
                    }else
                    if(T.uv[1].y<=0&&T.uv[0].y>=0&&T.uv[2].y>=0){
                        
                        if(triangles[i].SliceTriangle(T.v[1],T.v[2],T.v[0],T.uvs[1],T.uvs[2],T.uvs[0],T.n[1],T.n[2],T.n[0],false,true,o120))triangles[i].Remove(T);
                    }else
                    if(T.uv[2].y<=0&&T.uv[1].y>=0&&T.uv[0].y>=0){
                        
                        if(triangles[i].SliceTriangle(T.v[2],T.v[1],T.v[0],T.uvs[2],T.uvs[1],T.uvs[0],T.n[2],T.n[1],T.n[0],false,true,o210))triangles[i].Remove(T);
                    }
                }
                if(Completed)break;
            }
            }
        }

*/

        public void FitUVs()
        {
            Debug.LogWarning($"{name} has Uvs Outside Bounds, This Will Result In Mesh Slicing And Increased Geometry For Texture Packing");
            for (int i = 0; i < triangles.Length; i++)
            {
                int TrisCount = 0;

                for (int j = 0; j < 50; j++)
                {
                    if (ModMapManager.Stop) return;
                    if (TrisCount != triangles[i].Count)
                    {
                        TrisCount = triangles[i].Count;
                        // if(TrisCount>68000)break;
                    }
                    else break;

                    ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Slicing Submesh {i} of {name} To Fit Atlas X ({j} iteraion{(j <= 1 ? "" : "s")} : {TrisCount} Triangles))", ModMapManager.FullProgress);
                    //System.Threading.Thread.Sleep(4*1000);
                    bool Completed = true;
                    ///////X Axis/////////////
                    foreach (Triangle T in triangles[i].ToArray())
                    {
                        if (T.InUVBounds_X) continue;
                        Completed = false;
                        if (T.uv[0].x >= 1 && T.uv[1].x <= 1 && T.uv[2].x <= 1)
                        {

                            if (triangles[i].SliceTriangle(T.v[0], T.v[1], T.v[2], T.uvs[0], T.uvs[1], T.uvs[2], T.n[0], T.n[1], T.n[2], true, false)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[1].x >= 1 && T.uv[0].x <= 1 && T.uv[2].x <= 1)
                        {

                            if (triangles[i].SliceTriangle(T.v[1], T.v[2], T.v[0], T.uvs[1], T.uvs[2], T.uvs[0], T.n[1], T.n[2], T.n[0], true, false, o120)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[2].x >= 1 && T.uv[1].x <= 1 && T.uv[0].x <= 1)
                        {

                            if (triangles[i].SliceTriangle(T.v[2], T.v[1], T.v[0], T.uvs[2], T.uvs[1], T.uvs[0], T.n[2], T.n[1], T.n[0], true, false, o210)) triangles[i].Remove(T);
                        }
                        else

                        if (T.uv[0].x <= 1 && T.uv[1].x >= 1 && T.uv[2].x >= 1)
                        {

                            if (triangles[i].SliceTriangle(T.v[0], T.v[1], T.v[2], T.uvs[0], T.uvs[1], T.uvs[2], T.n[0], T.n[1], T.n[2], true, false)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[1].x <= 1 && T.uv[0].x >= 1 && T.uv[2].x >= 1)
                        {

                            if (triangles[i].SliceTriangle(T.v[1], T.v[2], T.v[0], T.uvs[1], T.uvs[2], T.uvs[0], T.n[1], T.n[2], T.n[0], true, false, o120)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[2].x <= 1 && T.uv[1].x >= 1 && T.uv[0].x >= 1)
                        {

                            if (triangles[i].SliceTriangle(T.v[2], T.v[1], T.v[0], T.uvs[2], T.uvs[1], T.uvs[0], T.n[2], T.n[1], T.n[0], true, false, o210)) triangles[i].Remove(T);
                        }
                    }
                    ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Slicing Submesh {i} of {name} To Fit Atlas X ({j} iteraion{(j <= 1 ? "" : "s")} : {TrisCount} Triangles))", ModMapManager.FullProgress);
                    foreach (Triangle T in triangles[i].ToArray())
                    {
                        if (T.InUVBounds_X) continue;
                        Completed = false;
                        if (T.uv[0].x >= 0 && T.uv[1].x <= 0 && T.uv[2].x <= 0)
                        {

                            if (triangles[i].SliceTriangle(T.v[0], T.v[1], T.v[2], T.uvs[0], T.uvs[1], T.uvs[2], T.n[0], T.n[1], T.n[2], false, false)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[1].x >= 0 && T.uv[0].x <= 0 && T.uv[2].x <= 0)
                        {

                            if (triangles[i].SliceTriangle(T.v[1], T.v[2], T.v[0], T.uvs[1], T.uvs[2], T.uvs[0], T.n[1], T.n[2], T.n[0], false, false, o120)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[2].x >= 0 && T.uv[1].x <= 0 && T.uv[0].x <= 0)
                        {

                            if (triangles[i].SliceTriangle(T.v[2], T.v[1], T.v[0], T.uvs[2], T.uvs[1], T.uvs[0], T.n[2], T.n[1], T.n[0], false, false, o210)) triangles[i].Remove(T);
                        }
                        else

                        if (T.uv[0].x <= 0 && T.uv[1].x >= 0 && T.uv[2].x >= 0)
                        {

                            if (triangles[i].SliceTriangle(T.v[0], T.v[1], T.v[2], T.uvs[0], T.uvs[1], T.uvs[2], T.n[0], T.n[1], T.n[2], false, false)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[1].x <= 0 && T.uv[0].x >= 0 && T.uv[2].x >= 0)
                        {

                            if (triangles[i].SliceTriangle(T.v[1], T.v[2], T.v[0], T.uvs[1], T.uvs[2], T.uvs[0], T.n[1], T.n[2], T.n[0], false, false, o120)) triangles[i].Remove(T);
                        }
                        else
                        if (T.uv[2].x <= 0 && T.uv[1].x >= 0 && T.uv[0].x >= 0)
                        {

                            if (triangles[i].SliceTriangle(T.v[2], T.v[1], T.v[0], T.uvs[2], T.uvs[1], T.uvs[0], T.n[2], T.n[1], T.n[0], false, false, o210)) triangles[i].Remove(T);
                        }
                    }

                    if (Completed) break;
                }
            }
        }
        public Mesh ToMesh()
        {
            // Create a new Mesh object
            Mesh mesh = new Mesh();
            mesh.name = name;
            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Rebuilding {name}", ModMapManager.FullProgress);

            // Create lists to store the vertices, triangles, UVs, and normals of the new mesh
            List<Vector3> newVertices = new List<Vector3>();
            List<int>[] newTriangles = new List<int>[triangles.Length];
            List<Vector2>[] newUVs = new List<Vector2>[UVCount];
            for (int i = 0; i < UVCount; i++) { newUVs[i] = new List<Vector2>(); }
            List<Vector3> newNormals = new List<Vector3>();
            mesh.subMeshCount = triangles.Length;
            // Loop through the submeshes of the ComplexMesh object
            for (int i = 0; i < triangles.Length; i++)
            {
                if (ModMapManager.Stop) return null;
                // Get the list of triangles of the current submesh
                List<Triangle> submeshTriangles = triangles[i];
                newTriangles[i] = new List<int>();
                // Loop through the triangles of the current submesh
                for (int j = 0; j < submeshTriangles.Count; j++)
                {
                    // Get the current triangle
                    Triangle tri = submeshTriangles[j];
                    int v1Index = -1;
                    int v2Index = -1;
                    int v3Index = -1;
                    for (int k = 0; k < newVertices.Count; k++)
                    {
                        if (v1Index == -1 && (newVertices[k] - tri.v[0]).sqrMagnitude < 0.00001f && (newNormals[k] - tri.n[0]).sqrMagnitude < 0.00001f && (tri.uvs[0].Length <= 0 || (newUVs[0][k] - tri.uvs[0][0]).sqrMagnitude < 0.00001f)) v1Index = k;
                        else
                        if (v2Index == -1 && (newVertices[k] - tri.v[1]).sqrMagnitude < 0.00001f && (newNormals[k] - tri.n[1]).sqrMagnitude < 0.00001f && (tri.uvs[1].Length <= 0 || (newUVs[0][k] - tri.uvs[1][0]).sqrMagnitude < 0.00001f)) v2Index = k;
                        else
                        if (v3Index == -1 && (newVertices[k] - tri.v[2]).sqrMagnitude < 0.00001f && (newNormals[k] - tri.n[2]).sqrMagnitude < 0.00001f && (tri.uvs[2].Length <= 0 || (newUVs[0][k] - tri.uvs[2][0]).sqrMagnitude < 0.00001f)) v3Index = k;
                        else
                        if (v1Index != -1 && v2Index != -1 && v3Index != -1) break;
                    }

                    // Add the vertex positions, UVs, and normals of the triangle to the lists
                    if (v1Index == -1) { newVertices.Add(tri.v[0]); newNormals.Add(tri.n[0]); if (tri.uvs[0].Length > 0) for (int k = 0; k < UVCount; k++) newUVs[k].Add(tri.uvs[0][k]); v1Index = newVertices.Count - 1; }
                    if (v2Index == -1) { newVertices.Add(tri.v[1]); newNormals.Add(tri.n[1]); if (tri.uvs[1].Length > 0) for (int k = 0; k < UVCount; k++) newUVs[k].Add(tri.uvs[1][k]); v2Index = newVertices.Count - 1; }
                    if (v3Index == -1) { newVertices.Add(tri.v[2]); newNormals.Add(tri.n[2]); if (tri.uvs[2].Length > 0) for (int k = 0; k < UVCount; k++) newUVs[k].Add(tri.uvs[2][k]); v3Index = newVertices.Count - 1; }

                    // Add the vertex indices of the triangle to the list, using the current size of the newVertices list as the base index
                    newTriangles[i].Add(v1Index);
                    newTriangles[i].Add(v2Index);
                    newTriangles[i].Add(v3Index);
                }

                // Set the submesh index of the new mesh

                // Set the triangles of the current submesh of the new mesh
            }

            // Set the vertices, UVs, and normals of the new mesh
            mesh.vertices = newVertices.ToArray();
            for (int i = 0; i < UVCount; i++) if (newUVs[i].Count == newVertices.Count) foreach (Vector2 V2 in newUVs[i]) { mesh.SetUVs(i, newUVs[i]); break; }
            mesh.normals = newNormals.ToArray();
            for (int i = 0; i < triangles.Length; i++)
            {
                if (ModMapManager.Stop) return null;
                mesh.SetTriangles(newTriangles[i].ToArray(), i);
            }
            return mesh;
        }

        public Mesh ToShadowMesh()
        {
            // Create a new Mesh object
            Mesh mesh = new Mesh();
            mesh.name = name;

            int TrisCount = 0;
            for (int i = 0; i < triangles.Length; i++)
            {
                if (TrisCount != triangles[i].Count)
                {
                    TrisCount = triangles[i].Count;
                }
            }

            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Shadow Meshes", $"Rebuilding {name} With {TrisCount} Triangles", ModMapManager.FullProgress);
            if (ModMapManager.Stop) return null;

            // Create lists to store the vertices, triangles, UVs, and normals of the new mesh
            List<Vector3> newVertices = new List<Vector3>();
            List<int>[] newTriangles = new List<int>[triangles.Length];
            mesh.subMeshCount = triangles.Length;
            // Loop through the submeshes of the ComplexMesh object
            int steps = TrisCount / Mathf.Min(TrisCount, 100);
            for (int i = 0; i < triangles.Length; i++)
            {
                if (ModMapManager.Stop) return null;
                // Get the list of triangles of the current submesh
                List<Triangle> submeshTriangles = triangles[i];
                newTriangles[i] = new List<int>();
                // Loop through the triangles of the current submesh
                for (int j = 0; j < submeshTriangles.Count; j++)
                {
                    if (j % steps == 0)
                    {
                        ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Shadow Meshes", $"Rebuilding {mesh.name}   {j}/{TrisCount} Triangles", (float)j / TrisCount);
                        if (ModMapManager.Stop) return null;
                    }
                    // Get the current triangle
                    Triangle tri = submeshTriangles[j];
                    int v1Index = -1;
                    int v2Index = -1;
                    int v3Index = -1;
                    /*for(int k=0; k<newVertices.Count; k++){
                        Vector3 nv=newVertices[k];
                        if(v1Index==-1&&FastDist(nv,tri.v[0])<0.0001f)v1Index=k;else
                        if(v2Index==-1&&FastDist(nv,tri.v[1])<0.0001f)v2Index=k;else
                        if(v3Index==-1&&FastDist(nv,tri.v[2])<0.0001f)v3Index=k;else
                        if(v1Index>0&&v2Index>0&&v3Index>0)break;
                    }*/

                    // Add the vertex positions, UVs, and normals of the triangle to the lists
                    /*if(v1Index==-1){*/
                    newVertices.Add(tri.v[0]); v1Index = newVertices.Count - 1;//}
                    /*if(v2Index==-1){*/
                    newVertices.Add(tri.v[1]); v2Index = newVertices.Count - 1;//}
                    /*if(v3Index==-1){*/
                    newVertices.Add(tri.v[2]); v3Index = newVertices.Count - 1;//}

                    // Add the vertex indices of the triangle to the list, using the current size of the newVertices list as the base index
                    newTriangles[i].Add(v1Index);
                    newTriangles[i].Add(v2Index);
                    newTriangles[i].Add(v3Index);
                }
            }

            // Set the vertices, UVs, and normals of the new mesh
            mesh.SetVertices(newVertices);
            for (int i = 0; i < triangles.Length; i++)
            {
                if (ModMapManager.Stop) return null;
                mesh.SetTriangles(newTriangles[i].ToArray(), i);
            }
            MeshSimplifier MS = new MeshSimplifier();
            MS.Initialize(mesh);
            SimplificationOptions SO = new SimplificationOptions();
            SO.PreserveBorderEdges = true;
            SO.VertexLinkDistance = 0.01f;
            SO.PreserveSurfaceCurvature = true;
            SO.PreserveUVSeamEdges = true;
            SO.MaxIterationCount = 10;
            SO.Agressiveness = 15;
            MS.SimplificationOptions = SO;
            MS.SimplifyMesh(0.98f);
            mesh = MS.ToMesh();
            mesh.name = name;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.OptimizeReorderVertexBuffer();
            mesh.OptimizeIndexBuffers();
            mesh.Optimize();
            return mesh;
        }


    }

    public class Triangle
    {
        public Vector3[] v = new Vector3[3];
        public Vector2[][] uvs = new Vector2[3][];
        public Vector2[] uv = new Vector2[3];
        public Vector3[] n = new Vector3[3];

        public bool InUVBounds_X = true;
        public bool InUVBounds_Y = true;
        public bool InUVBounds;

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector2[] uv1, Vector2[] uv2, Vector2[] uv3, Vector3 n1, Vector3 n2, Vector3 n3, int[] od = null)
        {
            if (od == null)
            {
                v[0] = v1;
                v[1] = v2;
                v[2] = v3;

                uvs[0] = uv1.ToArray();
                uvs[1] = uv2.ToArray();
                uvs[2] = uv3.ToArray();

                n[0] = n1;
                n[1] = n2;
                n[2] = n3;
            }
            else
            {
                v[od[0]] = v1;
                v[od[1]] = v2;
                v[od[2]] = v3;

                uvs[od[0]] = uv1.ToArray();
                uvs[od[1]] = uv2.ToArray();
                uvs[od[2]] = uv3.ToArray();

                n[od[0]] = n1;
                n[od[1]] = n2;
                n[od[2]] = n3;
            }
            if (uvs[0].Length > 0)
            {
                uv[0] = uvs[0][0];
                uv[1] = uvs[1][0];
                uv[2] = uvs[2][0];

                for (int i = 0; i < 3; i++)
                {
                    if (!(uv[i].x >= -0.01f && uv[i].x <= 1.01f)) { InUVBounds_X = false; }
                    if (!(uv[i].y >= -0.01f && uv[i].y <= 1.01f)) { InUVBounds_Y = false; }
                    if (!InUVBounds_X && !InUVBounds_Y) break;
                }
                if (!InUVBounds)
                {
                    Vector2 center = (uv[0] + uv[1] + uv[2]) / 3;
                    Vector2 offset = new Vector2(Mathf.Repeat(center.x, 1), Mathf.Repeat(center.y, 1));
                    Vector2 adder = offset - center;
                    uvs[0][0] += adder;
                    uvs[1][0] += adder;
                    uvs[2][0] += adder;
                    uv[0] += adder;
                    uv[1] += adder;
                    uv[2] += adder;

                    InUVBounds = true;
                    for (int i = 0; i < 3; i++)
                    {
                        if (!(uv[i].x >= -0.01f && uv[i].x <= 1.01f)) { InUVBounds_X = false; }
                        if (!(uv[i].y >= -0.01f && uv[i].y <= 1.01f)) { InUVBounds_Y = false; }
                        if (!InUVBounds_X && !InUVBounds_Y) break;
                    }
                }
                InUVBounds = InUVBounds_X && InUVBounds_Y;
            }
        }

        public void Flip()
        {
            Vector3 tempV = v[0];
            v[0] = v[2];
            v[2] = tempV;

            Vector3 tempN = n[0];
            n[0] = n[2];
            n[2] = tempN;
        }

    }


    static bool SliceTriangle(this List<Triangle> SubMesh, Vector3 v1, Vector3 v2, Vector3 v3, Vector2[] uv1, Vector2[] uv2, Vector2[] uv3, Vector3 n1, Vector3 n2, Vector3 n3, bool sliceMax, bool Yaxis, int[] od = null)
    {
        //if(!IsValidTriangle(uv1[0],uv2[0],uv3[0]))return false;
        //else if(!IsValidTriangle(v1,v2,v3,1e-8))return false;
        if (od == null) od = new int[] { 0, 1, 2 };
        int limit = sliceMax ? 1 : 0;
        float t1 = Yaxis ? ((limit - uv1[0].y) / (uv2[0].y - uv1[0].y)) : ((limit - uv1[0].x) / (uv2[0].x - uv1[0].x));
        float t2 = Yaxis ? ((limit - uv1[0].y) / (uv3[0].y - uv1[0].y)) : ((limit - uv1[0].x) / (uv3[0].x - uv1[0].x));

        Vector3 p1 = Vector3.LerpUnclamped(v1, v2, t1);
        Vector3 p2 = Vector3.LerpUnclamped(v1, v3, t2);

        Vector2[] uvp1 = new Vector2[UVCount];
        Vector2[] uvp2 = new Vector2[UVCount];

        for (int i = 0; i < UVCount; i++)
        {
            if (uv1[i] == null) continue;
            uvp1[i] = Vector2.LerpUnclamped(uv1[i], uv2[i], t1);
            uvp2[i] = Vector2.LerpUnclamped(uv1[i], uv3[i], t2);
        }

        Vector3 pn1 = Vector3.Lerp(n1, n2, t1);
        Vector3 pn2 = Vector3.Lerp(n1, n3, t2);

        if (IsValidTriangle(v1, p1, p2)) SubMesh.Add(new Triangle(v1, p1, p2, uv1, uvp1, uvp2, n1, pn1, pn2, od));
        if (IsValidTriangle(p1, v2, p2)) SubMesh.Add(new Triangle(p1, v2, p2, uvp1, uv2, uvp2, pn1, n2, pn2, od));
        if (IsValidTriangle(p2, v2, v3)) SubMesh.Add(new Triangle(p2, v2, v3, uvp2, uv2, uv3, pn2, n2, n3, od));
        return true;
    }

    public static bool IsValidTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 edge1 = v2 - v1;
        Vector3 edge2 = v3 - v1;
        if (0.5 * Vector3.Cross(edge1, edge2).magnitude < 0.000001f) return false;
        Vector3 edge3 = v3 - v2;

        double angle1 = Math.Acos(Math.Min(1.0, Math.Max(-1.0, Vector3.Dot(edge1.normalized, edge2.normalized))));
        if (angle1 <= 0.001) return false;
        double angle2 = Math.Acos(Math.Min(1.0, Math.Max(-1.0, Vector3.Dot(-edge1.normalized, edge3.normalized))));
        if (angle2 <= 0.001) return false;
        double angle3 = Math.Acos(Math.Min(1.0, Math.Max(-1.0, Vector3.Dot(-edge2.normalized, -edge3.normalized))));
        return angle3 > 0.0001;


    }

    public static Mesh GetSlicedMesh(this Mesh mesh)
    {
        for (int i = 0; i < 8; i++) { List<Vector2> UVs = new List<Vector2>(); mesh.GetUVs(i, UVs); if (UVs == null || UVs.Count < mesh.vertexCount) { UVCount = i; break; } }
        ComplexMesh CM = new ComplexMesh(mesh);
        CM.FitUVs();
        //CM.ReduceFaces();
        if (ModMapManager.Stop) return null;
        return CM.ToMesh();
    }
#endif
    public static int UVCount;

    public static int ToPowerOf2(int number)
    {
        int result = 1;
        while (result < number)
        {
            result <<= 1; // Left shift to multiply by 2
        }

        // Check for the closest power of 2
        if (result - number > number - (result >> 1))
        {
            result >>= 1; // Right shift to divide by 2
        }

        return result;
    }

#if UNITY_EDITOR
    public static void SetTextureFormat(string Path, TextureImporterFormat format, float mipmapBias = 0)
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(Path) as TextureImporter;

        if (textureImporter != null)
        {
            TextureImporterPlatformSettings androidSettings = textureImporter.GetPlatformTextureSettings("Android");
            // Set the texture format to ASTC
            if (androidSettings.format == format && textureImporter.mipMapBias == mipmapBias) return;
            textureImporter.textureCompression = TextureImporterCompression.Compressed;
            textureImporter.mipMapBias = mipmapBias;
            androidSettings.format = format;
            androidSettings.overridden = true;
            textureImporter.SetPlatformTextureSettings(androidSettings);
            textureImporter.SaveAndReimport();
            // Apply the changes
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
        }
        else
        {
            Debug.LogError("Failed to get TextureImporter for path: " + Path);
        }
    }

    public static Color ToLinear(this Color sRGBColor, bool convert)
    {
        if (!convert) return sRGBColor;
        return new Color(
            Mathf.Pow(sRGBColor.r, 1.0f / 2.2f),
            Mathf.Pow(sRGBColor.g, 1.0f / 2.2f),
            Mathf.Pow(sRGBColor.b, 1.0f / 2.2f),
            sRGBColor.a
        );
    }

    public static bool IsColorTexture(Texture texture)
    {
        string assetPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (textureImporter != null)
        {
            if (textureImporter.sRGBTexture)
            {
                //Debug.Log("Texture is set to use sRGB color space.");
                return true; // Texture importer found
            }
            else
            {
                // Debug.Log("Texture is set to use Linear color space.");
                return false; // Texture importer found
            }

        }
        else
        {
            Debug.LogWarning("Could not retrieve TextureImporter. This might not be a Texture2D asset.");
            return true; // Texture importer not found
        }
    }

    public static void AutoWeld(Mesh mesh, float Posthreshold = 0.0001f, float Angthreshold = 10, float bucketStep = 20)
    {

        Vector3[] oldVertices = mesh.vertices;
        Vector3[] oldNormals = mesh.normals;
        Vector3[] newVertices = new Vector3[oldVertices.Length];
        Vector3[] newNormals = new Vector3[oldNormals.Length];
        int[] old2new = new int[oldVertices.Length];
        int newSize = 0;

        // Find AABB
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < oldVertices.Length; i++)
        {
            if (oldVertices[i].x < min.x) min.x = oldVertices[i].x;
            if (oldVertices[i].y < min.y) min.y = oldVertices[i].y;
            if (oldVertices[i].z < min.z) min.z = oldVertices[i].z;
            if (oldVertices[i].x > max.x) max.x = oldVertices[i].x;
            if (oldVertices[i].y > max.y) max.y = oldVertices[i].y;
            if (oldVertices[i].z > max.z) max.z = oldVertices[i].z;
        }

        // Make cubic buckets, each with dimensions "bucketStep"
        int bucketSizeX = Mathf.FloorToInt((max.x - min.x) / bucketStep) + 1;
        int bucketSizeY = Mathf.FloorToInt((max.y - min.y) / bucketStep) + 1;
        int bucketSizeZ = Mathf.FloorToInt((max.z - min.z) / bucketStep) + 1;
        List<int>[,,] buckets = new List<int>[bucketSizeX, bucketSizeY, bucketSizeZ];

        int vertCount = oldVertices.Length;
        int steps = vertCount / Mathf.Min(vertCount, 100);
        // Make new vertices
        for (int i = 0; i < vertCount; i++)
        {
            if (i % steps == 0)
            {
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Shadow Meshes", $"Merging {mesh.name}   {i}/{vertCount} vertices", (float)i / vertCount);
                if (ModMapManager.Stop) return;
            }
            // Determine which bucket it belongs to
            int x = Mathf.FloorToInt((oldVertices[i].x - min.x) / bucketStep);
            int y = Mathf.FloorToInt((oldVertices[i].y - min.y) / bucketStep);
            int z = Mathf.FloorToInt((oldVertices[i].z - min.z) / bucketStep);

            // Check to see if it's already been added
            if (buckets[x, y, z] == null)
                buckets[x, y, z] = new List<int>(); // Make buckets lazily

            for (int j = 0; j < buckets[x, y, z].Count; j++)
            {
                int otherIndex = buckets[x, y, z][j];
                Vector3 to = newVertices[otherIndex] - oldVertices[i];

                if (Vector3.SqrMagnitude(to) < Posthreshold)
                {
                    float angle = Vector3.Angle(oldNormals[i], newNormals[otherIndex]);
                    if (angle < Angthreshold)
                    {
                        old2new[i] = otherIndex;
                        goto skip; // Skip to the next old vertex if this one is already there
                    }
                }
            }

            // Add a new vertex
            newVertices[newSize] = oldVertices[i];
            newNormals[newSize] = oldNormals[i];
            buckets[x, y, z].Add(newSize);
            old2new[i] = newSize;
            newSize++;

        skip:;
        }

        // Make new triangles
        int[] oldTris = mesh.triangles;
        int[] newTris = new int[oldTris.Length];

        for (int i = 0; i < oldTris.Length; i++)
        {
            newTris[i] = old2new[oldTris[i]];
        }

        Vector3[] finalVertices = new Vector3[newSize];
        Vector3[] finalNormals = new Vector3[newSize];
        for (int i = 0; i < newSize; i++)
        {
            finalVertices[i] = newVertices[i];
            finalNormals[i] = newNormals[i];
        }

        mesh.Clear();
        mesh.vertices = finalVertices;
        mesh.normals = finalNormals;
        mesh.triangles = newTris;
        mesh.RecalculateNormals();
        mesh.Optimize();
    }




    public static void RemoveInvalidTriangles(Mesh mesh, float angleThreshold = 5)
    {

        if (mesh == null)
        {
            Debug.LogError("Mesh is null!");
            return;
        }

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // List to store simplified triangles
        var ValidTriangles = new List<int>();

        // Iterate through each vertex
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            if (IsValidTriangle(v1, v2, v3))
            {
                ValidTriangles.Add(triangles[i]);
                ValidTriangles.Add(triangles[i + 1]);
                ValidTriangles.Add(triangles[i + 2]);
            }
        }


        // Create a new mesh with the simplified triangles
        mesh.vertices = vertices;
        mesh.triangles = ValidTriangles.ToArray();
    }



    public static Mesh RemoveBackfacesAndFlip(this Mesh mesh, Transform T)
    {
        if (mesh == null) return null;

        int triangleCount = mesh.triangles.Length / 3;
        ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Shadow Meshes", $"Removing Backfaces From {mesh.name} With {triangleCount} Triangles", ModMapManager.FullProgress);

        // Get the direction
        Vector3 direction = -T.forward;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Create new lists for modified vertices and triangles
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        int steps = triangleCount / Mathf.Min(triangleCount, 100);

        for (int j = 0; j < triangleCount; j++)
        {
            if (j % steps == 0)
            {
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Shadow Meshes", $"Removing Backfaces Faces From {mesh.name}   {j}/{triangleCount} Triangles", (float)j / triangleCount);
                if (ModMapManager.Stop) return null;
            }

            int baseIndex = j * 3;
            Vector3 v0 = vertices[triangles[baseIndex]];
            Vector3 v1 = vertices[triangles[baseIndex + 1]];
            Vector3 v2 = vertices[triangles[baseIndex + 2]];

            // Calculate the normal
            Vector3 triNorm = CalculateTriangleNormal(v0, v1, v2);
            float dot = Vector3.Dot(triNorm, direction);

            if (dot > 0)
            {
                // Include the vertices and triangles for faces with normals facing the given direction
                newVertices.Add(v0);
                newVertices.Add(v2);
                newVertices.Add(v1);

                newTriangles.Add(newVertices.Count - 3);
                newTriangles.Add(newVertices.Count - 2);
                newTriangles.Add(newVertices.Count - 1);
            }
        }

        // Create a new mesh and assign the modified vertices and triangles
        Mesh modifiedMesh = new Mesh();
        if (newVertices.Count >= ushort.MaxValue) modifiedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        modifiedMesh.vertices = newVertices.ToArray();
        modifiedMesh.triangles = newTriangles.ToArray();

        // Recalculate normals and other necessary data
        modifiedMesh.name = mesh.name;
        modifiedMesh.Optimize();

        return modifiedMesh;
    }

    public static Vector3 CalculateTriangleNormal(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Vector3.Cross(p1 - p0, p2 - p0).normalized;
    }
    public class ShadowMesh
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<int> triangleindices = new List<int>();
    }

    static RaycastHit hit = new RaycastHit();
    public static Mesh[] RemoveOccludedFacesAndFlip(this Mesh mesh, Transform T, Collider coll, int iterations, int GroupSize)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        if (mesh == null) return null;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].x < min.x) min.x = vertices[i].x;
            if (vertices[i].z < min.z) min.z = vertices[i].z;

            if (vertices[i].x > max.x) max.x = vertices[i].x;
            if (vertices[i].z > max.z) max.z = vertices[i].z;
        }

        //Debug.Log($"Min : {min}");
        //Debug.Log($"Max : {max}");

        int bucketSizeX = Mathf.FloorToInt((max.x - min.x) / GroupSize) + 1;
        int bucketSizeZ = Mathf.FloorToInt((max.z - min.z) / GroupSize) + 1;


        ShadowMesh[,] buckets = new ShadowMesh[bucketSizeX, bucketSizeZ];

        int triangleCount = mesh.triangles.Length / 3;
        ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Shadow Meshes", $"Removing Occluded Faces From {mesh.name} With {triangleCount} Triangles", 0);

        // Get the direction
        Vector3 direction = -T.forward;



        // Create new lists for modified vertices and triangles
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        int steps = triangleCount / Mathf.Min(triangleCount, 100);

        for (int j = 0; j < triangleCount; j++)
        {
            if (j % steps == 0)
            {
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Shadow Meshes", $"Removing Occluded Faces From {mesh.name}  {j}/{triangleCount} Triangles", (float)j / triangleCount);
                if (ModMapManager.Stop) return null;
            }


            int baseIndex = j * 3;
            Vector3 v0 = vertices[triangles[baseIndex]];
            Vector3 v1 = vertices[triangles[baseIndex + 1]];
            Vector3 v2 = vertices[triangles[baseIndex + 2]];

            float area = CalculateTriangleSize(v0, v1, v2);
            int subdivs = (int)Mathf.Lerp(1, 25, area / 10);

            bool invalid = RaycastFromPointsInTriangle(v0, v1, v2, subdivs, coll, direction);

            if (!invalid)
            {
                Vector3 center = (v0 + v1 + v2) / 3;

                int x = Mathf.Clamp(Mathf.FloorToInt((center.x - min.x) / GroupSize), 0, bucketSizeX);
                int z = Mathf.Clamp(Mathf.FloorToInt((center.z - min.z) / GroupSize), 0, bucketSizeZ);
                if (buckets[x, z] == null)
                    buckets[x, z] = new ShadowMesh();

                ShadowMesh SM = buckets[x, z];

                // Include the vertices and triangles for non-occluded faces
                SM.vertices.Add(v0);
                SM.vertices.Add(v2);
                SM.vertices.Add(v1);
                Vector3 norm = CalculateTriangleNormal(v0, v2, v1);

                SM.normals.Add(norm);
                SM.normals.Add(norm);
                SM.normals.Add(norm);

                SM.triangleindices.Add(SM.vertices.Count - 3);
                SM.triangleindices.Add(SM.vertices.Count - 2);
                SM.triangleindices.Add(SM.vertices.Count - 1);
            }
        }

        // Create a new mesh and assign the modified vertices and triangles
        List<Mesh> modifiedMeshs = new List<Mesh>();
        int index = 0;
        foreach (ShadowMesh SM in buckets)
        {
            if (SM == null || SM.vertices.Count <= 2 || SM.triangleindices.Count <= 0) continue;
            Mesh modifiedMesh = new Mesh();
            if (SM.vertices.Count >= ushort.MaxValue) modifiedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            modifiedMesh.vertices = SM.vertices.ToArray();
            modifiedMesh.normals = SM.normals.ToArray();
            modifiedMesh.triangles = SM.triangleindices.ToArray();
            index++;
            modifiedMesh.name = $"Shadows_{index}";
            // Recalculate normals and other necessary data
            AutoWeld(modifiedMesh, 0.005f, 5, 20);
            RemoveInvalidTriangles(modifiedMesh);
            MeshSimplifier MS = new MeshSimplifier();
            MS.Initialize(modifiedMesh);
            SimplificationOptions SO = new SimplificationOptions();
            SO.PreserveBorderEdges = true;
            SO.VertexLinkDistance = 0.05f;
            SO.PreserveSurfaceCurvature = false;
            SO.PreserveUVSeamEdges = false;
            SO.MaxIterationCount = 100;
            SO.Agressiveness = 50;
            MS.SimplificationOptions = SO;
            MS.SimplifyMeshLossless();
            modifiedMesh = MS.ToMesh();
            modifiedMesh.name = $"Shadows_{index}";
            LimitedDissolve(modifiedMesh, 5, iterations);

            modifiedMesh.name = mesh.name;
            modifiedMesh.RecalculateBounds();
            modifiedMesh.OptimizeReorderVertexBuffer();
            modifiedMesh.OptimizeIndexBuffers();
            modifiedMesh.Optimize();
            if (modifiedMesh.vertexCount > 2 && modifiedMesh.triangles.Length > 1) modifiedMeshs.Add(modifiedMesh);
        }
        return modifiedMeshs.ToArray();
    }

    public static bool AreFaceAnglesAboveThreshold(Vector3[] vertices, int[] triangles, float angleThreshold)
    {
        int triangleCount = triangles.Length / 3;
        float count = 0;
        float Avg = 0;

        for (int i = 0; i < triangleCount - 1; i++)
        {
            int v1 = triangles[i * 3];
            int v2 = triangles[i * 3 + 1];
            int v3 = triangles[i * 3 + 2];

            Vector3 normal = Vector3.Cross(vertices[v2] - vertices[v1], vertices[v3] - vertices[v1]).normalized;

            for (int j = i + 1; j < triangleCount; j++)
            {
                int v4 = triangles[j * 3];
                int v5 = triangles[j * 3 + 1];
                int v6 = triangles[j * 3 + 2];

                Vector3 normal2 = Vector3.Cross(vertices[v5] - vertices[v4], vertices[v6] - vertices[v4]).normalized;

                float angle = Vector3.Angle(normal, normal2);
                //Debug.Log("Angle : "+angle);
                count++;
                Avg += angle;

                // Check if the angle between face normals is below the threshold
                if (angle > angleThreshold)
                {
                    return true;
                }
            }
        }

        //Debug.Log("Angle Average : "+(Avg/count));
        return false;
    }

    static HashSet<int> InvalidVertices = new HashSet<int>();
    public static void LimitedDissolve(Mesh originalMesh, float angleThreshold, float iterations = 65000)
    {
        Vector3[] vertices = originalMesh.vertices;
        int[] triArray = originalMesh.triangles;
        List<int> triList = new List<int>(triArray);
        int[] old2new = new int[vertices.Length];
        for (int i = 0; i < old2new.Length; i++) old2new[i] = i;
        int removedVertices = 0;
        int VertexCount = vertices.Length;
        int mode = 0;
        int retries = 0;
        // List to store simplified triangles
        //int steps = (int)(iterations / Mathf.Min(iterations,100));
        InvalidVertices.Clear();
        int steps = 20;
        for (int i = 0; i < iterations; i++)
        {
            bool removed = false;
            if (i % steps == 0)
            {
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"({retries})Processing Shadow Meshes (Iteration : {i})", $"Dissolving {(mode == 0 ? "Edges" : "Faces")} From {originalMesh.name}  {VertexCount - removedVertices}/{VertexCount} verts", (VertexCount - removedVertices) / (VertexCount + 1));
                if (ModMapManager.Stop) return;
            }

            // Iterate through each vertex
            for (int vertexIndex = 0; vertexIndex < VertexCount; vertexIndex++)
            {
                if (InvalidVertices.Contains(vertexIndex)) continue;
                int mergeIndex = -1;
                // Get triangles connected to the current vertex
                int[] connectedTriangles = GetConnectedTriangles(vertices, vertexIndex, triArray, out mergeIndex);
                if (connectedTriangles.Length < 2)
                {
                    InvalidVertices.Add(vertexIndex);
                    continue;
                }
                if (mode == 0 || mode == 2)
                {
                    mergeIndex = GetDissolvePoint(connectedTriangles, vertices, vertexIndex, angleThreshold);
                    if (mergeIndex != -1)
                    {
                        old2new[vertexIndex] = mergeIndex;
                        InvalidVertices.Add(vertexIndex);
                        removed = true;
                        mode = 0;
                        break;
                    }
                }
                else
                if (mode == 1 || mode == 2)
                {
                    if (IsClosedLoop(connectedTriangles) == 0)
                    {
                        //Debug.Log("Is Closed Loop");
                        if (!AreFaceAnglesAboveThreshold(vertices, connectedTriangles, angleThreshold))
                        {
                            old2new[vertexIndex] = mergeIndex;
                            removed = true;
                            //                    Debug.Log("Removed A Closed Vertex At Index : "+vertexIndex+" With "+(connectedTriangles.Length/3)+" Triangles");
                            InvalidVertices.Add(vertexIndex);
                            if (mode == 2) mode = 0;
                            break;
                        }
                    }
                }
                InvalidVertices.Add(vertexIndex);
            }
            int offset = 0;
            // Include triangles around the vertex in the simplified mesh
            for (int j = 0; j < triArray.Length; j += 3)
            {
                // Update the indices using the old2new mapping
                triList[j + offset] = old2new[triArray[j]];
                triList[j + 1 + offset] = old2new[triArray[j + 1]];
                triList[j + 2 + offset] = old2new[triArray[j + 2]];

                // Check if the updated triangle is valid
                if (!IsValidTriangle(vertices[triArray[j]], vertices[triArray[j + 1]], vertices[triArray[j + 2]]))
                {
                    // If not valid, remove the entire triangle
                    triList.RemoveAt(j + 2 + offset);
                    triList.RemoveAt(j + 1 + offset);
                    triList.RemoveAt(j + offset);
                    offset -= 3;

                }
            }
            triArray = triList.ToArray();
            for (int j = 0; j < old2new.Length; j++) old2new[j] = j;
            if (!removed) { mode++; if (mode == 2) retries++; InvalidVertices.Clear(); } else removedVertices++;
            if (mode > 2) break;
        }



        originalMesh.vertices = vertices;
        originalMesh.triangles = triArray;
    }


    public static int[] GetConnectedTriangles(Vector3[] vertices, int vertexIndex, int[] triangles, out int mergeindex)
    {
        // List to store triangles connected to the vertex
        List<int> connectedTriangles = new List<int>();
        HashSet<int> SurroundingVertices = new HashSet<int>();
        Vector3 referenceVector = Vector3.zero;
        bool foundRefVector = false;

        mergeindex = -1;
        // Iterate through triangles to find those connected to the vertex
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (triangles[i] == vertexIndex || triangles[i + 1] == vertexIndex || triangles[i + 2] == vertexIndex)
            {
                int i1 = triangles[i];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                if (i1 != vertexIndex) SurroundingVertices.Add(i1);
                if (i2 != vertexIndex) SurroundingVertices.Add(i2);
                if (i3 != vertexIndex) SurroundingVertices.Add(i3);

                connectedTriangles.Add(i1);
                connectedTriangles.Add(i2);
                connectedTriangles.Add(i3);

                if (!foundRefVector)
                {
                    referenceVector = CalculateTriangleNormal(vertices[i1], vertices[i2], vertices[i3]).normalized;

                    if (referenceVector != Vector3.zero)
                    {
                        foundRefVector = true;
                        //Debug.LogError("referenceVector : "+referenceVector);
                    }
                }
            }
        }
        if (connectedTriangles.Count == 0) return new int[0];
        int[] connectedTrianglesArr = connectedTriangles.ToArray();

        foreach (int SVs in SurroundingVertices)
        {
            if (mergeindex != -1) break;
            mergeindex = SVs;
            for (int i = 0; i < connectedTrianglesArr.Length; i += 3)
            {
                int i1 = connectedTrianglesArr[i];
                int i2 = connectedTrianglesArr[i + 1];
                int i3 = connectedTrianglesArr[i + 2];

                if (i1 == vertexIndex) i1 = SVs;
                else
                if (i2 == vertexIndex) i2 = SVs;
                else
                if (i3 == vertexIndex) i3 = SVs;

                Vector3 newnorm = CalculateTriangleNormal(vertices[i1], vertices[i2], vertices[i3]);
                float dotProduct = Vector3.Dot(newnorm.normalized, referenceVector);

                // If the dot product is negative, the normals are pointing in opposite directions
                if (dotProduct < 0)
                {
                    //Debug.LogError("Found A BackFaced Tris");
                    mergeindex = -1;
                    break;
                }
            }
        }
        //return connectedTrianglesArr;
        return mergeindex == -1 ? new int[0] : connectedTrianglesArr;
    }



    static int IsClosedLoop(int[] triangles)
    {
        Dictionary<int, int> vertexCount = new Dictionary<int, int>();
        int maxcount = 0;
        // Count the number of times each vertex appears in the triangles array
        foreach (int vertexIndex in triangles)
        {
            if (vertexCount.ContainsKey(vertexIndex))
            {
                vertexCount[vertexIndex]++;
            }
            else
            {
                vertexCount[vertexIndex] = 1;
            }
        }

        // Check if each vertex is shared by at least two triangles
        foreach (int count in vertexCount.Values)
        {
            if (count < 2)
            {
                maxcount++;
            }
        }

        return maxcount;
    }

    static int GetDissolvePoint(int[] triangles, Vector3[] vertices, int vertexIndex, float angleThreshold)
    {
        Vector3[] Points = new Vector3[3];
        Points[0] = vertices[vertexIndex];
        Points[1] = Vector3.zero;
        Points[2] = Vector3.zero;

        HashSet<int> PIndexes = new HashSet<int>();
        HashSet<Vector2Int> PIndexPairs = new HashSet<Vector2Int>();
        Vector2Int SmallestPair = Vector2Int.zero;
        float smallestAngle = 180;
        int mergeindex = -1;

        int ClosedLoop = IsClosedLoop(triangles);

        if (ClosedLoop > 2) return -1;

        // Find All neighboring vertices
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] == vertexIndex) continue;
            PIndexes.Add(triangles[i]);
        }

        if (ClosedLoop == 2)
        {
            foreach (int pi in PIndexes.ToArray())
            {
                int count = 0;
                for (int i = 0; i < triangles.Length; i++)
                {
                    if (triangles[i] == pi) count++;
                }
                if (count > 1) PIndexes.Remove(pi);
            }
        }

        foreach (int i in PIndexes)
        {
            foreach (int j in PIndexes)
            {
                if (i < j)
                { // only add pairs when i is less than j
                    PIndexPairs.Add(new Vector2Int(i, j));
                }
            }
        }
        foreach (Vector2Int V2I in PIndexPairs)
        {
            Points[1] = vertices[V2I.x];
            Points[2] = vertices[V2I.y];
            if (Points[1] != Vector3.zero && Points[2] != Vector3.zero)
            {
                // Calculate vectors
                Vector3 vector1 = Points[1] - Points[0];
                Vector3 vector2 = Points[2] - Points[0];

                // Calculate the angle between vectors in degrees
                float angle = 180 - Vector3.Angle(vector1, vector2);

                // Check if the angle is below the threshold for dissolving
                //        Debug.Log("Angle : "+(180-angle));
                if (angle < angleThreshold && angle < smallestAngle)
                {
                    SmallestPair = V2I;
                    smallestAngle = angle;
                }

            }
        }

        if (smallestAngle > angleThreshold) return -1;


        for (int i = 0; i < 2; i++)
        {
            int SVs = i == 0 ? SmallestPair.x : SmallestPair.y;
            if (mergeindex != -1) break;
            mergeindex = SVs;
            for (int j = 0; j < triangles.Length; j += 3)
            {
                int i1 = triangles[j];
                int i2 = triangles[j + 1];
                int i3 = triangles[j + 2];


                Vector3 OriginalNorm = CalculateTriangleNormal(vertices[i1], vertices[i2], vertices[i3]);


                if (i1 == vertexIndex) i1 = SVs;
                if (i2 == vertexIndex) i2 = SVs;
                if (i3 == vertexIndex) i3 = SVs;
                if (!IsValidTriangle(vertices[i1], vertices[i2], vertices[i3])) continue;

                Vector3 newnorm = CalculateTriangleNormal(vertices[i1], vertices[i2], vertices[i3]);
                float dotProduct = Vector3.Dot(newnorm, OriginalNorm);

                // If the dot product is negative, the normals are pointing in opposite directions
                if (dotProduct < 0)
                {
                    //                Debug.Log("BackFaced Triangle Found : ");
                    mergeindex = -1;
                    break;
                }
            }
        }

        return mergeindex;


    }

    public static Vector3[] GetPointsInTriangle(Vector3 v0, Vector3 v1, Vector3 v2, int gridSize)
    {
        Vector3[] points = new Vector3[(gridSize + 1) * (gridSize + 2) / 2];
        int index = 0;

        for (int i = 0; i <= gridSize; i++)
        {
            for (int j = 0; j <= gridSize - i; j++)
            {
                float alpha = (float)i / gridSize;
                float beta = (float)j / gridSize;
                float gamma = 1 - alpha - beta;

                Vector3 barycentric = new Vector3(alpha, beta, gamma);
                Vector3 point = BarycentricToWorld(v0, v1, v2, barycentric);

                points[index++] = point;
            }
        }

        return points;
    }

    public static bool RaycastFromPointsInTriangle(Vector3 v0, Vector3 v1, Vector3 v2, int gridSize, Collider col, Vector3 Direction)
    {

        for (int i = 0; i <= gridSize; i++)
        {
            for (int j = 0; j <= gridSize - i; j++)
            {
                float alpha = (float)i / gridSize;
                float beta = (float)j / gridSize;
                float gamma = 1 - alpha - beta;

                Vector3 barycentric = new Vector3(alpha, beta, gamma);
                Vector3 point = BarycentricToWorld(v0, v1, v2, barycentric);

                if (!col.Raycast(new Ray(point + (Direction * 0.00001f), Direction), out hit, Mathf.Infinity)) return false;
            }
        }
        return true;
    }

    static Vector3 BarycentricToWorld(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 barycentric)
    {
        Vector3 worldPoint = v0 * barycentric.x + v1 * barycentric.y + v2 * barycentric.z;
        return worldPoint;
    }

    public static float CalculateTriangleSize(Vector3 a, Vector3 b, Vector3 c)
    {
        // Using Heron's formula to calculate the area of the triangle
        float lengthA = a.magnitude;
        float lengthB = b.magnitude;
        float lengthC = c.magnitude;

        float s = (lengthA + lengthB + lengthC) / 2; // semi-perimeter
        float area = Mathf.Sqrt(s * (s - lengthA) * (s - lengthB) * (s - lengthC));
        return area;
    }
#endif

    public static float FastDist(Vector3 a, Vector3 b)
    {
        return (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y) + (b.z - a.z) * (b.z - a.z);
    }


    // Read all bytes from a file into a dynamically resizing array
    public static int ReadBytesToDBuffer(string filePath, ref byte[] data, bool deflated)
    {
        const int bufferSize = 4096; // Adjust the buffer size based on your needs

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            Stream inputStream = fileStream;

            if (deflated)
            {
                // Wrap the file stream with a DeflateStream
                inputStream = new DeflateStream(fileStream, CompressionMode.Decompress);
            }

            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = inputStream.Read(data, totalBytesRead, bufferSize)) > 0)
            {
                totalBytesRead += bytesRead;

                // Resize the array if necessary
                if (totalBytesRead + bufferSize > data.Length)
                    Array.Resize(ref data, data.Length + bufferSize);
            }
            inputStream.DisposeAsync();
            return totalBytesRead;
        }
    }

    public static void InstallMap(CWMap cWMap)
    {
        string mapPath = CreateDirectory(Application.persistentDataPath + "/Maps/" + cWMap.name + "/");
        if (Directory.Exists(Application.persistentDataPath + "/Maps/" + cWMap.name + "/")) Directory.Delete(Application.persistentDataPath + "/Maps/" + cWMap.name + "/", true);
        string TexPath = CreateDirectory(mapPath + "Textures/");
        string AudioPath = CreateDirectory(mapPath + "Audios/");
        string MeshPath = CreateDirectory(mapPath + "Meshes/");
        string ShadersPath = CreateDirectory(mapPath + "Shaders/");
        string TerrainsPath = CreateDirectory(mapPath + "Terrains/");
        string LightSettingsPath = CreateDirectory(mapPath + "LightSettings/");
        string AIPath = CreateDirectory(mapPath + "AI/");

        foreach (KeyValuePair<string, byte[]> KVP in cWMap.TexturesOnStage) File.WriteAllBytes(TexPath + KVP.Key + ".bundle", KVP.Value); cWMap.TexturesOnStage = null;
        foreach (KeyValuePair<string, byte[]> KVP in cWMap.AudiosOnStage) File.WriteAllBytes(AudioPath + KVP.Key + ".bundle", KVP.Value); cWMap.AudiosOnStage = null;
        foreach (KeyValuePair<string, byte[]> KVP in cWMap.MeshesOnStage) File.WriteAllBytes(MeshPath + KVP.Key + ".umesh", KVP.Value); cWMap.MeshesOnStage = null;
        foreach (KeyValuePair<string, byte[]> KVP in cWMap.TerrainsOnStage) File.WriteAllBytes(TerrainsPath + KVP.Key + ".bundle", KVP.Value); cWMap.TerrainsOnStage = null;
        foreach (KeyValuePair<string, byte[]> KVP in cWMap.ShadersOnStage) File.WriteAllBytes(ShadersPath + KVP.Key + ".bundle", KVP.Value); cWMap.ShadersOnStage = null;

        File.WriteAllBytes(AIPath + "NavMesh.bundle", cWMap.NavMeshData); cWMap.NavMeshData = null;
        if(cWMap.VNavMeshData!=null)File.WriteAllBytes(AIPath + "VNavMesh.bundle", cWMap.VNavMeshData); cWMap.VNavMeshData = null;

        if (cWMap.PostProcessingProfile != null) File.WriteAllBytes(ShadersPath + "PostFX.bundle", cWMap.PostProcessingProfile); cWMap.PostProcessingProfile = null;

        //if (cWMap.lightProbesData != null) 
        File.WriteAllBytes(LightSettingsPath + "LightProbes.bundle", cWMap.lightProbesData); cWMap.lightProbesData = null;
        cWMap.root = mapPath;
        File.WriteAllBytes(mapPath + "data.dat", MessagePackSerializer.Serialize(cWMap));


    }


    // Generic method to copy a component from one GameObject to another
    public static T CopyTo<T>(this T original, GameObject destination) where T : Component
    {
        // Add a new component of the same type to the destination GameObject
        T copy = destination.AddComponent<T>();

        // Get all fields of the component and copy their values
        foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            field.SetValue(copy, field.GetValue(original));
        }

        return copy;
    }

}

[System.Serializable]
[MessagePackObject]
public class MapInfo
{
    public enum MapSize { Small = 0, Mid = 1, Large = 2 }
    [Key(0)] public string Name = "Cool Map";
    [Key(1)] public string Description;
    //public int ID;
    [Key(2)] public bool Vehichles = true;
    [Key(3)] public bool Traps = true;
    [Key(4)] public bool SupplyCrate;
    [Key(5)] public byte Size = 0;
    [IgnoreMember] public Sprite Icon;
    [HideInInspector][Key(6)] public string IconID;
    [IgnoreMember] public Sprite MapIcon;
    [HideInInspector][Key(7)] public string MapIconID;
    [Key(8)] public bool HasSafeZone;
    [Key(9)] public bool ShowInMP = true;
    [Key(10)] public bool ShowInBR;
}
[Serializable]
public class PlayerPrefs
{

    [System.Serializable]
    [MessagePackObject]
    public class Vars
    {

        [Key(0)] public Dictionary<string, bool> Booleans = new Dictionary<string, bool>();
        [Key(1)] public Dictionary<string, int> Integers = new Dictionary<string, int>();
        [Key(2)] public Dictionary<string, float> Floats = new Dictionary<string, float>();
        [Key(3)] public Dictionary<string, string> Strings = new Dictionary<string, string>();
    }
    public static Dictionary<string, bool> Booleans = new Dictionary<string, bool>();
    public static Dictionary<string, int> Integers = new Dictionary<string, int>();
    public static Dictionary<string, float> Floats = new Dictionary<string, float>();
    public static Dictionary<string, string> Strings = new Dictionary<string, string>();

    public static bool GetBool(string Key) { if (Booleans.ContainsKey(Key)) return Booleans[Key]; else return false; }
    public static string GetString(string Key) { return Strings[Key]; }
    public static float GetFloat(string Key) { return Floats[Key]; }
    public static int GetInt(string Key) { return Integers[Key]; }

    public static void SetBool(string Key, bool value) { if (HasKey(Key)) Booleans[Key] = value; else Booleans.Add(Key, value); }
    public static void SetString(string Key, string value) { if (HasKey(Key)) Strings[Key] = value; else Strings.Add(Key, value); }
    public static void SetFloat(string Key, float value) { if (HasKey(Key)) Floats[Key] = value; else Floats.Add(Key, value); }
    public static void SetInt(string Key, int value) { if (HasKey(Key)) Integers[Key] = value; else Integers.Add(Key, value); }

    public static bool HasKey(string Key)
    {
        return Booleans.ContainsKey(Key) || Strings.ContainsKey(Key) || Floats.ContainsKey(Key) || Integers.ContainsKey(Key);
    }

    public static void DeleteKey(string Key)
    {
        if (Booleans.ContainsKey(Key)) Booleans.Remove(Key);
        if (Strings.ContainsKey(Key)) Strings.Remove(Key);
        if (Floats.ContainsKey(Key)) Floats.Remove(Key);
        if (Integers.ContainsKey(Key)) Integers.Remove(Key);
    }

    static string path = Application.persistentDataPath + "/Prefs.json";
    static string Compressedpath = Application.persistentDataPath + "/CompressedPrefs.json";
    public static void Save()
    {

        Vars SaveData = new Vars();
        SaveData.Booleans = Booleans;
        SaveData.Integers = Integers;
        SaveData.Floats = Floats;
        SaveData.Strings = Strings;

        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(SaveData));
        //File.WriteAllText(path,JsonUtility.ToJson(SaveData));
    }

    public static void Load()
    {
#if UNITY_STANDALONE
        if(!File.Exists(path)&&File.Exists(Application.streamingAssetsPath+"/Prefs.Json"))File.Copy(Application.streamingAssetsPath+"/Prefs.Json",path);
#endif

        if (File.Exists(path))
        {
            /*BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            Vars SaveData = bf.Deserialize(stream) as Vars;
            stream.Close();*/
            Vars SaveData = Newtonsoft.Json.JsonConvert.DeserializeObject<Vars>(File.ReadAllText(path));
            //Vars SaveData = JsonUtility.FromJson<Vars>(File.ReadAllText(path));
            Booleans = SaveData.Booleans;
            Integers = SaveData.Integers;
            Strings = SaveData.Strings;
            Floats = SaveData.Floats;
        }

    }




}