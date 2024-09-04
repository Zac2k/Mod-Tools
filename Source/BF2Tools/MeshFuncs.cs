using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public static class MeshFuncs
    {

        public const float PI = 3.14159265358979f;
        public const float RADTODEG = 180f / PI;
        public const float DEGTORAD = PI / 180f;
        public const float EPSILON = 0.0000001f;

// Adds two float3 vectors
    public static Vector3 AddFloat3(Vector3 a, Vector3 b)
    {
        Vector3 result;
        result.x = a.x + b.x;
        result.y = a.y + b.y;
        result.z = a.z + b.z;
        
        return result;
    }

    // Returns magnitude of a vector
public static float Magnitude(Vector3 vector)
{
    float v = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
    if (v < 0)
        return 0;

    return (float)Math.Sqrt(v);
}

// Rescales vector to the length of one
public static Vector3 Normalize(Vector3 vector)
{
    float m = Magnitude(vector);

    // Prevent division by zero
    if (m < EPSILON)
        return new Vector3(0, 0, 0);

    return new Vector3(vector.x / m, vector.y / m, vector.z / m);
}

public static float DotProduct(Vector3 vector1, Vector3 vector2)
{
    return vector1.x * vector2.x + vector1.y * vector2.y + vector1.z * vector2.z;
}

public static Vector3 CrossProduct(Vector3 vec1, Vector3 vec2)
{
    Vector3 result;
    result.x = vec1.y * vec2.z - vec1.z * vec2.y;
    result.y = vec1.z * vec2.x - vec1.x * vec2.z;
    result.z = vec1.x * vec2.y - vec1.y * vec2.x;
    return result;
}


public static void mat4identity(ref Matrix4x4 m)
{
    m.m00 = 1;
    m.m01 = 0;
    m.m02 = 0;
    m.m03 = 0;

    m.m10 = 0;
    m.m11 = 1;
    m.m12 = 0;
    m.m13 = 0;

    m.m20 = 0;
    m.m21 = 0;
    m.m22 = 1;
    m.m23 = 0;

    m.m30 = 0;
    m.m31 = 0;
    m.m32 = 0;
    m.m33 = 1;
}


    }

