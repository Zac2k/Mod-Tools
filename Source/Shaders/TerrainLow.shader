Shader "ZicZac/LowGraphic/Terrain"{
// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified VertexLit shader, optimized for high-poly meshes. Differences from regular VertexLit one:
// - less per-vertex work compared with Mobile-VertexLit
// - supports only DIRECTIONAL lights and ambient term, saves some vertex processing power
// - no per-material color
// - no specular
// - no emission

    Properties {
        [noscaleoffset]_Splat ("Splat (RGBA)", 2D) = "white" {}
        _Scales ("Scales", Vector) = (1, 1, 1, 1)

        [noscaleoffset]_Tex1 ("Tex1 (RGBA)", 2D) = "white" {}

        [noscaleoffset]_Tex2 ("Tex2 (RGBA)", 2D) = "white" {}

        [noscaleoffset]_Tex3 ("Tex3 (RGBA)", 2D) = "white" {}

        [noscaleoffset]_Tex4 ("Tex4 (RGBA)", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 80

    Pass {
        Name "FORWARD"
        Tags { "LightMode" = "ForwardBase" }
CGPROGRAM
#pragma vertex vert_surf
#pragma fragment frag_surf noforwardadd interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
#pragma target 2.0
#pragma multi_compile_fwdbase
#pragma multi_compile_fog
#include "HLSLSupport.cginc"
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

        inline fixed3 LightingLambertVS (fixed3 normal, fixed3 lightDir)
        {
            fixed diff = max (0, dot (normal, lightDir));
            return _LightColor0.rgb * diff;
        }

        sampler2D _Splat;
        sampler2D _Tex1;
        sampler2D _Tex2;
        sampler2D _Tex3;
        sampler2D _Tex4;

        fixed4 _Scales;
        

        struct Input {
            float2 uv_Splat;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 Splat = tex2D (_Splat, IN.uv_Splat);

            fixed4 c1 = tex2D (_Tex1, IN.uv_Splat*_Scales.x);
            fixed4 c2 = tex2D (_Tex2, IN.uv_Splat*_Scales.y);
            fixed4 c3 = tex2D (_Tex3, IN.uv_Splat*_Scales.z);
            fixed4 c4 = tex2D (_Tex4, IN.uv_Splat*_Scales.w);
            

            o.Albedo =
            (c1.rgb*Splat.r)+
            (c2.rgb*Splat.g)+
            (c3.rgb*Splat.b)+
            (c4.rgb*Splat.a);
        }

        
        struct v2f_surf {
  fixed4 pos : SV_POSITION;
  float2 pack0 : TEXCOORD0;
  #ifndef LIGHTMAP_ON
  fixed3 normal : TEXCOORD1;
  #endif
  #ifdef LIGHTMAP_ON
  half2 lmap : TEXCOORD2;
  #endif
  #ifndef LIGHTMAP_ON
  half3 vlight : TEXCOORD2;
  #endif
  LIGHTING_COORDS(3,4)
  UNITY_FOG_COORDS(5)
  UNITY_VERTEX_OUTPUT_STEREO
};
fixed4 _Splat_ST;
v2f_surf vert_surf (appdata_full v)
{
    v2f_surf o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.pack0.xy = TRANSFORM_TEX(v.texcoord, _Splat);
    #ifdef LIGHTMAP_ON
    o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #endif 
    fixed3 worldN = UnityObjectToWorldNormal(v.normal);
    #ifndef LIGHTMAP_ON
    o.normal = worldN;
    #endif
    #ifndef LIGHTMAP_ON

    o.vlight = ShadeSH9 (half4(worldN,1.0));
    o.vlight += LightingLambertVS (worldN, _WorldSpaceLightPos0.xyz);

    #endif
    TRANSFER_VERTEX_TO_FRAGMENT(o);
    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}
half4 frag_surf (v2f_surf IN) : SV_Target
{
    Input surfIN;
    surfIN.uv_Splat = IN.pack0.xy;
    SurfaceOutput o;
    o.Albedo = 0.0;
    o.Emission = 0.0;
    o.Specular = 0.0;
    o.Alpha = 0.0;
    o.Gloss = 0.0;
    #ifndef LIGHTMAP_ON
    o.Normal = IN.normal; 
    #else
    o.Normal = 0;
    #endif
    surf (surfIN, o);
    fixed atten = LIGHT_ATTENUATION(IN);
    fixed4 c = 0;
    #ifndef LIGHTMAP_ON
    c.rgb = o.Albedo * IN.vlight * atten;
    #endif
    #ifdef LIGHTMAP_ON
    fixed3 lm = DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy));
    #ifdef SHADOWS_SCREEN
    c.rgb += o.Albedo * min(lm, atten*2);
    #else
    c.rgb += o.Albedo * lm;
    #endif
    c.a = o.Alpha;
    #endif
    UNITY_APPLY_FOG(IN.fogCoord, c);
    UNITY_OPAQUE_ALPHA(c.a);
    return c;
}

ENDCG
    }
}

FallBack "Mobile/VertexLit"
}
