Shader "ZicZac/HighGraphic/Cutout"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGBA)", 2D) = "white" {}
        [noscaleoffset]_PBR ("PBR (RGBA)", 2D) = "yellow" {}
        [Gamma]_Metallic ("Metallic", Range(0,1)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 1
        [noscaleoffset][Normal]_BumpMap ("Normalmap", 2D) = "bump" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="True"}
        Cull Off

        LOD 512

        CGPROGRAM
        // Physically based Standard lighting model, with shadows enabled on all light types
        #pragma surface surf Standard alphatest:_Cutoff interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass

        // Use shader model 3.0 target for better lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PBR;
        sampler2D _BumpMap;

        //half _Metallic;
        //half _Glossiness;
        //fixed4 _Color;

        struct Input
        {
            fixed2 uv_MainTex;
        };

        // Add instancing support
        #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(fixed, _Metallic)
            UNITY_DEFINE_INSTANCED_PROP(fixed, _Glossiness)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample textures
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            fixed4 pbr = tex2D(_PBR, IN.uv_MainTex);
            
            // Albedo
            o.Albedo = c.rgb;
            // Normal map
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            // Alpha
            o.Alpha = c.a;
            // Metallic and smoothness
            o.Occlusion = pbr.r;
            o.Metallic = pbr.g * UNITY_ACCESS_INSTANCED_PROP(Props, _Metallic);
            o.Emission = c.rgb * pbr.b;
            o.Smoothness = pbr.a * UNITY_ACCESS_INSTANCED_PROP(Props, _Glossiness);
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}
