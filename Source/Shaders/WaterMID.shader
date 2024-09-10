// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "ZicZac/Water/Mid" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Cube ("Reflection Cubemap", Cube) = "_Skybox" { }
     [noscaleoffset]_BumpMap ("Normalmap", 2D) = "bump" {}
    _BumpMap2 ("Normalmap2", 2D) = "bump" {}
    _ScrollSpeed ("Scroll Speed", Range(-10,10)) = 1 // The speed at which the bump maps scroll
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 300
    cull off
CGPROGRAM
#pragma surface surf Lambert alpha:fade noforwardadd interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass novertexlights noshadow nolightmap nofog
#pragma target 2.0

sampler2D _MainTex;
sampler2D _BumpMap;
 sampler2D _BumpMap2;
samplerCUBE _Cube;
fixed4 _Color;
fixed4 _ReflectColor;
fixed _ScrollSpeed; // Scrolling speed for the bump maps

struct Input {
    float2 uv_MainTex;
    float2 uv2_BumpMap2;
    fixed3 worldRefl;
    INTERNAL_DATA
};

void surf (Input IN, inout SurfaceOutput o) {
    // Scroll the UV coordinates of the bump maps using time and scroll speed
    float2 scrolledUV1 = IN.uv_MainTex + (_ScrollSpeed * _Time.y);
    float2 scrolledUV2 = IN.uv2_BumpMap2 - (_ScrollSpeed * _Time.y);

    // Albedo comes from a texture tinted by color
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    o.Albedo = c.rgb;

    // Combine and unpack the scrolled normal maps
    half3 normal1 = UnpackNormal(tex2D(_BumpMap, scrolledUV1))*0.5;
    half3 normal2 = UnpackNormal(tex2D(_BumpMap2, scrolledUV2))*0.5;
    o.Normal = normalize(normal1 + normal2);

    fixed3 worldRefl = WorldReflectionVector (IN, o.Normal);
    fixed4 reflcol = texCUBE (_Cube, worldRefl);
    //reflcol *= c.a;
    o.Emission = reflcol.rgb * _ReflectColor.rgb;
    o.Alpha =c.a*_Color.a; 
   // reflcol.a * _ReflectColor.a; 
   // o.Albedo*=reflcol;
}
ENDCG
}

FallBack "Legacy Shaders/Transparent/Diffuse"
}
