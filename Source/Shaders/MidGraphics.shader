Shader "ZicZac/MidGraphic/Opaque"
{Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
    _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
   // _PBR ("PBR (RGB)", 2D) = "white" {}
    [noscaleoffset]_BumpMap ("Normalmap", 2D) = "bump" {}
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 400

    CGPROGRAM
    #pragma surface surf BlinnPhong interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
    #pragma target 2.0
    
sampler2D _MainTex;
//sampler2D _PBR;
sampler2D _BumpMap;
fixed4 _Color;
half _Shininess;

struct Input {
    fixed2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex)* _Color;
   // fixed4 pbr = tex2D(_PBR, IN.uv_MainTex);
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
    o.Albedo = tex.rgb ;
   // o.Emission = tex.rgb*pbr.b;
    o.Gloss = tex.a;
    //o.Alpha = tex.a * _Color.a;
    o.Specular = tex.a*_Shininess;
}
ENDCG

}

    FallBack "Diffuse"
}
