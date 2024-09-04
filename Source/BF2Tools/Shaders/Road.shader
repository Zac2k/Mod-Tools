Shader "BF2/Road"
{
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Detail ("Detail (RGB)", 2D) = "gray" {}

        _Glossiness ("Smoothness", Range(0,1)) = 0.3
        
        _Blend ("DetailBlend", Range (0, 1)) = 0.08
    }

    SubShader {
        LOD 200
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        struct Input {
            float2 uv_MainTex;
    float2 uv_Detail;
    fixed4 color : COLOR;
        };

        half _Glossiness;
        half _Metallic;

        half _MainScale;
        half _DetailScale;


        sampler2D _MainTex;
        sampler2D _Detail;

        float _Blend;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half2 uv = IN.uv_MainTex;

            half4 baseColor = tex2D(_MainTex, uv);
            half4 Detail = tex2D(_Detail, IN.uv_Detail);
            
            
            o.Albedo = lerp(baseColor,Detail,_Blend);
            

            
            half2 NRMUV = uv;

            o.Smoothness = _Glossiness * (baseColor* unity_ColorSpaceDouble.r);

            // Apply the fade factor to the alpha
            o.Alpha = baseColor.a*IN.color.a;
        }
        ENDCG
    }
Fallback "Legacy Shaders/Transparent/VertexLit"
}


