Shader "ZicZac/HighGraphic/Opaque"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGBA)", 2D) = "white" {}
        [noscaleoffset]_PBR ("PBR (RGBA)", 2D) = "white" {}
        [Gamma]_Metallic ("Metallic", Range(0,1)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 1
        [noscaleoffset][Normal]_BumpMap ("Normalmap", 2D) = "bump" {}
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" }

        LOD 512

        CGPROGRAM
        #pragma surface surf Standard interpolateview approxview nolppv nometa exclude_path:deferred exclude_path:prepass fullforwardshadows

        #pragma target 3.0

        // Enable instancing support
        #pragma instancing_options assumeuniformscaling

        sampler2D _MainTex;
        sampler2D _PBR;
        sampler2D _BumpMap;

        struct Input
        {
            fixed2 uv_MainTex;
        };

        //fixed _Metallic;
        //fixed _Glossiness;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
        UNITY_DEFINE_INSTANCED_PROP(fixed, _Metallic)
        UNITY_DEFINE_INSTANCED_PROP(fixed, _Glossiness)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            fixed4 pbr = tex2D(_PBR, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Occlusion = pbr.r;
            o.Metallic = pbr.g * UNITY_ACCESS_INSTANCED_PROP(Props, _Metallic);
            o.Emission = c.rgb * pbr.b;
            o.Smoothness = pbr.a * UNITY_ACCESS_INSTANCED_PROP(Props, _Glossiness);
        }
        ENDCG
    }
    // FallBack "Diffuse"
}
