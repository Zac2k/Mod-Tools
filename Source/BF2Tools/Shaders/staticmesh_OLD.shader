Shader "BF2/BaseDetailNDetail"
{
    Properties
    {
        _MainTex ("Albedo (RGBA)", 2D) = "white" {}
        _Detail ("Detail (RGB)", 2D) = "gray" {}
        [noscaleoffset][Normal]_BumpMap ("DetailNormal", 2D) = "bump" {}
        _Metallic ("Metallic", Range(0,1)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 1 
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" }

        LOD 512

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Detail;
        sampler2D _BumpMap;

        struct Input
        {
            fixed2 uv_MainTex;
    float2 uv3_Detail;
        };

        half _Metallic;
        half _Glossiness;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        //UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
            half4 d = tex2D(_Detail, IN.uv3_Detail);

            c*=d* unity_ColorSpaceDouble.r;
            
            o.Albedo = c;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv3_Detail));
            // Metallic and smoothness come from slider variables
            o.Smoothness =c.a*_Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}


