Shader "BF2/Terrain"
{
    Properties {
        [NoScaleOffset]_MainTex ("Base (RGB)", 2D) = "white" {}
        [NoScaleOffset]_LightMap ("Lightmap (RGB)", 2D) = "black" {}
        [NoScaleOffset]_DetailtMap1 ("DetailtMap1 (RGB)", 2D) = "white" {}
        [NoScaleOffset]_DetailtMap2 ("DetailtMap2 (RGB)", 2D) = "black" {}
        [NoScaleOffset]_LowDetailtMap ("LowDetailtMap (RGB)", 2D) = "white" {}
        [NoScaleOffset]_LowDetailLayer ("_LowDetailLayer (RGB)", 2D) = "white" {}
        [NoScaleOffset]_Layer_1 ("Layer_1 (RGB)", 2D) = "white" {}
        [NoScaleOffset]_Layer_2 ("Layer_2 (RGB)", 2D) = "white" {}
        [NoScaleOffset]_Layer_3 ("Layer_3 (RGB)", 2D) = "white" {}
        [NoScaleOffset]_Layer_4 ("Layer_4 (RGB)", 2D) = "white" {}
        [NoScaleOffset]_Layer_5 ("Layer_5 (RGB)", 2D) = "white" {}
        [NoScaleOffset]_Layer_6 ("Layer_6 (RGB)", 2D) = "white" {}

        [Toggle] _Rotate90("Rotate 90", Float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.3
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Scale1 ("Layer 1 Scale", Float) = 32
        _Scale2 ("Layer 2 Scale", Float) = 32
        _Scale3 ("Layer 3 Scale", Float) = 32
        _Scale4 ("Layer 4 Scale", Float) = 32
        _Scale5 ("Layer 5 Scale", Float) = 32
        _Scale6 ("Layer 6 Scale", Float) = 32
        _LowDetailScale ("Low Detail Layer Scale", Float) = 12
    }

    SubShader {
        LOD 200
        Tags { "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Standard nodynlightmap
        #pragma target 3.0

        struct Input {
            float2 uv_MainTex;
            float2 uv2_MainTex;
            float2 uv_LightMap;
            float2 uv_DetailtMap1;
            float2 uv_DetailtMap2;
            float2 uv_LowDetailtMap;
        };

        fixed _Glossiness;
        fixed _Metallic;
        sampler2D _MainTex;
        sampler2D _LightMap;
        sampler2D _DetailtMap1;
        sampler2D _DetailtMap2;
        sampler2D _LowDetailtMap;
        sampler2D _LowDetailLayer;
        sampler2D _Layer_1;
        sampler2D _Layer_2;
        sampler2D _Layer_3;
        sampler2D _Layer_4;
        sampler2D _Layer_5;
        sampler2D _Layer_6;
        bool _Rotate90;

        fixed _Scale1;
        fixed _Scale2;
        fixed _Scale3;
        fixed _Scale4;
        fixed _Scale5;
        fixed _Scale6;
        fixed _LowDetailScale;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            if(_Rotate90){
                uv.x = IN.uv_MainTex.y; 
                uv.y = IN.uv_MainTex.x;
            }

            fixed4 lm = tex2D(_LightMap, uv);
            fixed3 baseColor = tex2D(_MainTex, uv).rgb;
            fixed3 detail1 = tex2D(_DetailtMap1, uv).rgb;
            fixed3 detail2 = tex2D(_DetailtMap2, uv).rgb;
            fixed3 lowDetail = tex2D(_LowDetailtMap, uv).rgb;

            fixed3 layer1 = tex2D(_Layer_1, uv * _Scale1).rgb;
            fixed3 layer2 = tex2D(_Layer_2, uv * _Scale2).rgb;
            fixed3 layer3 = tex2D(_Layer_3, uv * _Scale3).rgb;
            fixed3 layer4 = tex2D(_Layer_4, uv * _Scale4).rgb;
            fixed3 layer5 = tex2D(_Layer_5, uv * _Scale5).rgb;
            fixed3 layer6 = tex2D(_Layer_6, uv * _Scale6).rgb;
            fixed3 lowDetailLayer = tex2D(_LowDetailLayer, uv * _LowDetailScale).rgb;

            fixed3 detail = 
            (layer1.rgb * detail1.b) +
            (layer2.rgb * detail1.g) +
            (layer3.rgb * detail1.r) +
            (layer4.rgb * detail2.b) +
            (layer5.rgb * detail2.g) +
            (layer6.rgb * detail2.r);

            detail *= unity_ColorSpaceDouble.r;

            fixed3 lowDet = 
            (lowDetailLayer.r * detail1.b) +
            (lowDetailLayer.g * detail1.g) +
            (lowDetailLayer.b * detail1.r);

            lowDet *= unity_ColorSpaceDouble.r;

            fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * baseColor;
            
            o.Albedo = lerp(ambient, baseColor, clamp((12 * lm.g) - 0.5, 0, 1)) * detail * (1 - lm.r);
            o.Emission = lm.b * lm.g * o.Albedo.rgb;
            o.Smoothness = _Glossiness * lm.b * lowDet * detail;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Lightmapped/VertexLit"
}
