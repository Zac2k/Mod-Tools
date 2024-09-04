// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "ZicZac/Water/OpaqueHigh" {
 Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 1
        _Glossiness ("Smoothness", Range(-1,1)) = 1
         [noscaleoffset]_BumpMap ("Normalmap", 2D) = "bump" {}
         _BumpMap2 ("Normalmap2", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }
        LOD 512
        cull off
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard noforwardadd interpolateview approxview nolppv nometa nodynlightmap nolightmap exclude_path:deferred exclude_path:prepass novertexlights

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 2.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _BumpMap2;

        struct Input
        {
            fixed2 uv_MainTex;
            fixed2 uv2_BumpMap2;
        };

        half _Metallic;
        half _Glossiness;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal((tex2D(_BumpMap, IN.uv_MainTex)+tex2D(_BumpMap2, IN.uv2_BumpMap2))/2);
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            //o.Emission = c.rgb*pbr.b;
            o.Smoothness =_Glossiness;
           // o.Alpha=c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
