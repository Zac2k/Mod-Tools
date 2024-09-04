// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "ZicZac/Water/Mid" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Cube ("Reflection Cubemap", Cube) = "_Skybox" { }
     [noscaleoffset]_BumpMap ("Normalmap", 2D) = "bump" {}
    _BumpMap2 ("Normalmap2", 2D) = "bump" {}
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

struct Input {
    fixed2 uv_MainTex;
    fixed2 uv_BumpMap;
    fixed2 uv2_BumpMap2;
    fixed3 worldRefl;
    INTERNAL_DATA
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    o.Albedo = c.rgb*0.5;
    o.Normal = UnpackNormal((tex2D(_BumpMap, IN.uv_MainTex)+tex2D(_BumpMap2, IN.uv2_BumpMap2)));
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
