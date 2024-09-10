Shader "ZicZac/MidGraphic/Atlas/Cutout"
{
    Properties
    {
        [noscaleoffset]_MainTex ("Albedo (RGBA)", 2D) = "white" {}
        _Offset ("Offset", Vector) = (0, 0, 1, 1)
        [noscaleoffset][Normal]_BumpMap ("Normalmap", 2D) = "bump" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="True"}
        Cull Off
        LOD 400

        CGPROGRAM
        #pragma surface surf BlinnPhong alphatest:_Cutoff interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
        #pragma target 2.0
        
        sampler2D _MainTex;
        sampler2D _BumpMap;

        half4 _Offset;
        struct Input {
            half2 uv_MainTex;
        };

        #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // Additional per-instance properties
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Calculate UV coordinates based on the atlas offset
            half2 uv = frac(IN.uv_MainTex);
            uv = fmod(uv, _Offset.zw);
            uv += _Offset.xy;

            // Sample the main texture and normal map
            fixed4 tex = tex2D(_MainTex, uv);
            fixed4 normalTex = tex2D(_BumpMap, uv);
            o.Normal = UnpackNormal(normalTex);
            o.Albedo = tex.rgb;
            o.Gloss = tex.a;
            o.Specular = tex.a;
            o.Alpha = tex.a;
        }
        ENDCG
    }
    FallBack "Diffuse" 
}
