Shader "ZicZac/MidGraphic/Opaque"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        [noscaleoffset]_BumpMap ("Normalmap", 2D) = "bump" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 400

        CGPROGRAM
        #pragma surface surf BlinnPhong interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
        #pragma target 2.0

        // Enable instancing support
        #pragma instancing_options assumeuniformscaling

        sampler2D _MainTex;
        sampler2D _BumpMap;

        struct Input
        {
            fixed2 uv_MainTex;
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
        UNITY_DEFINE_INSTANCED_PROP(fixed, _Shininess)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Albedo = tex.rgb;
            o.Gloss = tex.a;
            o.Specular = tex.a * UNITY_ACCESS_INSTANCED_PROP(Props, _Shininess);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
